using System.Security.Cryptography;

namespace Packer;

internal sealed record DownloadDigest(HashAlgorithmName Algorithm, string Value)
{
    public static DownloadDigest FromBytes(HashAlgorithmName algorithm, byte[] value)
    {
        int expected = GetByteLength(algorithm);

        if (value.Length != expected)
        {
            throw new FormatException($"A {algorithm} digest is {expected} bytes, not {value.Length}.");
        }

        return new DownloadDigest(algorithm, Convert.ToHexStringLower(value));
    }

    public static DownloadDigest FromBase64(HashAlgorithmName algorithm, string encoded)
    {
        return FromBytes(algorithm, Convert.FromBase64String(encoded));
    }

    public static DownloadDigest FromHex(HashAlgorithmName algorithm, string encoded)
    {
        return FromBytes(algorithm, Convert.FromHexString(encoded));
    }

    public async Task<bool> MatchesAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);

        byte[] actual = await CryptographicOperations.HashDataAsync(Algorithm, stream, cancellationToken);

        return Convert.ToHexStringLower(actual) == Value;
    }

    private static int GetByteLength(HashAlgorithmName algorithm)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(algorithm);

        return hash.HashLengthInBytes;
    }
}
