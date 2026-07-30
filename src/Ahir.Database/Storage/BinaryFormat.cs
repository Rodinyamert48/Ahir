using System.Text;

namespace Ahir.Database.Storage;

public sealed class BinaryHeader
{
    public const int Size = 64;
    public const uint MagicNumber = 0x41484952;
    public const ushort CurrentVersion = 1;

    public uint Magic { get; set; } = MagicNumber;
    public ushort Version { get; set; } = CurrentVersion;
    public ushort Flags { get; set; }
    public long CreatedAt { get; set; }
    public long ModifiedAt { get; set; }
    public long DataOffset { get; set; } = Size;
    public long DataLength { get; set; }
    public long IndexOffset { get; set; }
    public long IndexLength { get; set; }
    public uint Checksum { get; set; }
    public byte CompressionType { get; set; }
    public byte EncryptionType { get; set; }

    public void Write(Span<byte> buffer)
    {
        var writer = new BinaryWriter(new MemoryStream(), Encoding.UTF8);
        // In production: manual byte writing for performance
    }

    public static BinaryHeader Read(ReadOnlySpan<byte> buffer)
    {
        // In production: manual byte reading for performance
        return new BinaryHeader();
    }
}

public sealed class RecordHeader
{
    public const int Size = 48;

    public long Position { get; set; }
    public int Length { get; set; }
    public int DataLength { get; set; }
    public long Timestamp { get; set; }
    public uint Checksum { get; set; }
    public byte Flags { get; set; }
    public byte CompressionType { get; set; }
    public byte EncryptionType { get; set; }
}

public enum CompressionType : byte
{
    None = 0,
    LZ4 = 1,
    Zstd = 2
}

public enum EncryptionType : byte
{
    None = 0,
    Aes256Gcm = 1
}

public enum RecordFlag : byte
{
    None = 0,
    Deleted = 1,
    Compressed = 2,
    Encrypted = 4
}