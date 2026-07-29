namespace Ahir.Core.Constants;

public static class AhirConstants
{
    public const string ProductName = "Ahir";
    public const string Version = "1.0.0";
    public const string Company = "Ahir Technologies";
    public const string Description = "Next-generation backend platform with embedded database engine";

    public const int DefaultHttpPort = 8080;
    public const int DefaultHttpsPort = 8443;
    public const int DefaultWebSocketPort = 9090;
    public const int DefaultDashboardPort = 3000;

    public const int MinPort = 1024;
    public const int MaxPort = 65535;

    public const int MaxDatabaseNameLength = 64;
    public const int MaxCollectionNameLength = 64;
    public const int MaxFieldNameLength = 128;
    public const int MaxRecordSize = 16 * 1024 * 1024;
    public const int MaxBatchSize = 10000;

    public const int DefaultCacheSize = 256 * 1024 * 1024;
    public const int DefaultWriteBufferSize = 64 * 1024 * 1024;
    public const int DefaultReadBufferSize = 4 * 1024;

    public const int DefaultMaxConnections = 1000;
    public const int DefaultMaxWorkerThreads = 32;
    public const int DefaultMemoryLimit = 512 * 1024 * 1024;

    public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan DefaultReconnectInterval = TimeSpan.FromSeconds(5);

    public const string ConfigFileName = "ahir.json";
    public const string LogDirectory = "logs";
    public const string DataDirectory = "data";
    public const string BackupDirectory = "backups";
    public const string PluginDirectory = "plugins";
    public const string TempDirectory = "temp";

    public const string DefaultAdminUsername = "admin";
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int SaltLength = 32;
    public const int HashLength = 64;
    public const int KeyLength = 32;
    public const int IvLength = 12;
    public const int TokenExpirationHours = 24;
    public const int RefreshTokenExpirationDays = 7;

    public const int BloomFilterSize = 10_000_000;
    public const int LruCacheCapacity = 100_000;
}