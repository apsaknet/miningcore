using System.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using System.Net;
using System.Threading.Tasks;
using Miningcore.Blockchain.Apsak.Configuration;
using Miningcore.Configuration;
using Miningcore.Extensions;
using Miningcore.Mining;
using NLog;
using apsakWalletd = Miningcore.Blockchain.Apsak.ApsakWalletd;
using apsakd = Miningcore.Blockchain.Apsak.Apsakd;

namespace Miningcore.Blockchain.Apsak;

public static class ApsakClientFactory
{
    public static apsakd.ApsakdRPC.ApsakdRPCClient CreateApsakdRPCClient(DaemonEndpointConfig[] daemonEndpoints, string protobufDaemonRpcServiceName)
    {
        var daemonEndpoint = daemonEndpoints.First();

        var baseUrl = new UriBuilder(daemonEndpoint.Ssl || daemonEndpoint.Http2 ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
            daemonEndpoint.Host, daemonEndpoint.Port, daemonEndpoint.HttpPath);
        
        var channel = GrpcChannel.ForAddress(baseUrl.ToString(), new GrpcChannelOptions()
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            },
            /*
             * The following options are not "perfectly" optimized, you can experiment but these values seem the more trouble free
             * Tweak at your own risk
             * https://learn.microsoft.com/en-us/aspnet/core/grpc/configuration?view=aspnetcore-6.0
             * https://grpc.github.io/grpc/csharp-dotnet/api/Grpc.Net.Client.GrpcChannelOptions.html
             */
            DisposeHttpClient = true,
            MaxReceiveMessageSize = 2097152, // 2MB
            MaxSendMessageSize = 2097152 // 2MB
        });

        return new apsakd.ApsakdRPC.ApsakdRPCClient(new apsakd.ApsakdRPC(protobufDaemonRpcServiceName), channel);
    }

        public static apsakWalletd.ApsakWalletdRPC.ApsakWalletdRPCClient CreateApsakWalletdRPCClient(DaemonEndpointConfig[] daemonEndpoints, string protobufWalletRpcServiceName)
    {
        var daemonEndpoint = daemonEndpoints.First();

        var baseUrl = new UriBuilder(daemonEndpoint.Ssl || daemonEndpoint.Http2 ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
            daemonEndpoint.Host, daemonEndpoint.Port, daemonEndpoint.HttpPath);
        
        var channel = GrpcChannel.ForAddress(baseUrl.ToString(), new GrpcChannelOptions()
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            },
            /*
             * The following options are not "perfectly" optimized, you can experiment but these values seem the more trouble free
             * Tweak at your own risk
             * https://learn.microsoft.com/en-us/aspnet/core/grpc/configuration?view=aspnetcore-6.0
             * https://grpc.github.io/grpc/csharp-dotnet/api/Grpc.Net.Client.GrpcChannelOptions.html
             */
            DisposeHttpClient = true,
            MaxReceiveMessageSize = 2097152, // 2MB
            MaxSendMessageSize = 2097152 // 2MB
        });

        return new apsakWalletd.ApsakWalletdRPC.ApsakWalletdRPCClient(new apsakWalletd.ApsakWalletdRPC(protobufWalletRpcServiceName), channel);
    }
}