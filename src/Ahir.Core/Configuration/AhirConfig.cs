using Ahir.Core.Constants;

namespace Ahir.Core.Configuration;

public sealed class AhirConfig
{
    public ServerConfig Server { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public StorageConfig Storage { get; set; } = new();
    public RealtimeConfig Realtime { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public MonitoringConfig Monitoring { get; set; } = new();
}

public sealed class ServerConfig
{
    public int HttpPort { get; set; } = AhirConstants.DefaultHttpPort;
    public int HttpsPort { get; set; } = AhirConstants.DefaultHttpsPort;
    public int MaxConnections { get; set; } = AhirConstants.DefaultMaxConnections;
    public int MaxWorkerThreads { get; set; } = AhirConstants.DefaultMaxWorkerThreads;
    public int MemoryLimit { get; set; } = AhirConstants.DefaultMemoryLimit;
    public bool EnableSsl { get; set; }
    public string? SslCertificatePath { get; set; }
    public string? SslCertificatePassword { get; set; }
    public string? AllowedOrigins { get; set; }
    public int RequestBodySizeLimit { get; set; } = 10 * 1024 * 1024;
    public TimeSpan OperationTimeout { get; set; } = AhirConstants.DefaultOperationTimeout;
    public bool EnableCompression { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
}

public sealed class DatabaseConfig
{
    public string DataPath { get; set; } = AhirConstants.DataDirectory;
    public int CacheSize { get; set; } = AhirConstants.DefaultCacheSize;
    public int WriteBufferSize { get; set; } = AhirConstants.DefaultWriteBufferSize;
    public int ReadBufferSize { get; set; } = AhirConstants.DefaultReadBufferSize;
    public bool EnableCompression { get; set; } = true;
    public bool EnableEncryption { get; set; }
    public string? EncryptionKey { get; set; }
    public int FlushIntervalMs { get; set; } = 1000;
    public int MaxWriteQueueSize { get; set; } = 10000;
    public bool EnableBloomFilter { get; set; } = true;
    public int BloomFilterSize { get; set; } = AhirConstants.BloomFilterSize;
    public bool EnableLruCache { get; set; } = true;
    public int LruCacheCapacity { get; set; } = AhirConstants.LruCacheCapacity;
    public bool EnableAutoCompaction { get; set; } = true;
    public int CompactionIntervalMinutes { get; set; } = 60;
    public bool EnableTransactionLog { get; set; } = true;
}

public sealed class SecurityConfig
{
    public string JwtSecret { get; set; } = string.Empty;
    public int TokenExpirationHours { get; set; } = AhirConstants.TokenExpirationHours;
    public int RefreshTokenExpirationDays { get; set; } = AhirConstants.RefreshTokenExpirationDays;
    public int ArgonIterations { get; set; } = 3;
    public int ArgonMemorySize { get; set; } = 65536;
    public int ArgonParallelism { get; set; } = 4;
    public bool EnableAuditLog { get; set; } = true;
    public bool EnableIpBan { get; set; } = true;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LoginLockoutMinutes { get; set; } = 15;
    public IReadOnlyList<string>? WhitelistedIps { get; set; }
    public IReadOnlyList<string>? BlacklistedIps { get; set; }
}

public sealed class StorageConfig
{
    public string StoragePath { get; set; } = "storage";
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024;
    public long MaxBucketSize { get; set; } = 10L * 1024 * 1024 * 1024;
    public bool EnableDeduplication { get; set; } = true;
    public bool EnableEncryption { get; set; }
    public int ChunkSize { get; set; } = 1024 * 1024;
    public IReadOnlyList<string>? AllowedExtensions { get; set; }
}

public sealed class RealtimeConfig
{
    public int WebSocketPort { get; set; } = AhirConstants.DefaultWebSocketPort;
    public int MaxConnections { get; set; } = 10000;
    public TimeSpan HeartbeatInterval { get; set; } = AhirConstants.DefaultHeartbeatInterval;
    public bool EnablePresence { get; set; } = true;
    public int MaxMessageSize { get; set; } = 256 * 1024;
    public int MessageQueueSize { get; set; } = 1000;
}

public sealed class LoggingConfig
{
    public string LogPath { get; set; } = AhirConstants.LogDirectory;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
    public bool EnableConsole { get; set; } = true;
    public bool EnableFile { get; set; } = true;
    public int MaxFileSizeMb { get; set; } = 100;
    public int MaxFileCount { get; set; } = 31;
    public bool EnableJsonFormat { get; set; }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

public sealed class MonitoringConfig
{
    public bool EnablePrometheus { get; set; } = true;
    public bool EnableOpenTelemetry { get; set; }
    public int MetricsPort { get; set; } = 9100;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public bool EnableTracing { get; set; }
    public bool EnablePerformanceCounters { get; set; } = true;
}