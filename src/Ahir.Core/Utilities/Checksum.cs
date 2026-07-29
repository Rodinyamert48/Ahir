using System.Security.Cryptography;

namespace Ahir.Core.Utilities;

public static class Checksum
{
    public static string Compute(ReadOnlySpan<byte> data)
    {
        var hash = SHA512.HashData(data);
        return Convert.ToHexStringLower(hash);
    }

    public static string Compute(Stream stream)
    {
        using var sha512 = SHA512.Create();
        var hash = sha512.ComputeHash(stream);
        return Convert.ToHexStringLower(hash);
    }

    public static async Task<string> ComputeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var sha512 = SHA512.Create();
        var hash = await sha512.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }

    public static bool Verify(ReadOnlySpan<byte> data, string checksum)
    {
        var computed = Compute(data);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(computed),
            Convert.FromHexString(checksum));
    }

    public static string ComputeFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Compute(stream);
    }

    public static async Task<string> ComputeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await ComputeAsync(stream, cancellationToken);
    }

    public static string ComputeCrc32(ReadOnlySpan<byte> data)
    {
        var crc = System.IO.Hashing.Crc32.Hash(data);
        return Convert.ToHexStringLower(crc);
    }
}