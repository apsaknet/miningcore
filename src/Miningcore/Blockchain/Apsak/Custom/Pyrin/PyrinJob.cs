using System.Text;
using Miningcore.Contracts;
using Miningcore.Crypto;
using Miningcore.Crypto.Hashing.Algorithms;
using Miningcore.Extensions;

namespace Miningcore.Blockchain.Apsak.Custom.Pyrin;

public class PyrinJob : ApsakJob
{
    public PyrinJob(IHashAlgorithm customBlockHeaderHasher, IHashAlgorithm customCoinbaseHasher, IHashAlgorithm customShareHasher) : base(customBlockHeaderHasher, customCoinbaseHasher, customShareHasher)
    {
    }
}
