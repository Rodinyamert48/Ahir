using System.Text;

namespace Ahir.Core.Utilities;

public static class ByteConverter
{
    public static byte[] ToBytes<T>(T value) where T : struct
    {
        return value switch
        {
            int i => BitConverter.GetBytes(i),
            long l => BitConverter.GetBytes(l),
            short s => BitConverter.GetBytes(s),
            ushort us => BitConverter.GetBytes(us),
            uint ui => BitConverter.GetBytes(ui),
            ulong ul => BitConverter.GetBytes(ul),
            float f => BitConverter.GetBytes(f),
            double d => BitConverter.GetBytes(d),
            bool b => BitConverter.GetBytes(b),
            char c => BitConverter.GetBytes(c),
            _ => throw new ArgumentException($"Unsupported type: {typeof(T).Name}")
        };
    }

    public static T FromBytes<T>(byte[] bytes, int startIndex = 0) where T : struct
    {
        return typeof(T) switch
        {
            var t when t == typeof(int) => (T)(object)BitConverter.ToInt32(bytes, startIndex),
            var t when t == typeof(long) => (T)(object)BitConverter.ToInt64(bytes, startIndex),
            var t when t == typeof(short) => (T)(object)BitConverter.ToInt16(bytes, startIndex),
            var t when t == typeof(ushort) => (T)(object)BitConverter.ToUInt16(bytes, startIndex),
            var t when t == typeof(uint) => (T)(object)BitConverter.ToUInt32(bytes, startIndex),
            var t when t == typeof(ulong) => (T)(object)BitConverter.ToUInt64(bytes, startIndex),
            var t when t == typeof(float) => (T)(object)BitConverter.ToSingle(bytes, startIndex),
            var t when t == typeof(double) => (T)(object)BitConverter.ToDouble(bytes, startIndex),
            var t when t == typeof(bool) => (T)(object)BitConverter.ToBoolean(bytes, startIndex),
            var t when t == typeof(char) => (T)(object)BitConverter.ToChar(bytes, startIndex),
            _ => throw new ArgumentException($"Unsupported type: {typeof(T).Name}")
        };
    }

    public static byte[] EncodeString(string value, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return encoding.GetBytes(value);
    }

    public static string DecodeString(byte[] bytes, int index, int count, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return encoding.GetString(bytes, index, count);
    }

    public static byte[] Concat(params byte[][] arrays)
    {
        var totalLength = arrays.Sum(a => a.Length);
        var result = new byte[totalLength];
        var offset = 0;
        foreach (var array in arrays)
        {
            array.CopyTo(result, offset);
            offset += array.Length;
        }
        return result;
    }

    public static byte[] Slice(byte[] data, int offset, int length)
    {
        var result = new byte[length];
        Buffer.BlockCopy(data, offset, result, 0, length);
        return result;
    }
}