using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Autofac;
using AutoMapper;
using Microsoft.IO;
using Miningcore.Blockchain.Bitcoin;
using Miningcore.Blockchain.Apsak.Configuration;
using Miningcore.Configuration;
using Miningcore.Extensions;
using Miningcore.JsonRpc;
using Miningcore.Messaging;
using Miningcore.Mining;
using Miningcore.Nicehash;
using Miningcore.Notifications.Messages;
using Miningcore.Persistence;
using Miningcore.Persistence.Repositories;
using Miningcore.Stratum;
using Miningcore.Time;
using Miningcore.Util;
using Newtonsoft.Json;
using static Miningcore.Util.ActionUtils;

namespace Miningcore.Blockchain.Apsak;

[CoinFamily(CoinFamily.Apsak)]
public class ApsakPool : PoolBase
{
    public ApsakPool(IComponentContext ctx,
        JsonSerializerSettings serializerSettings,
        IConnectionFactory cf,
        IStatsRepository statsRepo,
        IMapper mapper,
        IMasterClock clock,
        IMessageBus messageBus,
        RecyclableMemoryStreamManager rmsm,
        NicehashService nicehashService) :
        base(ctx, serializerSettings, cf, statsRepo, mapper, clock, messageBus, rmsm, nicehashService)
    {
    }

    protected object[] currentJobParams;
    protected ApsakJobManager manager;
    private ApsakPoolConfigExtra extraPoolConfig;
    private ApsakCoinTemplate coin;

    protected virtual async Task OnSubscribeAsync(StratumConnection connection, Timestamped<JsonRpcRequest> tsRequest, CancellationToken ct)
    {
        var request = tsRequest.Value;
        var context = connection.ContextAs<ApsakWorkerContext>();

        if(request.Id == null)
            throw new StratumException(StratumError.MinusOne, "missing request id");

        // setup worker context
        var requestParams = request.ParamsAs<string[]>();
        context.UserAgent = requestParams.FirstOrDefault()?.Trim();
        context.IsLargeJob = manager.ValidateIsLargeJob(context.UserAgent);

        if(manager.ValidateIsGodMiner(context.UserAgent))
        {
            var data = new object[]
            {
                null,
            }
            .Concat(manager.GetSubscriberData(connection))
            .ToArray();

            await connection.RespondAsync(data, request.Id);
        }

        else
        {
            var data = new object[]
            {
                true,
                "ApsakStratum/1.0.0",
            };

            await connection.RespondAsync(data, request.Id);
            await connection.NotifyAsync(ApsakStratumMethods.SetExtraNonce, manager.GetSubscriberData(connection));
        }

        context.IsSubscribed = true;
    }

    protected virtual async Task OnAuthorizeAsync(StratumConnection connection, Timestamped<JsonRpcRequest> tsRequest, CancellationToken ct)
    {
        var request = tsRequest.Value;

        if(request.Id == null)
            throw new StratumException(StratumError.MinusOne, "missing request id");

        var context = connection.ContextAs<ApsakWorkerContext>();

        if(!context.IsSubscribed)
            throw new StratumException(StratumError.NotSubscribed, "subscribe first please, we aren't savages");

        var requestParams = request.ParamsAs<string[]>();

        // setup worker context
        context.IsSubscribed = true;

        var workerValue = requestParams?.Length > 0 ? requestParams[0] : null;
        var password = requestParams?.Length > 1 ? requestParams[1] : null;
        var passParts = password?.Split(PasswordControlVarsSeparator);

        // extract worker/miner
        var split = workerValue?.Split('.');
        var minerName = split?.FirstOrDefault()?.Trim();
        var workerName = split?.Skip(1).FirstOrDefault()?.Trim() ?? string.Empty;

        // assumes that minerName is an address
        var (apsakAddressUtility, errorApsakAddressUtility) = ApsakUtils.ValidateAddress(minerName, manager.Network, coin.Symbol);
        if (errorApsakAddressUtility != null)
            logger.Warn(() => $"[{connection.ConnectionId}] Unauthorized worker: {errorApsakAddressUtility}");
        else
        {
            context.IsAuthorized = true;
            logger.Info(() => $"[{connection.ConnectionId}] worker: {minerName} => {ApsakConstants.ApsakAddressType[apsakAddressUtility.ApsakAddress.Version()]}");
        }

        context.Miner = minerName;
        context.Worker = workerName;

        if(context.IsAuthorized)
        {
            // respond
            await connection.RespondAsync(context.IsAuthorized, request.Id);

            // log association
            logger.Info(() => $"[{connection.ConnectionId}] Authorized worker {workerValue}");

            // extract control vars from password
            var staticDiff = GetStaticDiffFromPassparts(passParts);

            // Nicehash support
            var nicehashDiff = await GetNicehashStaticMinDiff(context, coin.Name, coin.GetAlgorithmName());

            if(nicehashDiff.HasValue)
            {
                if(!staticDiff.HasValue || nicehashDiff > staticDiff)
                {
                    logger.Info(() => $"[{connection.ConnectionId}] Nicehash detected. Using API supplied difficulty of {nicehashDiff.Value}");

                    staticDiff = nicehashDiff;
                }

                else
                    logger.Info(() => $"[{connection.ConnectionId}] Nicehash detected. Using miner supplied difficulty of {staticDiff.Value}");
            }

            // Static diff
            if(staticDiff.HasValue &&
               (context.VarDiff != null && staticDiff.Value >= context.VarDiff.Config.MinDiff ||
                   context.VarDiff == null && staticDiff.Value > context.Difficulty))
            {
                context.VarDiff = null; // disable vardiff
                context.SetDifficulty(staticDiff.Value);

                logger.Info(() => $"[{connection.ConnectionId}] Setting static difficulty of {staticDiff.Value}");
            }

            // send intial job
            await SendJob(connection, context, currentJobParams);
        }

        else
        {
            await connection.RespondErrorAsync(StratumError.UnauthorizedWorker, "Authorization failed", request.Id, context.IsAuthorized);

            if(clusterConfig?.Banning?.BanOnLoginFailure is null or true)
            {
                // issue short-time ban if unauthorized to prevent DDos on daemon (validateaddress RPC)
                logger.Info(() => $"[{connection.ConnectionId}] Banning unauthorized worker {minerName} for {loginFailureBanTimeout.TotalSeconds} sec");

                banManager.Ban(connection.RemoteEndpoint.Address, loginFailureBanTimeout);

                Disconnect(connection);
            }
        }
    }

    protected virtual async Task OnSubmitAsync(StratumConnection connection, Timestamped<JsonRpcRequest> tsRequest, CancellationToken ct)
    {
        var request = tsRequest.Value;
        var context = connection.ContextAs<ApsakWorkerContext>();

        try
        {
            if(request.Id == null)
                throw new StratumException(StratumError.MinusOne, "missing request id");

            // check age of submission (aged submissions are usually caused by high server load)
            var requestAge = clock.Now - tsRequest.Timestamp.UtcDateTime;

            if(requestAge > maxShareAge)
            {
                logger.Warn(() => $"[{connection.ConnectionId}] Dropping stale share submission request (server overloaded?)");
                return;
            }

            // check worker state
            context.LastActivity = clock.Now;

            // validate worker
            if(!context.IsAuthorized)
                throw new StratumException(StratumError.UnauthorizedWorker, "Unauthorized worker");
            else if(!context.IsSubscribed)
                throw new StratumException(StratumError.NotSubscribed, "Not subscribed");
            // check UserAgent
            if (context.UserAgent.StartsWith("lolMiner 1.") &&
                !context.UserAgent.StartsWith("lolMiner 1.89") &&
                !context.UserAgent.StartsWith("lolMiner 1.9"))
            {
                logger.Info(() => $"[{connection.ConnectionId}] Banning worker for [{context.UserAgent}]");
                banManager.Ban(connection.RemoteEndpoint.Address, TimeSpan.FromSeconds(60));
                Disconnect(connection);
                throw new StratumException(StratumError.NotSubscribed, "Unsupported miner version");
            }

            var requestParams = request.ParamsAs<string[]>();

            // submit
            var share = await manager.SubmitShareAsync(connection, requestParams, ct);
            await connection.RespondAsync(true, request.Id);

            // publish
            messageBus.SendMessage(share);

            // telemetry
            PublishTelemetry(TelemetryCategory.Share, clock.Now - tsRequest.Timestamp.UtcDateTime, true);
            logger.Info(() => $"[{connection.ConnectionId}] Share accepted: D={Math.Round(share.Difficulty * ApsakConstants.ShareMultiplier, 3)} [{context.UserAgent}]");

            // update pool stats
            if(share.IsBlockCandidate)
                poolStats.LastPoolBlockTime = clock.Now;

            // update client stats
            context.Stats.ValidShares++;
            await UpdateVarDiffAsync(connection, false, ct);
        }

        catch(StratumException ex)
        {
            // telemetry
            PublishTelemetry(TelemetryCategory.Share, clock.Now - tsRequest.Timestamp.UtcDateTime, false);

            // update client stats
            context.Stats.InvalidShares++;
            logger.Info(() => $"[{connection.ConnectionId}] Share rejected: {ex.Message} [{context.UserAgent}]");

            // banning
            ConsiderBan(connection, context, poolConfig.Banning);

            throw;
        }
    }

    protected virtual async Task OnNewJobAsync(object[] jobParams)
    {
        currentJobParams = jobParams;

        logger.Info(() => $"Broadcasting job {jobParams[0]}");

        await Guard(() => ForEachMinerAsync(async (connection, ct) =>
        {
            var context = connection.ContextAs<ApsakWorkerContext>();

            await SendJob(connection, context, currentJobParams);
        }));
    }

    private async Task SendJob(StratumConnection connection, ApsakWorkerContext context, object[] jobParams)
    {
        object[] jobParamsActual;
        if(context.IsLargeJob)
        {
            jobParamsActual = new object[] {
                jobParams[0],
                jobParams[1],
            };
        }
        else
        {
            jobParamsActual = new object[] {
                jobParams[0],
                jobParams[2],
                jobParams[3],
            };
        }

        // send difficulty
        await connection.NotifyAsync(ApsakStratumMethods.SetDifficulty, new object[] { context.Difficulty });

        // send job
        await connection.NotifyAsync(ApsakStratumMethods.MiningNotify, jobParamsActual);
    }

    public override double HashrateFromShares(double shares, double interval)
    {
        var multiplier = ApsakConstants.Pow2xDiff1TargetNumZero * (double) ApsakConstants.MinHash;
        var result = shares * multiplier / interval;

        return result;
    }

    public override double ShareMultiplier => ApsakConstants.ShareMultiplier;

    #region Overrides

    public override void Configure(PoolConfig pc, ClusterConfig cc)
    {
        coin = pc.Template.As<ApsakCoinTemplate>();
        extraPoolConfig = pc.Extra.SafeExtensionDataAs<ApsakPoolConfigExtra>();

        base.Configure(pc, cc);
    }

    protected override async Task SetupJobManager(CancellationToken ct)
    {
        var extraNonce1Size = extraPoolConfig?.ExtraNonce1Size ?? 2;

        manager = ctx.Resolve<ApsakJobManager>(
            new TypedParameter(typeof(IExtraNonceProvider), new ApsakExtraNonceProvider(poolConfig.Id, extraNonce1Size, clusterConfig.InstanceId)));

        manager.Configure(poolConfig, clusterConfig);

        await manager.StartAsync(ct);

        if(poolConfig.EnableInternalStratum == true)
        {
            disposables.Add(manager.Jobs
                .Select(job => Observable.FromAsync(() =>
                    Guard(()=> OnNewJobAsync(job),
                        ex=> logger.Debug(() => $"{nameof(OnNewJobAsync)}: {ex.Message}"))))
                .Concat()
                .Subscribe(_ => { }, ex =>
                {
                    logger.Debug(ex, nameof(OnNewJobAsync));
                }));

            // start with initial blocktemplate
            await manager.Jobs.Take(1).ToTask(ct);
        }

        else
        {
            // keep updating NetworkStats
            disposables.Add(manager.Jobs.Subscribe());
        }
    }

    protected override async Task InitStatsAsync(CancellationToken ct)
    {
        await base.InitStatsAsync(ct);

        blockchainStats = manager.BlockchainStats;
    }

    protected override WorkerContextBase CreateWorkerContext()
    {
        return new ApsakWorkerContext();
    }

    protected override async Task OnRequestAsync(StratumConnection connection,
        Timestamped<JsonRpcRequest> tsRequest, CancellationToken ct)
    {
        var request = tsRequest.Value;

        try
        {
            switch(request.Method)
            {
                case ApsakStratumMethods.Subscribe:
                    await OnSubscribeAsync(connection, tsRequest, ct);
                    break;

                case ApsakStratumMethods.ExtraNonceSubscribe:
                    var context = connection.ContextAs<ApsakWorkerContext>();

                    var data = new object[]
                    {
                        context.ExtraNonce1,
                        ApsakConstants.ExtranoncePlaceHolderLength - manager.GetExtraNonce1Size(),
                    };

                    await connection.NotifyAsync(ApsakStratumMethods.SetExtraNonce, data);
                    break;

                case ApsakStratumMethods.Authorize:
                    await OnAuthorizeAsync(connection, tsRequest, ct);
                    break;

                case ApsakStratumMethods.SubmitShare:
                    await OnSubmitAsync(connection, tsRequest, ct);
                    break;

                default:
                    logger.Debug(() => $"[{connection.ConnectionId}] Unsupported RPC request: {JsonConvert.SerializeObject(request, serializerSettings)}");

                    await connection.RespondErrorAsync(StratumError.Other, $"Unsupported request {request.Method}", request.Id);
                    break;
            }
        }

        catch(StratumException ex)
        {
            await connection.RespondErrorAsync(ex.Code, ex.Message, request.Id, false);
        }
    }

    protected override async Task<double?> GetNicehashStaticMinDiff(WorkerContextBase context, string coinName, string algoName)
    {
        var result = await base.GetNicehashStaticMinDiff(context, coinName, algoName);

        // adjust value to fit with our target value calculation
        if(result.HasValue)
            result = result.Value / uint.MaxValue;

        return result;
    }

    protected override async Task OnVarDiffUpdateAsync(StratumConnection connection, double newDiff, CancellationToken ct)
    {
        await base.OnVarDiffUpdateAsync(connection, newDiff, ct);

        var context = connection.ContextAs<ApsakWorkerContext>();

        if(context.ApplyPendingDifficulty())
        {
            // send job
            await SendJob(connection, context, currentJobParams);
        }
    }

    #endregion // Overrides
}
