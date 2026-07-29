using Ahir.Core.Models;

namespace Ahir.Core.Interfaces;

public interface IDatabaseEngine : IAsyncDisposable
{
    string Name { get; }
    DatabaseInfo Info { get; }

    Task<AhirResult<DatabaseInfo>> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<AhirResult<DatabaseInfo>> OpenAsync(string name, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> DropAsync(string name, CancellationToken cancellationToken = default);
    Task<AhirResult<DatabaseInfo>> GetInfoAsync(string name, CancellationToken cancellationToken = default);
    Task<AhirResult<IReadOnlyList<DatabaseInfo>>> ListAsync(CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> CompactAsync(string name, CancellationToken cancellationToken = default);
}

public interface ICollectionEngine
{
    string Name { get; }
    string Database { get; }
    CollectionInfo Info { get; }

    Task<AhirResult<CollectionInfo>> CreateAsync(string database, string name, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> DropAsync(string database, string name, CancellationToken cancellationToken = default);
    Task<AhirResult<CollectionInfo>> GetInfoAsync(string database, string name, CancellationToken cancellationToken = default);

    Task<AhirResult<AhirRecord>> InsertAsync(string database, string collection, Dictionary<string, object?> fields, CancellationToken cancellationToken = default);
    Task<AhirResult<AhirRecord>> UpdateAsync(string database, string collection, string id, Dictionary<string, object?> fields, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> DeleteAsync(string database, string collection, string id, CancellationToken cancellationToken = default);
    Task<AhirResult<AhirRecord>> GetAsync(string database, string collection, string id, CancellationToken cancellationToken = default);

    Task<AhirResult<PageResult<AhirRecord>>> QueryAsync(string database, string collection, QueryOptions options, CancellationToken cancellationToken = default);
    Task<AhirResult<long>> CountAsync(string database, string collection, IReadOnlyList<QueryFilter>? filters = null, CancellationToken cancellationToken = default);

    Task<AhirResult<bool>> CreateIndexAsync(string database, string collection, string field, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> DropIndexAsync(string database, string collection, string field, CancellationToken cancellationToken = default);
}

public interface IStorageEngine
{
    Task<AhirResult<string>> UploadAsync(string bucket, string name, Stream content, bool overwrite = false, CancellationToken cancellationToken = default);
    Task<AhirResult<Stream?>> DownloadAsync(string bucket, string name, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> DeleteAsync(string bucket, string name, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> ExistsAsync(string bucket, string name, CancellationToken cancellationToken = default);
    Task<AhirResult<long>> GetSizeAsync(string bucket, string name, CancellationToken cancellationToken = default);

    Task<AhirResult<string>> UploadChunkAsync(string bucket, string name, int chunkIndex, Stream content, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> CommitUploadAsync(string bucket, string name, int totalChunks, string checksum, CancellationToken cancellationToken = default);

    Task<AhirResult<IReadOnlyList<string>>> ListAsync(string bucket, string? prefix = null, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> CreateBucketAsync(string name, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> DeleteBucketAsync(string name, CancellationToken cancellationToken = default);
}

public interface ISecurityProvider
{
    Task<AhirResult<string>> HashPasswordAsync(string password, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> VerifyPasswordAsync(string password, string hash, CancellationToken cancellationToken = default);

    Task<AhirResult<AuthToken>> CreateTokenAsync(UserInfo user, CancellationToken cancellationToken = default);
    Task<AhirResult<AuthToken>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<AhirResult<UserInfo>> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<AhirResult<UserInfo>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    string GenerateApiKey();
    string GenerateSecret();
    (byte[] Key, byte[] Iv) GenerateEncryptionKey();
}

public interface IRealtimeEngine
{
    Task<bool> PublishAsync(string channel, string eventType, object? data, CancellationToken cancellationToken = default);
    Task SubscribeAsync(string channel, Func<RealtimeMessage, Task> handler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string channel, CancellationToken cancellationToken = default);
    Task BroadcastAsync(string eventType, object? data, CancellationToken cancellationToken = default);
    IAsyncEnumerable<RealtimeMessage> StreamAsync(string channel, CancellationToken cancellationToken = default);
}

public sealed class RealtimeMessage
{
    public string Channel { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public object? Data { get; init; }
    public string? SenderId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public interface IPluginEngine
{
    Task<AhirResult<PluginInfo>> LoadAsync(string pluginPath, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> UnloadAsync(string pluginId, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> StartAsync(string pluginId, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> StopAsync(string pluginId, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> ReloadAsync(string pluginId, CancellationToken cancellationToken = default);
    IReadOnlyList<PluginInfo> GetLoadedPlugins();
    PluginInfo? GetPlugin(string pluginId);
}

public interface IBackupService
{
    Task<AhirResult<BackupInfo>> CreateBackupAsync(string? databaseName = null, BackupType type = BackupType.Full, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> RestoreAsync(string backupId, CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);
    Task<AhirResult<IReadOnlyList<BackupInfo>>> ListBackupsAsync(CancellationToken cancellationToken = default);
    Task<AhirResult<BackupInfo>> GetBackupInfoAsync(string backupId, CancellationToken cancellationToken = default);
}

public interface IMonitorService
{
    AhirMetrics GetCurrentMetrics();
    IAsyncEnumerable<AhirMetrics> StreamMetricsAsync(TimeSpan interval, CancellationToken cancellationToken = default);
    event Action<AhirMetrics>? OnMetricsUpdated;
}

public interface IServerHost
{
    string InstanceId { get; }
    DateTime StartedAt { get; }
    ServerState State { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
    Task<AhirResult<bool>> HealthCheckAsync(CancellationToken cancellationToken = default);

    IDatabaseEngine Database { get; }
    IStorageEngine Storage { get; }
    ISecurityProvider Security { get; }
    IRealtimeEngine Realtime { get; }
    IPluginEngine Plugin { get; }
    IBackupService Backup { get; }
    IMonitorService Monitor { get; }
}

public enum ServerState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Restarting,
    Error
}

public interface IConfigService
{
    Task<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : new();
    Task SaveAsync<T>(string path, T config, CancellationToken cancellationToken = default);
    T GetDefault<T>() where T : new();
}

public interface ILogService
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Error(Exception exception, string message);
    void Fatal(string message);
    void Fatal(Exception exception, string message);
    IDisposable BeginScope(string key, object value);
}

public interface ICacheService : IDisposable
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    long Count { get; }
}

public interface IRateLimiter
{
    Task<bool> IsAllowedAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default);
    Task<RateLimitStatus> GetStatusAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default);
}

public sealed class RateLimitStatus
{
    public bool IsAllowed { get; init; }
    public int Remaining { get; init; }
    public long ResetAtUnixMs { get; init; }
    public int Limit { get; init; }
}