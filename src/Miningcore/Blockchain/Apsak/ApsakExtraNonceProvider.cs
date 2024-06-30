namespace Miningcore.Blockchain.Apsak;

public class ApsakExtraNonceProvider : ExtraNonceProviderBase
{
    public ApsakExtraNonceProvider(string poolId, int size, byte? clusterInstanceId) : base(poolId, size, clusterInstanceId)
    {
    }
}