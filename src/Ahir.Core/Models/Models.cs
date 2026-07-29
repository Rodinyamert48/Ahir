namespace Ahir.Core.Models;

public sealed class AhirRecord
{
    public string Id { get; set; } = string.Empty;
    public string Collection { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public Dictionary<string, object?> Fields { get; set; } = new();
    public long CreatedAt { get; set; }
    public long ModifiedAt { get; set; }
    public long Version { get; set; } = 1;
    public string? Checksum { get; set; }

    public T? GetField<T>(string name)
    {
        if (Fields.TryGetValue(name, out var value) && value is T typed)
            return typed;
        return default;
    }

    public bool HasField(string name) => Fields.ContainsKey(name);
}

public sealed class CollectionInfo
{
    public string Name { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public long RecordCount { get; init; }
    public long SizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public Dictionary<string, string> Indexes { get; init; } = new();
}

public sealed class DatabaseInfo
{
    public string Name { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public int CollectionCount { get; init; }
    public long RecordCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public DatabaseStatus Status { get; init; }
}

public enum DatabaseStatus
{
    Active,
    ReadOnly,
    Corrupted,
    BackingUp,
    Restoring
}

public sealed class FieldInfo
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool Indexed { get; init; }
    public bool Required { get; init; }
    public object? DefaultValue { get; init; }
}

public sealed class PageResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasNext => Page * PageSize < TotalCount;
    public bool HasPrevious => Page > 1;
}

public sealed class AhirResult<T>
{
    public bool Success { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static AhirResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static AhirResult<T> Fail(string errorCode, string message) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = message };
}

public sealed class QueryFilter
{
    public string Field { get; init; } = string.Empty;
    public FilterOperator Operator { get; init; }
    public object? Value { get; init; }
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    In,
    NotIn,
    Between
}

public sealed class SortOption
{
    public string Field { get; init; } = string.Empty;
    public bool Descending { get; init; }
}

public sealed class QueryOptions
{
    public IReadOnlyList<QueryFilter>? Filters { get; init; }
    public IReadOnlyList<SortOption>? Sort { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 100;
    public int? Limit { get; init; }
    public int? Offset { get; init; }
    public IReadOnlyList<string>? Fields { get; init; }
}

public sealed class AhirMetrics
{
    public long UptimeSeconds { get; init; }
    public long TotalRequests { get; init; }
    public long ActiveConnections { get; init; }
    public long ActiveWebSockets { get; init; }
    public long DatabaseSizeBytes { get; init; }
    public long StorageSizeBytes { get; init; }
    public double CpuUsagePercent { get; init; }
    public double MemoryUsageBytes { get; init; }
    public long DiskReadBytes { get; init; }
    public long DiskWriteBytes { get; init; }
    public int PluginCount { get; init; }
    public int DatabaseCount { get; init; }
    public DateTime ServerTime { get; init; }
}

public sealed class BackupInfo
{
    public string Id { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public BackupType Type { get; init; }
    public BackupStatus Status { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? Checksum { get; init; }
    public bool Encrypted { get; init; }
}

public enum BackupType
{
    Full,
    Incremental,
    Snapshot
}

public enum BackupStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Restoring
}

public sealed class PluginInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public PluginState State { get; init; }
}

public enum PluginState
{
    Loaded,
    Running,
    Stopped,
    Error,
    Disabled
}

public sealed class UserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public bool Enabled { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public sealed class AuthToken
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}