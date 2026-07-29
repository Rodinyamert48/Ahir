using K4os.Compression.LZ4;
using Ahir.Core.Configuration;
using Ahir.Core.Utilities;

namespace Ahir.Database.Storage;

internal sealed class StorageEngine : IDisposable
{
    private readonly string _basePath;
    private readonly DatabaseConfig _config;
    private readonly Lock _lock = new();
    private FileStream? _dataFile;
    private FileStream? _walFile;
    private long _nextPosition;
    private bool _disposed;

    public StorageEngine(string basePath, DatabaseConfig config)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        Directory.CreateDirectory(basePath);
    }

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        var dataPath = Path.Combine(_basePath, "data.ahir");
        var walPath = Path.Combine(_basePath, "wal.ahir");

        _dataFile = new FileStream(dataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, _config.WriteBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        _walFile = new FileStream(walPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, _config.WriteBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

        _nextPosition = _dataFile.Length;
        await RecoverAsync(cancellationToken);
    }

    public async Task<long> WriteAsync(byte[] data, CompressionType compression = CompressionType.LZ4, CancellationToken cancellationToken = default)
    {
        var compressed = _config.EnableCompression && compression != CompressionType.None
            ? Compress(data)
            : data;

        var header = new RecordHeader
        {
            Length = compressed.Length + RecordHeader.Size,
            DataLength = compressed.Length,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Checksum = ComputeCrc32(compressed),
            CompressionType = (byte)compression,
            Position = _nextPosition
        };

        lock (_lock)
        {
            _dataFile!.Seek(_nextPosition, SeekOrigin.Begin);
            WriteHeader(_dataFile, header);
            _dataFile.Write(compressed, 0, compressed.Length);
            _dataFile.Flush(true);

            _walFile!.Seek(0, SeekOrigin.End);
            WriteHeader(_walFile, header);
            _walFile.Write(compressed, 0, compressed.Length);
            _walFile.Flush(true);

            var position = _nextPosition;
            _nextPosition += header.Length;
            return position;
        }
    }

    public async Task<byte[]?> ReadAsync(long position, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (position >= _dataFile!.Length)
                return null;

            _dataFile.Seek(position, SeekOrigin.Begin);
            var header = ReadHeaderFromStream(_dataFile);

            if ((header.Flags & (byte)RecordFlag.Deleted) != 0)
                return null;

            var data = new byte[header.DataLength];
            _dataFile.Read(data, 0, data.Length);

            return header.CompressionType != 0 ? Decompress(data) : data;
        }
    }

    public async Task<bool> DeleteAsync(long position)
    {
        lock (_lock)
        {
            if (position >= _dataFile!.Length)
                return false;

            _dataFile.Seek(position + 36, SeekOrigin.Begin);
            _dataFile.WriteByte((byte)RecordFlag.Deleted);
            _dataFile.Flush(true);
            return true;
        }
    }

    public async Task<long> GetLengthAsync()
    {
        lock (_lock)
        {
            return _dataFile?.Length ?? 0;
        }
    }

    public async Task CompactAsync(string targetPath, Func<long, bool> shouldKeep, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            using var compactFile = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, _config.WriteBufferSize);
            _dataFile!.Seek(0, SeekOrigin.Begin);

            var buffer = new byte[RecordHeader.Size];
            long sourcePosition = 0;
            long targetPosition = 0;

            while (sourcePosition < _dataFile.Length)
            {
                _dataFile.Read(buffer, 0, RecordHeader.Size);
                var header = ReadHeaderFromBuffer(buffer);
                var data = new byte[header.DataLength];
                _dataFile.Read(data, 0, data.Length);

                if (shouldKeep(sourcePosition))
                {
                    header.Position = targetPosition;
                    WriteHeader(compactFile, header);
                    compactFile.Write(data, 0, data.Length);
                    targetPosition += header.Length;
                }

                sourcePosition += header.Length;
            }

            compactFile.Flush(true);
        }
    }

    private async Task RecoverAsync(CancellationToken cancellationToken)
    {
        if (_walFile!.Length <= RecordHeader.Size)
        {
            _walFile.SetLength(0);
            _walFile.Flush(true);
            return;
        }

        _walFile.Seek(0, SeekOrigin.Begin);
        while (_walFile.Position < _walFile.Length - RecordHeader.Size)
        {
            var buffer = new byte[RecordHeader.Size];
            _walFile.Read(buffer, 0, RecordHeader.Size);
            var header = ReadHeaderFromBuffer(buffer);
            _walFile.Seek(header.DataLength, SeekOrigin.Current);
        }

        _walFile.SetLength(0);
        _walFile.Flush(true);
    }

    private static byte[] Compress(byte[] data)
    {
        var maxLength = LZ4Codec.MaximumOutputSize(data.Length);
        var target = new byte[maxLength];
        var compressedLength = LZ4Codec.Encode(data, target);
        Array.Resize(ref target, compressedLength);
        return target;
    }

    private static byte[] Decompress(byte[] data)
    {
        var maxLength = data.Length * 4;
        var target = new byte[maxLength];
        var decodedLength = LZ4Codec.Decode(data, target);
        Array.Resize(ref target, decodedLength);
        return target;
    }

    private static void WriteHeader(Stream stream, RecordHeader header)
    {
        Span<byte> buffer = stackalloc byte[RecordHeader.Size];
        BitConverter.TryWriteBytes(buffer[..8], header.Position);
        BitConverter.TryWriteBytes(buffer[8..12], header.Length);
        BitConverter.TryWriteBytes(buffer[12..16], header.DataLength);
        BitConverter.TryWriteBytes(buffer[16..24], header.Timestamp);
        BitConverter.TryWriteBytes(buffer[24..28], header.Checksum);
        buffer[36] = header.Flags;
        buffer[37] = header.CompressionType;
        stream.Write(buffer);
    }

    private static RecordHeader ReadHeaderFromStream(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[RecordHeader.Size];
        stream.Read(buffer);
        return ReadHeaderFromSpan(buffer);
    }

    private static RecordHeader ReadHeaderFromBuffer(byte[] buffer)
    {
        return ReadHeaderFromSpan(buffer.AsSpan());
    }

    private static RecordHeader ReadHeaderFromSpan(ReadOnlySpan<byte> buffer)
    {
        return new RecordHeader
        {
            Position = BitConverter.ToInt64(buffer[..8]),
            Length = BitConverter.ToInt32(buffer[8..12]),
            DataLength = BitConverter.ToInt32(buffer[12..16]),
            Timestamp = BitConverter.ToInt64(buffer[16..24]),
            Checksum = BitConverter.ToUInt32(buffer[24..28]),
            Flags = buffer[36],
            CompressionType = buffer[37],
        };
    }

    private static uint ComputeCrc32(byte[] data)
    {
        var hash = System.IO.Hashing.Crc32.Hash(data);
        return BitConverter.ToUInt32(hash);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _dataFile?.Dispose();
        _walFile?.Dispose();
    }
}