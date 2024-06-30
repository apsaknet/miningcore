using Miningcore.Contracts;
using Miningcore.Crypto;
using Miningcore.Crypto.Hashing.Algorithms;

namespace Miningcore.Blockchain.Apsak.Custom.Karlsencoin;

public class KarlsencoinJob : ApsakJob
{
    public KarlsencoinJob(IHashAlgorithm customBlockHeaderHasher, IHashAlgorithm customCoinbaseHasher, IHashAlgorithm customShareHasher) : base(customBlockHeaderHasher, customCoinbaseHasher, customShareHasher)
    {
    }
}
