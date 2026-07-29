using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Core.Utilities;
using Ahir.Database.Cache;
using Ahir.Database.Index;
using Ahir.Database.Storage;

namespace Ahir.Database;

public sealed class DatabaseEngine : IDatabaseEngine, ICollectionEngine, IDisposable, IAsyncDisposable
{
    private readonly DatabaseConfig _config;
    private readonly string _basePath;
    private readonly ConcurrentDictionary<string, CollectionStore> _collections = new();
    private readonly ConcurrentDictionary<string, StorageEngine> _storageEngines = new();
    private readonly IndexEngine _indexEngine = new();
    private readonly CacheEngine _cache;
    private readonly BloomFilter? _bloomFilter;
    private readonly Lock _lock = new();
    private bool _disposed;

    string ICollectionEngine.Name => _currentCollection;
    string ICollectionEngine.Database => _currentDatabase;
    CollectionInfo ICollectionEngine.Info => _currentCollectionInfo ?? new();

    public string Name { get; private set; } = string.Empty;
    public DatabaseInfo Info => GetInfoInternal();

    private string _currentCollection = string.Empty;
    private string _currentDatabase = string.Empty;
    private CollectionInfo? _currentCollectionInfo;

    public DatabaseEngine(DatabaseConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _basePath = config.DataPath;
        _cache = new CacheEngine(config.LruCacheCapacity, config.CacheSize);
        if (config.EnableBloomFilter)
            _bloomFilter = new BloomFilter(config.BloomFilterSize);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<AhirResult<DatabaseInfo>> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        Guard.ValidDatabaseName(name);
        var dbPath = GetDatabasePath(name);
        if (Directory.Exists(dbPath))
            return AhirResult<DatabaseInfo>.Fail("ALREADY_EXISTS", $"Database '{name}' already exists.");

        Directory.CreateDirectory(dbPath);
        await SaveMetadataAsync(name, new DatabaseInfo
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Status = DatabaseStatus.Active
        }, cancellationToken);

        return await OpenAsync(name, cancellationToken);
    }

    public async Task<AhirResult<DatabaseInfo>> OpenAsync(string name, CancellationToken cancellationToken = default)
    {
        Guard.ValidDatabaseName(name);
        var dbPath = GetDatabasePath(name);
        if (!Directory.Exists(dbPath))
            return AhirResult<DatabaseInfo>.Fail("NOT_FOUND", $"Database '{name}' not found.");

        Name = name;
        var storage = new StorageEngine(dbPath, _config);
        await storage.OpenAsync(cancellationToken);
        _storageEngines[name] = storage;

        await LoadCollectionsAsync(name, cancellationToken);
        return AhirResult<DatabaseInfo>.Ok(GetInfoInternal());
    }

    public async Task<AhirResult<bool>> DropAsync(string name, CancellationToken cancellationToken = default)
    {
        Guard.ValidDatabaseName(name);
        var dbPath = GetDatabasePath(name);
        if (!Directory.Exists(dbPath))
            return AhirResult<bool>.Fail("NOT_FOUND", $"Database '{name}' not found.");

        if (_storageEngines.TryRemove(name, out var storage))
            storage.Dispose();

        _collections.Clear();
        _indexEngine.ClearAll();
        _cache.Clear();
        _bloomFilter?.Clear();

        Directory.Delete(dbPath, true);
        return AhirResult<bool>.Ok(true);
    }

    public Task<AhirResult<DatabaseInfo>> GetInfoAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AhirResult<DatabaseInfo>.Ok(GetInfoInternal()));
    }

    public Task<AhirResult<IReadOnlyList<DatabaseInfo>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var databases = Directory.GetDirectories(_basePath)
            .Select(Path.GetFileName)
            .Where(n => n != null)
            .Select(n => new DatabaseInfo
            {
                Name = n!,
                SizeBytes = DirectorySize(Path.Combine(_basePath, n!)),
                CollectionCount = Directory.GetDirectories(Path.Combine(_basePath, n!)).Length,
                CreatedAt = Directory.GetCreationTimeUtc(Path.Combine(_basePath, n!)),
                ModifiedAt = Directory.GetLastWriteTimeUtc(Path.Combine(_basePath, n!)),
                Status = DatabaseStatus.Active
            })
            .ToList() as IReadOnlyList<DatabaseInfo>;

        return Task.FromResult(AhirResult<IReadOnlyList<DatabaseInfo>>.Ok(databases ?? Array.Empty<DatabaseInfo>()));
    }

    public async Task<AhirResult<bool>> CompactAsync(string name, CancellationToken cancellationToken = default)
    {
        Guard.ValidDatabaseName(name);
        if (!_storageEngines.TryGetValue(name, out var storage))
            return AhirResult<bool>.Fail("NOT_FOUND", $"Database '{name}' not open.");

        var tempPath = GetDatabasePath(name) + ".compact";
        await storage.CompactAsync(tempPath, _ => true, cancellationToken);
        File.Copy(tempPath, Path.Combine(GetDatabasePath(name), "data.ahir"), true);
        File.Delete(tempPath);
        return AhirResult<bool>.Ok(true);
    }

    public string DbName => Name;

    public async Task<AhirResult<CollectionInfo>> CreateAsync(string database, string name, CancellationToken cancellationToken = default)
    {
        Guard.ValidCollectionName(name);
        var collectionPath = GetCollectionPath(database, name);
        if (Directory.Exists(collectionPath))
            return AhirResult<CollectionInfo>.Fail("ALREADY_EXISTS", $"Collection '{name}' already exists.");

        Directory.CreateDirectory(collectionPath);
        var info = new CollectionInfo
        {
            Name = name,
            Database = database,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _collections[name] = new CollectionStore { Info = info };
        return AhirResult<CollectionInfo>.Ok(info);
    }

    public async Task<AhirResult<bool>> DropAsync(string database, string name, CancellationToken cancellationToken = default)
    {
        if (!_collections.TryRemove(name, out _))
            return AhirResult<bool>.Fail("NOT_FOUND", $"Collection '{name}' not found.");

        _indexEngine.ClearCollection(name);
        var collectionPath = GetCollectionPath(database, name);
        if (Directory.Exists(collectionPath))
            Directory.Delete(collectionPath, true);

        return AhirResult<bool>.Ok(true);
    }

    public Task<AhirResult<CollectionInfo>> GetInfoAsync(string database, string name, CancellationToken cancellationToken = default)
    {
        if (!_collections.TryGetValue(name, out var store))
            return Task.FromResult(AhirResult<CollectionInfo>.Fail("NOT_FOUND", $"Collection '{name}' not found."));

        return Task.FromResult(AhirResult<CollectionInfo>.Ok(store.Info));
    }

    public async Task<AhirResult<AhirRecord>> InsertAsync(string database, string collection, Dictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        var recordId = IdGenerator.NewId();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var record = new AhirRecord
        {
            Id = recordId,
            Collection = collection,
            Database = database,
            Fields = fields,
            CreatedAt = now,
            ModifiedAt = now,
            Version = 1
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(record);
        record.Checksum = Checksum.Compute(json);
        var jsonWithChecksum = JsonSerializer.SerializeToUtf8Bytes(record);

        if (_storageEngines.TryGetValue(database, out var storage))
        {
            await storage.WriteAsync(jsonWithChecksum, CompressionType.LZ4, CancellationToken.None);
            _cache.Set(recordId, jsonWithChecksum, TimeSpan.FromMinutes(30));
            _bloomFilter?.Add(Encoding.UTF8.GetBytes(recordId));
        }

        if (_collections.TryGetValue(collection, out var store))
        {
            Interlocked.Increment(ref store.RecordCount);
        }

        foreach (var field in fields)
        {
            if (_indexEngine.HasIndex(collection, field.Key))
                _indexEngine.AddEntry(collection, field.Key, field.Value, recordId);
        }

        return AhirResult<AhirRecord>.Ok(record);
    }

    public async Task<AhirResult<AhirRecord>> UpdateAsync(string database, string collection, string id, Dictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        var existing = await InternalGetAsync(database, collection, id, cancellationToken);
        if (existing == null)
            return AhirResult<AhirRecord>.Fail("NOT_FOUND", $"Record '{id}' not found.");

        foreach (var field in fields)
            existing.Fields[field.Key] = field.Value;

        existing.ModifiedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        existing.Version++;

        var json = JsonSerializer.SerializeToUtf8Bytes(existing);
        existing.Checksum = Checksum.Compute(json);
        var jsonWithChecksum = JsonSerializer.SerializeToUtf8Bytes(existing);

        if (_storageEngines.TryGetValue(database, out var storage))
        {
            await storage.WriteAsync(jsonWithChecksum, CompressionType.LZ4, CancellationToken.None);
            _cache.Set(id, jsonWithChecksum, TimeSpan.FromMinutes(30));
        }

        return AhirResult<AhirRecord>.Ok(existing);
    }

    public async Task<AhirResult<bool>> DeleteAsync(string database, string collection, string id, CancellationToken cancellationToken = default)
    {
        if (_storageEngines.TryGetValue(database, out var storage))
            await storage.DeleteAsync(0);

        _cache.Remove(id);
        _indexEngine.ClearCollection(collection);

        if (_collections.TryGetValue(collection, out var store))
        {
            Interlocked.Decrement(ref store.RecordCount);
        }

        return AhirResult<bool>.Ok(true);
    }

    public async Task<AhirResult<AhirRecord>> GetAsync(string database, string collection, string id, CancellationToken cancellationToken = default)
    {
        if (_bloomFilter != null && !_bloomFilter.Contains(Encoding.UTF8.GetBytes(id)))
            return AhirResult<AhirRecord>.Fail("NOT_FOUND", $"Record '{id}' not found.");

        var cached = _cache.Get(id);
        if (cached != null)
        {
            var record = JsonSerializer.Deserialize<AhirRecord>(cached);
            if (record != null)
                return AhirResult<AhirRecord>.Ok(record);
        }

        var result = await InternalGetAsync(database, collection, id, cancellationToken);
        if (result == null)
            return AhirResult<AhirRecord>.Fail("NOT_FOUND", $"Record '{id}' not found.");

        return AhirResult<AhirRecord>.Ok(result);
    }

    public async Task<AhirResult<PageResult<AhirRecord>>> QueryAsync(string database, string collection, QueryOptions options, CancellationToken cancellationToken = default)
    {
        var allRecords = new List<AhirRecord>();
        return AhirResult<PageResult<AhirRecord>>.Ok(new PageResult<AhirRecord>
        {
            Items = allRecords,
            TotalCount = allRecords.Count,
            Page = options.Page,
            PageSize = options.PageSize
        });
    }

    public Task<AhirResult<long>> CountAsync(string database, string collection, IReadOnlyList<QueryFilter>? filters = null, CancellationToken cancellationToken = default)
    {
        if (_collections.TryGetValue(collection, out var store))
            return Task.FromResult(AhirResult<long>.Ok(store.RecordCount));
        return Task.FromResult(AhirResult<long>.Ok(0L));
    }

    public async Task<AhirResult<bool>> CreateIndexAsync(string database, string collection, string field, CancellationToken cancellationToken = default)
    {
        _indexEngine.CreateIndex(collection, field);
        return AhirResult<bool>.Ok(true);
    }

    public async Task<AhirResult<bool>> DropIndexAsync(string database, string collection, string field, CancellationToken cancellationToken = default)
    {
        _indexEngine.DropIndex(collection, field);
        return AhirResult<bool>.Ok(true);
    }

    private async Task<AhirRecord?> InternalGetAsync(string database, string collection, string id, CancellationToken cancellationToken)
    {
        if (!_storageEngines.TryGetValue(database, out var storage))
            return null;

        var length = await storage.GetLengthAsync();
        long position = BinaryHeader.Size;
        while (position < length)
        {
            var data = await storage.ReadAsync(position, cancellationToken);
            if (data == null) break;
            try
            {
                var record = JsonSerializer.Deserialize<AhirRecord>(data);
                if (record != null && record.Id == id)
                {
                    _cache.Set(id, data, TimeSpan.FromMinutes(30));
                    return record;
                }
            }
            catch { }
            position += RecordHeader.Size + BitConverter.ToInt32(data[12..16]);
        }
        return null;
    }

    private DatabaseInfo GetInfoInternal()
    {
        return new DatabaseInfo
        {
            Name = Name,
            CollectionCount = _collections.Count,
            RecordCount = _collections.Values.Sum(c => c.RecordCount),
            Status = DatabaseStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            SizeBytes = _storageEngines.Values.Sum(s => s.GetLengthAsync().GetAwaiter().GetResult())
        };
    }

    private string GetDatabasePath(string name) => Path.Combine(_basePath, name);
    private string GetCollectionPath(string database, string collection) => Path.Combine(_basePath, database, collection);

    private async Task LoadCollectionsAsync(string database, CancellationToken cancellationToken)
    {
        var dbPath = GetDatabasePath(database);
        if (!Directory.Exists(dbPath)) return;

        foreach (var dir in Directory.GetDirectories(dbPath))
        {
            var name = Path.GetFileName(dir);
            _collections[name] = new CollectionStore
            {
                Info = new CollectionInfo
                {
                    Name = name,
                    Database = database,
                    CreatedAt = Directory.GetCreationTimeUtc(dir),
                    ModifiedAt = Directory.GetLastWriteTimeUtc(dir)
                }
            };
        }
    }

    private async Task SaveMetadataAsync(string name, DatabaseInfo info, CancellationToken cancellationToken)
    {
        var dbPath = GetDatabasePath(name);
        var metaPath = Path.Combine(dbPath, "metadata.json");
        var json = JsonSerializer.SerializeToUtf8Bytes(info, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllBytesAsync(metaPath, json, cancellationToken);
    }

    private static long DirectorySize(string path)
    {
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cache.Dispose();
        _indexEngine.Dispose();
        foreach (var storage in _storageEngines.Values)
            storage.Dispose();
        _storageEngines.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _cache.Dispose();
        _indexEngine.Dispose();
        foreach (var storage in _storageEngines.Values)
            storage.Dispose();
        _storageEngines.Clear();
        await Task.CompletedTask;
    }
}

internal sealed class CollectionStore
{
    public CollectionInfo Info { get; set; } = new();
    public long RecordCount;
}