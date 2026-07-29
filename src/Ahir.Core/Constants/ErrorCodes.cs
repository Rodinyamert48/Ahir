namespace Ahir.Core.Constants;

public static class ErrorCodes
{
    public const string Success = "SUCCESS";
    public const string InternalError = "INTERNAL_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string AlreadyExists = "ALREADY_EXISTS";
    public const string InvalidInput = "INVALID_INPUT";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string DatabaseNotFound = "DATABASE_NOT_FOUND";
    public const string CollectionNotFound = "COLLECTION_NOT_FOUND";
    public const string RecordNotFound = "RECORD_NOT_FOUND";
    public const string DatabaseAlreadyExists = "DATABASE_ALREADY_EXISTS";
    public const string CollectionAlreadyExists = "COLLECTION_ALREADY_EXISTS";
    public const string RecordAlreadyExists = "RECORD_ALREADY_EXISTS";
    public const string StorageLimitExceeded = "STORAGE_LIMIT_EXCEEDED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string PluginLoadFailed = "PLUGIN_LOAD_FAILED";
    public const string PluginInvalid = "PLUGIN_INVALID";
    public const string BackupInProgress = "BACKUP_IN_PROGRESS";
    public const string RestoreInProgress = "RESTORE_IN_PROGRESS";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string DatabaseCorrupted = "DATABASE_CORRUPTED";
    public const string QuotaExceeded = "QUOTA_EXCEEDED";
}