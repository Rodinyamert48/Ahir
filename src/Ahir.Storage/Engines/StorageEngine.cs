using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Core.Utilities;

namespace Ahir.Storage.Engines;

public sealed class StorageEngine : IStorageEngine
{
    private readonly StorageConfig _config;
    private readonly string _basePath;

    public StorageEngine(StorageConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _basePath = config.StoragePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<AhirResult<string>> UploadAsync(string bucket, string name, Stream content, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var bucketPath = GetBucketPath(bucket);
        Directory.CreateDirectory(bucketPath);

        var filePath = Path.Combine(bucketPath, SanitizeFileName(name));
        if (File.Exists(filePath) && !overwrite)
            return AhirResult<string>.Fail("ALREADY_EXISTS", $"File '{name}' already exists in bucket '{bucket}'.");

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await content.CopyToAsync(fileStream, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);

        return AhirResult<string>.Ok(name);
    }

    public Task<AhirResult<Stream?>> DownloadAsync(string bucket, string name, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(GetBucketPath(bucket), SanitizeFileName(name));
        if (!File.Exists(filePath))
            return Task.FromResult(AhirResult<Stream?>.Fail("NOT_FOUND", $"File '{name}' not found in bucket '{bucket}'."));

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        return Task.FromResult(AhirResult<Stream?>.Ok(stream));
    }

    public Task<AhirResult<bool>> DeleteAsync(string bucket, string name, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(GetBucketPath(bucket), SanitizeFileName(name));
        if (!File.Exists(filePath))
            return Task.FromResult(AhirResult<bool>.Fail("NOT_FOUND", $"File '{name}' not found in bucket '{bucket}'."));

        File.Delete(filePath);
        return Task.FromResult(AhirResult<bool>.Ok(true));
    }

    public Task<AhirResult<bool>> ExistsAsync(string bucket, string name, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(GetBucketPath(bucket), SanitizeFileName(name));
        return Task.FromResult(AhirResult<bool>.Ok(File.Exists(filePath)));
    }

    public Task<AhirResult<long>> GetSizeAsync(string bucket, string name, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(GetBucketPath(bucket), SanitizeFileName(name));
        if (!File.Exists(filePath))
            return Task.FromResult(AhirResult<long>.Fail("NOT_FOUND", $"File '{name}' not found in bucket '{bucket}'."));

        return Task.FromResult(AhirResult<long>.Ok(new FileInfo(filePath).Length));
    }

    public async Task<AhirResult<string>> UploadChunkAsync(string bucket, string name, int chunkIndex, Stream content, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(_basePath, bucket, ".chunks", SanitizeFileName(name));
        Directory.CreateDirectory(tempDir);

        var chunkPath = Path.Combine(tempDir, $"chunk_{chunkIndex:D6}");
        await using var fileStream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await content.CopyToAsync(fileStream, cancellationToken);

        return AhirResult<string>.Ok(chunkPath);
    }

    public async Task<AhirResult<bool>> CommitUploadAsync(string bucket, string name, int totalChunks, string checksum, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(_basePath, bucket, ".chunks", SanitizeFileName(name));
        var finalPath = Path.Combine(GetBucketPath(bucket), SanitizeFileName(name));

        await using var finalStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        using var hashStream = System.Security.Cryptography.SHA512.Create();

        for (var i = 0; i < totalChunks; i++)
        {
            var chunkPath = Path.Combine(tempDir, $"chunk_{i:D6}");
            if (!File.Exists(chunkPath))
                return AhirResult<bool>.Fail("INVALID_INPUT", $"Missing chunk {i}.");

            var chunkData = await File.ReadAllBytesAsync(chunkPath, cancellationToken);
            await finalStream.WriteAsync(chunkData, cancellationToken);
        }

        await finalStream.FlushAsync(cancellationToken);

        // Cleanup chunks
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);

        return AhirResult<bool>.Ok(true);
    }

    public Task<AhirResult<IReadOnlyList<string>>> ListAsync(string bucket, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var bucketPath = GetBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return Task.FromResult(AhirResult<IReadOnlyList<string>>.Ok(Array.Empty<string>()));

        var files = Directory.GetFiles(bucketPath)
            .Select(Path.GetFileName)
            .Where(f => f != null && (prefix == null || f.StartsWith(prefix)))
            .ToList() as IReadOnlyList<string>;

        return Task.FromResult(AhirResult<IReadOnlyList<string>>.Ok(files ?? Array.Empty<string>()));
    }

    public Task<AhirResult<bool>> CreateBucketAsync(string name, CancellationToken cancellationToken = default)
    {
        var bucketPath = GetBucketPath(name);
        Directory.CreateDirectory(bucketPath);
        return Task.FromResult(AhirResult<bool>.Ok(true));
    }

    public Task<AhirResult<bool>> DeleteBucketAsync(string name, CancellationToken cancellationToken = default)
    {
        var bucketPath = GetBucketPath(name);
        if (!Directory.Exists(bucketPath))
            return Task.FromResult(AhirResult<bool>.Fail("NOT_FOUND", $"Bucket '{name}' not found."));

        Directory.Delete(bucketPath, true);
        return Task.FromResult(AhirResult<bool>.Ok(true));
    }

    private string GetBucketPath(string bucket) => Path.Combine(_basePath, SanitizeFileName(bucket));
    private static string SanitizeFileName(string name) => name.Replace("..", "").Replace("/", "_").Replace("\\", "_");
}
