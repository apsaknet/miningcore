using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Miningcore.Crypto;
using Miningcore.Crypto.Hashing.Algorithms;
using Miningcore.Util;
using NBitcoin;

namespace Miningcore.Blockchain.Apsak;

public static class ApsakUtils
{
    public static (ApsakAddressUtility, Exception) ValidateAddress(string address, string network, string coinSymbol = "SAK")
    {
        if(string.IsNullOrEmpty(address))
            return (null, new ArgumentException($"Empty address..."));

        ApsakBech32Prefix networkBech32Prefix;

        switch(network.ToLower())
        {
            case "devnet":
                networkBech32Prefix = ApsakBech32Prefix.ApsakDev;

                break;
            case "simnet":
                networkBech32Prefix = ApsakBech32Prefix.ApsakSim;

                break;
            case "testnet":
                networkBech32Prefix = ApsakBech32Prefix.ApsakTest;

                break;
            default:
                networkBech32Prefix = ApsakBech32Prefix.ApsakMain;

                break;
        }

        try
        {
            var apsakAddressUtility = new ApsakAddressUtility(coinSymbol);
            apsakAddressUtility.DecodeAddress(address, networkBech32Prefix);

            return (apsakAddressUtility, null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    public static BigInteger DifficultyToTarget(double difficulty)
    {
        return BigInteger.Divide(ApsakConstants.Diff1Target, new BigInteger(difficulty));
    }

    public static BigInteger CalculateTarget(uint bits)
    {
        (uint mant, int expt) result;

        uint unshiftedExpt = bits >> 24;
        if (unshiftedExpt <= 3)
        {
            result.mant = (bits & 0xFFFFFF) >> (8 * (3 - (int)unshiftedExpt));
            result.expt = 0;
        }
        else
        {
            result.mant = bits & 0xFFFFFF;
            result.expt = 8 * ((int)(bits >> 24) - 3);
        }

        // The mantissa is signed but may not be negative
        if (result.mant > 0x7FFFFF)
        {
            return BigInteger.Zero;
        }
        else
        {
            return BigInteger.Pow(result.mant, result.expt);
        }
    }

    public static double TargetToDifficulty(BigInteger target)
    {
        return (double) new BigRational(ApsakConstants.Diff1Target, target);
    }

    public static double DifficultyToHashrate(double diff)
    {
        return (double) new BigRational(BigInteger.Multiply(BigInteger.Multiply(ApsakConstants.MinHash, ApsakConstants.BigGig), new BigInteger(diff)), ApsakConstants.Diff1);
    }

    public static double BigDiffToLittle(BigInteger diff)
    {
        BigInteger numerator = new BigInteger(2);
        numerator = numerator << 254;

        BigInteger final = BigInteger.Divide(numerator, diff);

        BigInteger tempA = BigInteger.Pow(2, 30);
        final = BigInteger.Divide(final, tempA);

        return (double) final;
    }

    public static BigInteger CompactToBig(uint compact)
    {
        uint mantissa = compact & 0x007FFFFF;
        bool isNegative = (compact & 0x00800000) != 0;
        uint exponent = compact >> 24;

        BigInteger result;

        if (exponent <= 3)
        {
            mantissa >>= (int)(8 * (3 - exponent));
            result = new BigInteger(mantissa);
        }
        else
        {
            result = new BigInteger(mantissa);
            result <<= (int)(8 * (exponent - 3));
        }

        if (isNegative)
        {
            result = BigInteger.Negate(result);
        }

        return result;
    }

    public static uint BigToCompact(BigInteger n)
    {
        if (n.Sign == 0)
        {
            return 0;
        }

        int exponent = n.ToByteArray().Length;
        uint mantissa;

        if (exponent <= 3)
        {
            mantissa = (uint)n;
            mantissa <<= (8 * (3 - exponent));
        }
        else
        {
            BigInteger tmp = BigInteger.Divide(n, BigInteger.Pow(256, exponent - 3));
            mantissa = (uint)tmp;
        }

        if ((mantissa & 0x00800000) != 0)
        {
            mantissa >>= 8;
            exponent++;
        }

        uint compact = (uint)(exponent << 24) | mantissa;

        if (n.Sign < 0)
        {
            compact |= 0x00800000;
        }

        return compact;
    }

    public static double CalcWork(uint bits)
    {
        BigInteger difficultyNum = CompactToBig(bits);

        if (difficultyNum.Sign <= 0)
            return (double) BigInteger.Zero;

        return (double) new BigRational(ApsakConstants.OneLsh256, BigInteger.Add(difficultyNum, ApsakConstants.BigOne));
    }

    public static byte[] HashBlake2b(byte[] serializedScript)
    {
        IHashAlgorithm scriptHasher = new Blake2b();
        Span<byte> hashBytes = stackalloc byte[32];
        scriptHasher.Digest(serializedScript, hashBytes);

        return hashBytes.ToArray();
    }
}

public interface ApsakIAddress
{
    string EncodeAddress();
    byte[] ScriptAddress();
    ApsakBech32Prefix Prefix();
    byte Version();
    bool IsForPrefix(ApsakBech32Prefix prefix);
}

public class ApsakAddressPublicKey : ApsakIAddress
{
    public byte version { get; private set; } = ApsakConstants.PubKeyAddrID;
    private readonly ApsakBech32Prefix prefix;
    private readonly byte[] publicKey;

    public ApsakAddressPublicKey(byte[] publicKey, ApsakBech32Prefix prefix)
    {
        if (publicKey.Length != ApsakConstants.PublicKeySize)
            throw new ArgumentException($"Public key must be {ApsakConstants.PublicKeySize} bytes", nameof(publicKey));

        this.prefix = prefix;
        this.publicKey = publicKey.ToArray();
    }

    public string EncodeAddress()
    {
        return ApsakBech32.Encode(prefix.ToString(), publicKey, version);
    }

    public byte[] ScriptAddress()
    {
        return publicKey.ToArray();
    }

    public ApsakBech32Prefix Prefix()
    {
        return prefix;
    }

    public byte Version()
    {
        return version;
    }

    public bool IsForPrefix(ApsakBech32Prefix prefix)
    {
        return this.prefix == prefix;
    }

    public override string ToString()
    {
        return EncodeAddress();
    }
}

public class ApsakAddressPublicKeyECDSA : ApsakIAddress
{
    public byte version { get; private set; } = ApsakConstants.PubKeyECDSAAddrID;
    private readonly ApsakBech32Prefix prefix;
    private readonly byte[] publicKey;

    public ApsakAddressPublicKeyECDSA(byte[] publicKey, ApsakBech32Prefix prefix)
    {
        if (publicKey.Length != ApsakConstants.PublicKeySizeECDSA)
            throw new ArgumentException($"Public key must be {ApsakConstants.PublicKeySizeECDSA} bytes", nameof(publicKey));

        this.prefix = prefix;
        this.publicKey = publicKey.ToArray();
    }

    public string EncodeAddress()
    {
        return ApsakBech32.Encode(prefix.ToString(), publicKey, version);
    }

    public byte[] ScriptAddress()
    {
        return publicKey.ToArray();
    }

    public ApsakBech32Prefix Prefix()
    {
        return prefix;
    }

    public byte Version()
    {
        return version;
    }

    public bool IsForPrefix(ApsakBech32Prefix prefix)
    {
        return this.prefix == prefix;
    }

    public override string ToString()
    {
        return EncodeAddress();
    }
}

public class ApsakAddressScriptHash : ApsakIAddress
{
    public byte version { get; private set; } = ApsakConstants.ScriptHashAddrID;
    private readonly ApsakBech32Prefix prefix;
    private readonly byte[] hash;

    public ApsakAddressScriptHash(byte[] serializedScript, ApsakBech32Prefix prefix)
    {
        var scriptHash = ApsakUtils.HashBlake2b(serializedScript);
        if (scriptHash.Length != ApsakConstants.Blake2bSize256)
            throw new ArgumentException($"Script hash must be {ApsakConstants.Blake2bSize256} bytes", nameof(scriptHash));

        this.prefix = prefix;
        this.hash = scriptHash.ToArray();
    }

    public string EncodeAddress()
    {
        return ApsakBech32.Encode(prefix.ToString(), hash, version);
    }

    public byte[] ScriptAddress()
    {
        return hash.ToArray();
    }

    public ApsakBech32Prefix Prefix()
    {
        return prefix;
    }

    public byte Version()
    {
        return version;
    }

    public bool IsForPrefix(ApsakBech32Prefix prefix)
    {
        return this.prefix == prefix;
    }

    public override string ToString()
    {
        return EncodeAddress();
    }
}

public class ApsakAddressUtility
{
    public ApsakIAddress ApsakAddress { get; private set; }
    public string CoinSymbol { get; private set; }

    public ApsakAddressUtility(string coinSymbol = "SAK")
    {
        this.CoinSymbol = coinSymbol;

        // Build address pattern based on network type and coin symbol
        switch(this.CoinSymbol)
        {
            case "BGA":
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { BugnaConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { BugnaConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { BugnaConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { BugnaConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
            case "CAS":
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { KaspaClassicConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { KaspaClassicConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { KaspaClassicConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { KaspaClassicConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
            case "HTN":
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { HoosatConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { HoosatConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { HoosatConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { HoosatConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
            case "KLS":
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { KarlsencoinConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { KarlsencoinConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { KarlsencoinConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { KarlsencoinConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
            case "NTL":
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { NautilusConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { NautilusConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { NautilusConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { NautilusConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
            case "NXL":
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { NexelliaConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { NexelliaConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { NexelliaConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { NexelliaConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
            case "PYI":
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { PyrinConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { PyrinConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { PyrinConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { PyrinConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
            case "SDR":
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { SedraCoinConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { SedraCoinConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { SedraCoinConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { SedraCoinConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
            default:
                this.stringsToBech32Prefixes = new Dictionary<string, ApsakBech32Prefix>
                {
                    { ApsakConstants.ChainPrefixMainnet, ApsakBech32Prefix.ApsakMain },
                    { ApsakConstants.ChainPrefixDevnet, ApsakBech32Prefix.ApsakDev },
                    { ApsakConstants.ChainPrefixTestnet, ApsakBech32Prefix.ApsakTest },
                    { ApsakConstants.ChainPrefixSimnet, ApsakBech32Prefix.ApsakSim },
                };

                break;
        }
    }

    public string EncodeAddress(ApsakBech32Prefix prefix, byte[] payload, byte version)
    {
        return ApsakBech32.Encode(PrefixToString(prefix), payload, version);
    }

    public void DecodeAddress(string addr, ApsakBech32Prefix expectedPrefix)
    {
        var (prefixString, decoded, version, error) = ApsakBech32.Decode(addr);
        if (error != null)
            throw new ArgumentException($"Decoded address is of unknown format: {error}");

        var prefix = ParsePrefix(prefixString);
        if (expectedPrefix != ApsakBech32Prefix.Unknown && expectedPrefix != prefix)
            throw new ArgumentException($"Decoded address is of wrong network. Expected {expectedPrefix.ToString()} but got {prefix.ToString()}");

        switch (version)
        {
            case ApsakConstants.PubKeyAddrID:
                this.ApsakAddress = new ApsakAddressPublicKey(decoded, prefix);

                break;
            case ApsakConstants.PubKeyECDSAAddrID:
                this.ApsakAddress = new ApsakAddressPublicKeyECDSA(decoded, prefix);

                break;
            case ApsakConstants.ScriptHashAddrID:
                this.ApsakAddress = new ApsakAddressScriptHash(ApsakUtils.HashBlake2b(decoded), prefix);

                break;
            default:
                throw new InvalidOperationException("Unknown address type");
        }
    }

    public ApsakBech32Prefix ParsePrefix(string prefixString)
    {
        if (!stringsToBech32Prefixes.TryGetValue(prefixString, out var prefix))
            throw new ArgumentException($"Could not parse prefix {prefixString}");

        return prefix;
    }

    public string PrefixToString(ApsakBech32Prefix prefix)
    {
        foreach (var (key, value) in stringsToBech32Prefixes)
        {
            if (prefix == value)
                return key;
        }

        return string.Empty;
    }

    private Dictionary<string, ApsakBech32Prefix> stringsToBech32Prefixes;
}

public static class ApsakBech32
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
    private const int ChecksumLength = 8;

    private class ConversionType
    {
        public byte FromBits { get; set; }
        public byte ToBits { get; set; }
        public bool Pad { get; set; }
    }

    private static readonly ConversionType FiveToEightBits = new ConversionType { FromBits = 5, ToBits = 8, Pad = false };
    private static readonly ConversionType EightToFiveBits = new ConversionType { FromBits = 8, ToBits = 5, Pad = true };

    private static readonly long[] Generator = { 0x98f2bc8e61, 0x79b76d99e2, 0xf33e5fb3c4, 0xae2eabe2a8, 0x1e4f43e470 };

    public static string Encode(string prefix, byte[] payload, byte version)
    {
        byte[] data = new byte[payload.Length + 1];
        data[0] = version;
        Array.Copy(payload, 0, data, 1, payload.Length);

        byte[] converted = ConvertBits(data, EightToFiveBits);

        return EncodeInternal(prefix, converted);
    }

    public static (string, byte[], byte, Exception) Decode(string encoded)
    {
        try
        {
            var (prefix, decoded) = DecodeInternal(encoded);
            var converted = ConvertBits(decoded, FiveToEightBits);
            var version = converted[0];
            var payload = converted.Skip(1).ToArray();
            return (prefix, payload, version, null);
        }
        catch (Exception ex)
        {
            return (null, null, 0, ex);
        }
    }

    private static string EncodeInternal(string prefix, byte[] data)
    {
        byte[] checksum = CalculateChecksum(prefix, data);
        byte[] combined = data.Concat(checksum).ToArray();

        string base32String = EncodeToBase32(combined);

        return $"{prefix}:{base32String}";
    }

    private static (string, byte[]) DecodeInternal(string encoded)
    {
        if (encoded.Length < ChecksumLength + 2)
            throw new Exception($"Invalid bech32 string length {encoded.Length}");

        foreach (char c in encoded)
        {
            if (c < 33 || c > 126)
                throw new Exception($"Invalid character in string: '{c}'");
        }

        string lower = encoded.ToLower();
        string upper = encoded.ToUpper();

        if (encoded != lower && encoded != upper)
            throw new Exception("String not all lowercase or all uppercase");

        encoded = lower;

        int colonIndex = encoded.LastIndexOf(':');
        if (colonIndex < 1 || colonIndex + ChecksumLength + 1 > encoded.Length)
            throw new Exception("Invalid index of ':'");

        string prefix = encoded.Substring(0, colonIndex);
        string data = encoded.Substring(colonIndex + 1);

        byte[] decoded = DecodeFromBase32(data);

        if (!VerifyChecksum(prefix, decoded))
        {
            string checksum = encoded.Substring(encoded.Length - ChecksumLength);
            string expected = EncodeToBase32(CalculateChecksum(prefix, decoded.Take(decoded.Length - ChecksumLength).ToArray()));

            throw new Exception($"Checksum failed. Expected {expected}, got {checksum}");
        }

        return (prefix, decoded.Take(decoded.Length - ChecksumLength).ToArray());
    }

    private static byte[] DecodeFromBase32(string base32String)
    {
        List<byte> decoded = new List<byte>(base32String.Length);
        foreach (char c in base32String)
        {
            int index = Charset.IndexOf(c);
            if (index < 0)
                throw new Exception($"Invalid character not part of charset: {c}");

            decoded.Add((byte)index);
        }
        return decoded.ToArray();
    }

    private static string EncodeToBase32(byte[] data)
    {
        StringBuilder result = new StringBuilder(data.Length);
        foreach (byte b in data)
        {
            if (b >= Charset.Length)
                return "";

            result.Append(Charset[b]);
        }
        return result.ToString();
    }

    private static byte[] ConvertBits(byte[] data, ConversionType conversionType)
    {
        List<byte> regrouped = new List<byte>();
        byte nextByte = 0;
        byte filledBits = 0;

        foreach (byte b in data)
        {
            byte shiftedB = (byte)(b << (8 - conversionType.FromBits));
            byte remainingFromBits = conversionType.FromBits;

            while (remainingFromBits > 0)
            {
                byte remainingToBits = (byte)(conversionType.ToBits - filledBits);
                byte toExtract = remainingFromBits < remainingToBits ? remainingFromBits : remainingToBits;

                nextByte = (byte)((nextByte << toExtract) | (shiftedB >> (8 - toExtract)));

                shiftedB = (byte)(shiftedB << toExtract);
                remainingFromBits -= toExtract;
                filledBits += toExtract;

                if (filledBits == conversionType.ToBits)
                {
                    regrouped.Add(nextByte);
                    filledBits = 0;
                    nextByte = 0;
                }
            }
        }

        if (conversionType.Pad && filledBits > 0)
        {
            nextByte = (byte)(nextByte << (conversionType.ToBits - filledBits));
            regrouped.Add(nextByte);
        }

        return regrouped.ToArray();
    }

    private static byte[] CalculateChecksum(string prefix, byte[] payload)
    {
        int[] prefixLower5Bits = PrefixToUint5Array(prefix);
        int[] payloadInts = PayloadToInts(payload);
        int[] templateZeroes = { 0, 0, 0, 0, 0, 0, 0, 0 };

        int[] concat = prefixLower5Bits.Concat(new[] { 0 }).Concat(payloadInts).Concat(templateZeroes).ToArray();
        long polyModResult = PolyMod(concat);

        byte[] res = new byte[ChecksumLength];
        for (int i = 0; i < ChecksumLength; i++)
        {
            res[i] = (byte)((polyModResult >> (5 * (ChecksumLength - 1 - i))) & 31);
        }

        return res;
    }

    private static bool VerifyChecksum(string prefix, byte[] payload)
    {
        int[] prefixLower5Bits = PrefixToUint5Array(prefix);
        int[] payloadInts = PayloadToInts(payload);

        int[] dataToVerify = prefixLower5Bits.Concat(new[] { 0 }).Concat(payloadInts).ToArray();
        return PolyMod(dataToVerify) == 0;
    }

    private static int[] PrefixToUint5Array(string prefix)
    {
        int[] prefixLower5Bits = new int[prefix.Length];
        for (int i = 0; i < prefix.Length; i++)
        {
            char c = prefix[i];
            int charLower5Bits = c & 31;
            prefixLower5Bits[i] = charLower5Bits;
        }

        return prefixLower5Bits;
    }

    private static int[] PayloadToInts(byte[] payload)
    {
        return payload.Select(b => (int)b).ToArray();
    }

    private static long PolyMod(int[] values)
    {
        long checksum = 1;
        foreach (int value in values)
        {
            long topBits = checksum >> 35;
            checksum = ((checksum & 0x07ffffffff) << 5) ^ value;

            for (int i = 0; i < Generator.Length; i++)
            {
                if (((topBits >> i) & 1) == 1)
                    checksum ^= Generator[i];
            }
        }

        return checksum ^ 1;
    }
}
