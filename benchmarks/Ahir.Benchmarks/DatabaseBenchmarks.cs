using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Database;

namespace Ahir.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90, iterationCount: 5, warmupCount: 2)]
[MemoryDiagnoser]
public class DatabaseBenchmarks : IDisposable
{
    private readonly string _testDir;
    private readonly DatabaseEngine _engine;

    public DatabaseBenchmarks()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "ahir_bench_" + Guid.NewGuid().ToString("N"));
        _engine = new DatabaseEngine(new DatabaseConfig
        {
            DataPath = _testDir,
            EnableBloomFilter = false
        });
        _engine.CreateAsync("bench_db").GetAwaiter().GetResult();
        ((ICollectionEngine)_engine).CreateAsync("bench_db", "bench_coll").GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task InsertSingleRecord()
    {
        var fields = new Dictionary<string, object?> { { "name", "benchmark" }, { "value", 42 } };
        await ((ICollectionEngine)_engine).InsertAsync("bench_db", "bench_coll", fields);
    }

    [Benchmark]
    public async Task QueryAllRecords()
    {
        await ((ICollectionEngine)_engine).QueryAsync("bench_db", "bench_coll", new QueryOptions());
    }

    [Benchmark]
    public async Task InsertBatch()
    {
        for (var i = 0; i < 100; i++)
        {
            await ((ICollectionEngine)_engine).InsertAsync("bench_db", "bench_coll",
                new Dictionary<string, object?> { { "idx", i }, { "data", Guid.NewGuid().ToString() } });
        }
    }

    public void Dispose()
    {
        _engine.Dispose();
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
        GC.SuppressFinalize(this);
    }
}
