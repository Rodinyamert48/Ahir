using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Database;

namespace Ahir.Tests;

[Collection("DatabaseTests")]
public class DatabaseTests : IDisposable
{
    private readonly string _testDir;
    private readonly DatabaseEngine _engine;

    public DatabaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ahir_test_" + Guid.NewGuid().ToString("N"));
        _engine = new DatabaseEngine(new DatabaseConfig
        {
            DataPath = _testDir,
            EnableBloomFilter = false,
            EnableLruCache = true,
            LruCacheCapacity = 1000,
            CacheSize = 1024 * 1024
        });
    }

    private async Task<AhirResult<CollectionInfo>> EnsureCollectionAsync(string db, string coll)
    {
        var dbResult = await _engine.CreateAsync(db);
        if (!dbResult.Success && dbResult.ErrorCode != "ALREADY_EXISTS")
            return AhirResult<CollectionInfo>.Fail(dbResult.ErrorCode, dbResult.ErrorMessage);

        return await ((ICollectionEngine)_engine).CreateAsync(db, coll);
    }

    [Fact]
    public async Task CreateAndDropDatabase()
    {
        var createResult = await _engine.CreateAsync("test_db");
        Assert.True(createResult.Success);
        Assert.Equal("test_db", createResult.Data?.Name);

        var dropResult = await _engine.DropAsync("test_db");
        Assert.True(dropResult.Success);
    }

    [Fact]
    public async Task CreateDuplicateDatabase_ReturnsError()
    {
        await _engine.CreateAsync("dup_db");
        var result = await _engine.CreateAsync("dup_db");
        Assert.False(result.Success);
        Assert.Equal("ALREADY_EXISTS", result.ErrorCode);
        await _engine.DropAsync("dup_db");
    }

    [Fact]
    public async Task CreateAndDropCollection()
    {
        var cr = await EnsureCollectionAsync("db_coll", "test_coll");
        Assert.True(cr.Success, cr.ErrorMessage);
        Assert.Equal("test_coll", cr.Data?.Name);

        var dropResult = await ((ICollectionEngine)_engine).DropAsync("db_coll", "test_coll");
        Assert.True(dropResult.Success);
        await _engine.DropAsync("db_coll");
    }

    [Fact]
    public async Task InsertAndGetRecord()
    {
        await EnsureCollectionAsync("db_igr", "coll");

        var fields = new Dictionary<string, object?> { { "name", "test" }, { "value", 42 } };
        var insertResult = await ((ICollectionEngine)_engine).InsertAsync("db_igr", "coll", fields);
        Assert.True(insertResult.Success);
        Assert.NotNull(insertResult.Data?.Id);

        var getResult = await ((ICollectionEngine)_engine).GetAsync("db_igr", "coll", insertResult.Data.Id);
        Assert.True(getResult.Success);
        Assert.Equal("test", getResult.Data?.Fields["name"]?.ToString());

        await _engine.DropAsync("db_igr");
    }

    [Fact]
    public async Task UpdateRecord()
    {
        await EnsureCollectionAsync("db_upd", "coll");

        var fields = new Dictionary<string, object?> { { "name", "original" } };
        var insert = await ((ICollectionEngine)_engine).InsertAsync("db_upd", "coll", fields);
        var id = insert.Data!.Id;

        var updateFields = new Dictionary<string, object?> { { "name", "updated" } };
        var updateResult = await ((ICollectionEngine)_engine).UpdateAsync("db_upd", "coll", id, updateFields);
        Assert.True(updateResult.Success);

        var getResult = await ((ICollectionEngine)_engine).GetAsync("db_upd", "coll", id);
        Assert.Equal("updated", getResult.Data?.Fields["name"]?.ToString());

        await _engine.DropAsync("db_upd");
    }

    [Fact]
    public async Task DeleteRecord()
    {
        await EnsureCollectionAsync("db_del", "coll");

        var insert = await ((ICollectionEngine)_engine).InsertAsync("db_del", "coll", new Dictionary<string, object?> { { "x", 1 } });
        var id = insert.Data!.Id;

        var deleteResult = await ((ICollectionEngine)_engine).DeleteAsync("db_del", "coll", id);
        Assert.True(deleteResult.Success);

        var getResult = await ((ICollectionEngine)_engine).GetAsync("db_del", "coll", id);
        Assert.False(getResult.Success);

        await _engine.DropAsync("db_del");
    }

    [Fact]
    public async Task QueryWithFilter()
    {
        await EnsureCollectionAsync("db_qry", "coll");

        await ((ICollectionEngine)_engine).InsertAsync("db_qry", "coll", new() { { "type", "a" }, { "val", 1 } });
        await ((ICollectionEngine)_engine).InsertAsync("db_qry", "coll", new() { { "type", "b" }, { "val", 2 } });
        await ((ICollectionEngine)_engine).InsertAsync("db_qry", "coll", new() { { "type", "a" }, { "val", 3 } });

        var options = new QueryOptions
        {
            Filters = new[] { new QueryFilter { Field = "type", Operator = FilterOperator.Equals, Value = "a" } }
        };
        var result = await ((ICollectionEngine)_engine).QueryAsync("db_qry", "coll", options);
        Assert.True(result.Success);
        Assert.Equal(2, result.Data?.TotalCount);

        await _engine.DropAsync("db_qry");
    }

    [Fact]
    public async Task QueryWithPagination()
    {
        await EnsureCollectionAsync("db_pg", "coll");

        for (var i = 0; i < 10; i++)
            await ((ICollectionEngine)_engine).InsertAsync("db_pg", "coll", new() { { "idx", i } });

        var options = new QueryOptions { Page = 1, PageSize = 3 };
        var result = await ((ICollectionEngine)_engine).QueryAsync("db_pg", "coll", options);
        Assert.True(result.Success);
        Assert.Equal(3, result.Data?.Items.Count);
        Assert.Equal(10, result.Data?.TotalCount);

        await _engine.DropAsync("db_pg");
    }

    [Fact]
    public async Task CountRecords()
    {
        await EnsureCollectionAsync("db_cnt", "coll");

        for (var i = 0; i < 5; i++)
            await ((ICollectionEngine)_engine).InsertAsync("db_cnt", "coll", new() { { "n", i } });

        var count = await ((ICollectionEngine)_engine).CountAsync("db_cnt", "coll");
        Assert.True(count.Success);
        Assert.Equal(5, count.Data);

        await _engine.DropAsync("db_cnt");
    }

    public void Dispose()
    {
        _engine.Dispose();
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
        GC.SuppressFinalize(this);
    }
}
