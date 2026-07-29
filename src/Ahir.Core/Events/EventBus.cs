namespace Ahir.Core.Events;

public sealed class AhirEventArgs : EventArgs
{
    public string EventType { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public object? Data { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}

public static class EventTypes
{
    public const string ServerStarted = "server.started";
    public const string ServerStopped = "server.stopped";
    public const string ServerRestarted = "server.restarted";
    public const string ServerError = "server.error";

    public const string DatabaseCreated = "database.created";
    public const string DatabaseDropped = "database.dropped";
    public const string DatabaseCompacted = "database.compacted";
    public const string DatabaseCorrupted = "database.corrupted";
    public const string DatabaseBackupStarted = "database.backup.started";
    public const string DatabaseBackupCompleted = "database.backup.completed";
    public const string DatabaseRestoreStarted = "database.restore.started";
    public const string DatabaseRestoreCompleted = "database.restore.completed";

    public const string CollectionCreated = "collection.created";
    public const string CollectionDropped = "collection.dropped";

    public const string RecordInserted = "record.inserted";
    public const string RecordUpdated = "record.updated";
    public const string RecordDeleted = "record.deleted";

    public const string UserLogin = "user.login";
    public const string UserLogout = "user.logout";
    public const string UserCreated = "user.created";
    public const string UserDeleted = "user.deleted";
    public const string PasswordChanged = "password.changed";

    public const string PluginLoaded = "plugin.loaded";
    public const string PluginUnloaded = "plugin.unloaded";
    public const string PluginError = "plugin.error";

    public const string SecurityAlert = "security.alert";
    public const string RateLimitHit = "ratelimit.hit";
    public const string IpBanned = "ip.banned";

    public const string BackupCompleted = "backup.completed";
    public const string BackupFailed = "backup.failed";
    public const string RestoreCompleted = "restore.completed";
    public const string RestoreFailed = "restore.failed";
}

public interface IEventBus
{
    void Publish(string eventType, object? data = null, string? correlationId = null);
    void Subscribe(string eventType, EventHandler<AhirEventArgs> handler);
    void Unsubscribe(string eventType, EventHandler<AhirEventArgs> handler);
}

public sealed class EventBus : IEventBus, IDisposable
{
    private readonly Dictionary<string, List<EventHandler<AhirEventArgs>>> _handlers = new();
    private readonly Lock _lock = new();

    public void Publish(string eventType, object? data = null, string? correlationId = null)
    {
        List<EventHandler<AhirEventArgs>>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers))
                return;
            handlers = handlers.ToList();
        }

        var args = new AhirEventArgs
        {
            EventType = eventType,
            Data = data,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        foreach (var handler in handlers)
        {
            try
            {
                handler(this, args);
            }
            catch
            {
                // Log but never throw in event bus
            }
        }
    }

    public void Subscribe(string eventType, EventHandler<AhirEventArgs> handler)
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<EventHandler<AhirEventArgs>>();
                _handlers[eventType] = handlers;
            }
            handlers.Add(handler);
        }
    }

    public void Unsubscribe(string eventType, EventHandler<AhirEventArgs> handler)
    {
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
                handlers.Remove(handler);
        }
    }

    public void Dispose()
    {
        lock (_lock)
            _handlers.Clear();
    }
}