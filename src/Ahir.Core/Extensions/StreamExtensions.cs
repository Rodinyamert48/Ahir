namespace Ahir.Core.Extensions;

public static class StreamExtensions
{
    public static async Task<byte[]> ReadAllAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    public static async Task WriteAllAsync(this Stream stream, byte[] data, CancellationToken cancellationToken = default)
    {
        await stream.WriteAsync(data, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task CopyToWithProgressAsync(
        this Stream source,
        Stream destination,
        long totalSize,
        Action<long>? onProgress = null,
        int bufferSize = 81920,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[bufferSize];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;
            onProgress?.Invoke(totalRead);
        }

        await destination.FlushAsync(cancellationToken);
    }
}