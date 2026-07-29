using System.Security.Cryptography;
using System.Text;

namespace Ahir.Core.Utilities;

public static class IdGenerator
{
    private static readonly char[] s_chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static long _lastTimestamp;
    private static long _sequence;

    public static string NewId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sequence = Interlocked.Increment(ref _sequence);

        if (timestamp <= _lastTimestamp)
            timestamp = Interlocked.Exchange(ref _lastTimestamp, timestamp) + 1;
        else
            Interlocked.Exchange(ref _lastTimestamp, timestamp);

        Span<byte> buffer = stackalloc byte[16];
        var timeBytes = BitConverter.GetBytes(timestamp);
        var seqBytes = BitConverter.GetBytes(sequence);
        var randomBytes = RandomNumberGenerator.GetBytes(4);

        timeBytes.CopyTo(buffer);
        seqBytes.CopyTo(buffer[8..]);
        randomBytes.CopyTo(buffer[12..]);

        if (BitConverter.IsLittleEndian)
        {
            buffer[0] ^= buffer[7];
            buffer[1] ^= buffer[6];
        }

        return ToBase62(buffer);
    }

    public static string NewShortId()
    {
        var bytes = RandomNumberGenerator.GetBytes(8);
        return ToBase62(bytes);
    }

    public static string NewSecret(int length = 64)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Convert.ToHexStringLower(bytes);
    }

    public static string NewApiKey()
    {
        var prefix = "ahir_"u8;
        var random = RandomNumberGenerator.GetBytes(32);
        Span<byte> combined = stackalloc byte[prefix.Length + random.Length];
        prefix.CopyTo(combined);
        random.CopyTo(combined[prefix.Length..]);
        return Convert.ToHexStringLower(combined);
    }

    private static string ToBase62(ReadOnlySpan<byte> data)
    {
        var value = new System.Numerics.BigInteger(data, isUnsigned: true, isBigEndian: false);
        if (value == 0) return s_chars[0].ToString();

        var result = new StringBuilder();
        var divisor = 62;
        while (value > 0)
        {
            value = System.Numerics.BigInteger.DivRem(value, divisor, out var remainder);
            result.Insert(0, s_chars[(int)remainder]);
        }

        return result.ToString();
    }
}