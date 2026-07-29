using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Ahir.Security.Encryption;

public sealed class Aes256GcmEngine : IDisposable
{
    private readonly byte[] _key;
    private bool _disposed;

    public Aes256GcmEngine(byte[] key)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes (256 bits).", nameof(key));
    }

    public byte[] Encrypt(byte[] plaintext, byte[]? associatedData = null)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        var result = new byte[12 + 16 + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, 12);
        ciphertext.CopyTo(result, 28);

        return result;
    }

    public byte[] Decrypt(byte[] encryptedData, byte[]? associatedData = null)
    {
        if (encryptedData.Length < 28)
            throw new ArgumentException("Invalid encrypted data.");

        var nonce = encryptedData[..12];
        var tag = encryptedData[12..28];
        var ciphertext = encryptedData[28..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

        return plaintext;
    }

    public byte[] EncryptString(string plaintext, byte[]? associatedData = null)
    {
        return Encrypt(System.Text.Encoding.UTF8.GetBytes(plaintext), associatedData);
    }

    public string DecryptToString(byte[] encryptedData, byte[]? associatedData = null)
    {
        return System.Text.Encoding.UTF8.GetString(Decrypt(encryptedData, associatedData));
    }

    public static byte[] GenerateKey()
    {
        return RandomNumberGenerator.GetBytes(32);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CryptographicOperations.ZeroMemory(_key);
    }
}