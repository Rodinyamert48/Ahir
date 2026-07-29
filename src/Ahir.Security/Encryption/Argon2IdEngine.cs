using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Ahir.Security.Encryption;

public sealed class Argon2IdEngine
{
    private readonly int _memorySize;
    private readonly int _iterations;
    private readonly int _parallelism;
    private readonly int _hashLength;

    public Argon2IdEngine(int memorySize = 65536, int iterations = 3, int parallelism = 4, int hashLength = 64)
    {
        _memorySize = memorySize;
        _iterations = iterations;
        _parallelism = parallelism;
        _hashLength = hashLength;
    }

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = ComputeHash(password, salt);

        var result = new byte[1 + 4 + 4 + 4 + 32 + hash.Length];
        result[0] = 1; // version

        BitConverter.TryWriteBytes(result.AsSpan(1, 4), _memorySize);
        BitConverter.TryWriteBytes(result.AsSpan(5, 4), _iterations);
        BitConverter.TryWriteBytes(result.AsSpan(9, 4), _parallelism);

        salt.CopyTo(result, 13);
        hash.CopyTo(result, 45);

        return Convert.ToBase64String(result);
    }

    public bool VerifyPassword(string password, string hashString)
    {
        var data = Convert.FromBase64String(hashString);
        if (data.Length < 45)
            return false;

        var version = data[0];
        var memorySize = BitConverter.ToInt32(data.AsSpan(1, 4));
        var iterations = BitConverter.ToInt32(data.AsSpan(5, 4));
        var parallelism = BitConverter.ToInt32(data.AsSpan(9, 4));

        var salt = data[13..45];
        var expectedHash = data[45..];

        var actualHash = ComputeHash(password, salt, memorySize, iterations, parallelism, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private byte[] ComputeHash(string password, byte[] salt)
    {
        return ComputeHash(password, salt, _memorySize, _iterations, _parallelism, _hashLength);
    }

    private static byte[] ComputeHash(string password, byte[] salt, int memorySize, int iterations, int parallelism, int hashLength)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySize,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };

        return argon2.GetBytes(hashLength);
    }
}