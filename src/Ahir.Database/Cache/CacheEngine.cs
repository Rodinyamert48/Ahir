using System.Collections.Concurrent;

namespace Ahir.Database.Cache;

internal sealed class CacheEngine : IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly LinkedList<string> _accessOrder = new();
    private readonly Lock _lock = new();
    private readonly int _capacity;
    private readonly int _maxMemoryBytes;
    private long _currentMemoryBytes;
    private readonly Timer? _cleanupTimer;

    public CacheEngine(int capacity, int maxMemoryBytes)
    {
        _capacity = capacity > 0 ? capacity : 100000;
        _maxMemoryBytes = maxMemoryBytes > 0 ? maxMemoryBytes : 256 * 1024 * 1024;
        _cleanupTimer = new Timer(_ => CleanupExpired(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public long Count => _cache.Count;

    public void Set(string key, byte[] value, TimeSpan? expiration = null)
    {
        var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : DateTime.MaxValue;
        var entry = new CacheEntry(value, expiresAt, value.Length);

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var existing))
                _currentMemoryBytes -= existing.Size;

            _cache[key] = entry;
            _currentMemoryBytes += entry.Size;
            _accessOrder.Remove(key);
            _accessOrder.AddFirst(key);
        }

        EvictIfNeeded();
    }

    public byte[]? Get(string key)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var entry))
                return null;

            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                RemoveInternal(key);
                return null;
            }

            _accessOrder.Remove(key);
            _accessOrder.AddFirst(key);
            return entry.Data;
        }
    }

    public bool Remove(string key)
    {
        lock (_lock)
        {
            return RemoveInternal(key);
        }
    }

    public bool Exists(string key)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var entry))
                return false;
            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                RemoveInternal(key);
                return false;
            }
            return true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _accessOrder.Clear();
            _currentMemoryBytes = 0;
        }
    }

    private void EvictIfNeeded()
    {
        lock (_lock)
        {
            while (_cache.Count > _capacity || _currentMemoryBytes > _maxMemoryBytes)
            {
                var last = _accessOrder.Last;
                if (last == null) break;
                RemoveInternal(last.Value);
            }
        }
    }

    private bool RemoveInternal(string key)
    {
        if (_cache.TryRemove(key, out var entry))
        {
            _currentMemoryBytes -= entry.Size;
            _accessOrder.Remove(key);
            return true;
        }
        return false;
    }

    private void CleanupExpired()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var expired = _cache.Where(kvp => now > kvp.Value.ExpiresAt).Select(kvp => kvp.Key).ToList();
            foreach (var key in expired)
                RemoveInternal(key);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        Clear();
    }
}

internal sealed class CacheEntry
{
    public byte[] Data { get; }
    public DateTime ExpiresAt { get; }
    public long Size { get; }

    public CacheEntry(byte[] data, DateTime expiresAt, long size)
    {
        Data = data;
        ExpiresAt = expiresAt;
        Size = size;
    }
}