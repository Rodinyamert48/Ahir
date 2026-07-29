using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Ahir.Database.Cache;

internal sealed class BloomFilter
{
    private readonly BitArray _bits;
    private readonly int _hashCount;
    private readonly int _size;

    public BloomFilter(int size, double falsePositiveRate = 0.01)
    {
        _size = size;
        _hashCount = (int)Math.Ceiling(Math.Log(2) * _size / Math.Log(1 / falsePositiveRate));
        _bits = new BitArray(size);
    }

    public BloomFilter(byte[] data, int hashCount)
    {
        _size = data.Length * 8;
        _hashCount = hashCount;
        _bits = new BitArray(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ReadOnlySpan<byte> key)
    {
        Span<byte> hashBuffer = stackalloc byte[64];
        SHA512.HashData(key, hashBuffer);

        for (var i = 0; i < _hashCount; i++)
        {
            var index = Math.Abs(BitConverter.ToInt32(hashBuffer[(i * 4)..])) % _size;
            _bits[index] = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ReadOnlySpan<byte> key)
    {
        Span<byte> hashBuffer = stackalloc byte[64];
        SHA512.HashData(key, hashBuffer);

        for (var i = 0; i < _hashCount; i++)
        {
            var index = Math.Abs(BitConverter.ToInt32(hashBuffer[(i * 4)..])) % _size;
            if (!_bits[index])
                return false;
        }
        return true;
    }

    public byte[] Serialize()
    {
        var bytes = new byte[_bits.Length / 8 + 1];
        _bits.CopyTo(bytes, 0);
        return bytes;
    }

    public void Clear()
    {
        _bits.SetAll(false);
    }
}

internal sealed class BitArray
{
    private readonly int[] _bits;
    private readonly int _length;

    public BitArray(int length)
    {
        _length = length;
        _bits = new int[(length + 31) / 32];
    }

    public BitArray(byte[] bytes)
    {
        _length = bytes.Length * 8;
        _bits = new int[(bytes.Length + 3) / 4];
        Buffer.BlockCopy(bytes, 0, _bits, 0, bytes.Length);
    }

    public bool this[int index]
    {
        get => (_bits[index / 32] & (1 << (index % 32))) != 0;
        set
        {
            if (value)
                _bits[index / 32] |= 1 << (index % 32);
            else
                _bits[index / 32] &= ~(1 << (index % 32));
        }
    }

    public int Length => _length;

    public void SetAll(bool value)
    {
        var fill = value ? -1 : 0;
        Array.Fill(_bits, fill);
    }

    public void CopyTo(byte[] array, int startIndex)
    {
        Buffer.BlockCopy(_bits, 0, array, startIndex, Math.Min(array.Length - startIndex, _bits.Length * 4));
    }
}