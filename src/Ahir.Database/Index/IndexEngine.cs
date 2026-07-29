using System.Collections.Concurrent;

namespace Ahir.Database.Index;

internal sealed class IndexEngine : IDisposable
{
    private readonly ConcurrentDictionary<string, IndexMap> _indexes = new();
    private readonly Lock _lock = new();

    public void CreateIndex(string collection, string field)
    {
        var key = GetIndexKey(collection, field);
        _indexes.GetOrAdd(key, _ => new IndexMap(field));
    }

    public void DropIndex(string collection, string field)
    {
        var key = GetIndexKey(collection, field);
        _indexes.TryRemove(key, out _);
    }

    public void AddEntry(string collection, string field, object? value, string recordId)
    {
        var key = GetIndexKey(collection, field);
        if (_indexes.TryGetValue(key, out var index))
            index.Add(value?.ToString() ?? "null", recordId);
    }

    public void RemoveEntry(string collection, string field, object? value, string recordId)
    {
        var key = GetIndexKey(collection, field);
        if (_indexes.TryGetValue(key, out var index))
            index.Remove(value?.ToString() ?? "null", recordId);
    }

    public IReadOnlySet<string>? Lookup(string collection, string field, object? value)
    {
        var key = GetIndexKey(collection, field);
        if (_indexes.TryGetValue(key, out var index))
            return index.Lookup(value?.ToString() ?? "null");
        return null;
    }

    public bool HasIndex(string collection, string field)
    {
        return _indexes.ContainsKey(GetIndexKey(collection, field));
    }

    public IReadOnlyList<string> GetIndexes(string collection)
    {
        return _indexes.Keys
            .Where(k => k.StartsWith(collection + ":"))
            .Select(k => k[(collection.Length + 1)..])
            .ToList();
    }

    public void ClearCollection(string collection)
    {
        var keys = _indexes.Keys.Where(k => k.StartsWith(collection + ":")).ToList();
        foreach (var key in keys)
            _indexes.TryRemove(key, out _);
    }

    public void ClearAll()
    {
        _indexes.Clear();
    }

    private static string GetIndexKey(string collection, string field)
        => $"{collection}:{field}";

    public void Dispose()
    {
        _indexes.Clear();
    }
}

internal sealed class IndexMap
{
    private readonly string _field;
    private readonly ConcurrentDictionary<string, HashSet<string>> _map = new();

    public IndexMap(string field)
    {
        _field = field;
    }

    public void Add(string key, string recordId)
    {
        _map.AddOrUpdate(key,
            _ => new HashSet<string> { recordId },
            (_, set) => { set.Add(recordId); return set; });
    }

    public void Remove(string key, string recordId)
    {
        if (_map.TryGetValue(key, out var set))
        {
            set.Remove(recordId);
            if (set.Count == 0)
                _map.TryRemove(key, out _);
        }
    }

    public IReadOnlySet<string>? Lookup(string key)
    {
        return _map.TryGetValue(key, out var set) ? set : null;
    }
}