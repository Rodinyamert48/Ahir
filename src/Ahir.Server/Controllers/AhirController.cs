using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ahir.Server.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class AhirController : ControllerBase
{
    private readonly IServerHost _host;
    private readonly MonitorService _monitor;

    public AhirController(IServerHost host, MonitorService monitor)
    {
        _host = host;
        _monitor = monitor;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new
    {
        status = _host.State == ServerState.Running ? "healthy" : "unhealthy",
        instance = _host.InstanceId,
        uptime = DateTime.UtcNow - _host.StartedAt,
        state = _host.State.ToString()
    });

    [HttpGet("metrics")]
    public IActionResult Metrics() => Ok(_monitor.GetCurrentMetrics());

    [HttpGet("server/info")]
    public IActionResult ServerInfo() => Ok(new
    {
        version = "1.0.0",
        instanceId = _host.InstanceId,
        startedAt = _host.StartedAt,
        state = _host.State.ToString(),
        uptime = DateTime.UtcNow - _host.StartedAt
    });

    [HttpPost("server/restart")]
    public async Task<IActionResult> Restart(CancellationToken ct)
    {
        _ = Task.Run(() => _host.RestartAsync(ct), ct);
        return Accepted(new { message = "Server restart initiated" });
    }

    [HttpGet("databases")]
    public async Task<IActionResult> ListDatabases(CancellationToken ct)
    {
        var result = await _host.Database.ListAsync(ct);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    [HttpPost("databases")]
    public async Task<IActionResult> CreateDatabase([FromBody] CreateDatabaseRequest request, CancellationToken ct)
    {
        var result = await _host.Database.CreateAsync(request.Name, ct);
        return result.Success ? Created($"/api/v1/databases/{request.Name}", result.Data) : BadRequest(result);
    }

    [HttpGet("databases/{name}")]
    public async Task<IActionResult> GetDatabase(string name, CancellationToken ct)
    {
        var result = await _host.Database.GetInfoAsync(name, ct);
        return result.Success ? Ok(result.Data) : NotFound(result);
    }

    [HttpDelete("databases/{name}")]
    public async Task<IActionResult> DropDatabase(string name, CancellationToken ct)
    {
        var result = await _host.Database.DropAsync(name, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("databases/{name}/compact")]
    public async Task<IActionResult> CompactDatabase(string name, CancellationToken ct)
    {
        var result = await _host.Database.CompactAsync(name, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("databases/{database}/collections")]
    public async Task<IActionResult> ListCollections(string database, CancellationToken ct)
    {
        var dbResult = await _host.Database.GetInfoAsync(database, ct);
        if (!dbResult.Success) return NotFound(dbResult);

        var collections = new List<CollectionInfo>();
        var collectionNames = Directory.GetDirectories(
            Path.Combine(AppContext.BaseDirectory, "data", database));
        foreach (var dir in collectionNames)
        {
            var name = Path.GetFileName(dir);
            var info = await ((ICollectionEngine)_host.Database).GetInfoAsync(database, name, ct);
            if (info.Success && info.Data != null)
                collections.Add(info.Data);
        }
        return Ok(collections);
    }

    [HttpPost("databases/{database}/collections")]
    public async Task<IActionResult> CreateCollection(string database, [FromBody] CreateCollectionRequest request, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).CreateAsync(database, request.Name, ct);
        return result.Success ? Created($"/api/v1/databases/{database}/collections/{request.Name}", result.Data) : BadRequest(result);
    }

    [HttpDelete("databases/{database}/collections/{collection}")]
    public async Task<IActionResult> DropCollection(string database, string collection, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).DropAsync(database, collection, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("databases/{database}/collections/{collection}/records")]
    public async Task<IActionResult> InsertRecord(string database, string collection, [FromBody] Dictionary<string, object?> fields, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).InsertAsync(database, collection, fields, ct);
        return result.Success
            ? Created($"/api/v1/databases/{database}/collections/{collection}/records/{result.Data?.Id}", result.Data)
            : BadRequest(result);
    }

    [HttpGet("databases/{database}/collections/{collection}/records/{id}")]
    public async Task<IActionResult> GetRecord(string database, string collection, string id, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).GetAsync(database, collection, id, ct);
        return result.Success ? Ok(result.Data) : NotFound(result);
    }

    [HttpPut("databases/{database}/collections/{collection}/records/{id}")]
    public async Task<IActionResult> UpdateRecord(string database, string collection, string id, [FromBody] Dictionary<string, object?> fields, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).UpdateAsync(database, collection, id, fields, ct);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    [HttpDelete("databases/{database}/collections/{collection}/records/{id}")]
    public async Task<IActionResult> DeleteRecord(string database, string collection, string id, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).DeleteAsync(database, collection, id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("databases/{database}/collections/{collection}/records/query")]
    public async Task<IActionResult> QueryRecords(string database, string collection, [FromBody] QueryOptions options, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).QueryAsync(database, collection, options, ct);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    [HttpPost("databases/{database}/collections/{collection}/records/aggregate")]
    public async Task<IActionResult> AggregateRecords(string database, string collection, [FromBody] AggregateRequest request, CancellationToken ct)
    {
        var allResult = await ((ICollectionEngine)_host.Database).QueryAsync(database, collection, new QueryOptions { PageSize = int.MaxValue }, ct);
        if (!allResult.Success || allResult.Data == null)
            return BadRequest(allResult);

        var records = allResult.Data.Items;
        var result = new Dictionary<string, object>();

        foreach (var agg in request.Aggregations)
        {
            var values = records
                .Select(r => r.Fields.TryGetValue(agg.Field, out var v) ? v : null)
                .Where(v => v != null)
                .ToList();

            if (values.Count == 0) { result[$"{agg.Type}_{agg.Field}"] = null; continue; }

            try
            {
                var nums = values.Select(v => Convert.ToDouble(v)).ToList();
                result[$"{agg.Type}_{agg.Field}"] = agg.Type.ToLowerInvariant() switch
                {
                    "sum" => nums.Sum(),
                    "avg" => nums.Average(),
                    "min" => nums.Min(),
                    "max" => nums.Max(),
                    "count" => nums.Count,
                    _ => null
                };
            }
            catch
            {
                result[$"{agg.Type}_{agg.Field}"] = agg.Type.ToLowerInvariant() switch
                {
                    "count" => values.Count,
                    "min" => values.Min(),
                    "max" => values.Max(),
                    _ => null
                };
            }
        }

        return Ok(new { database, collection, totalRecords = records.Count, aggregations = result });
    }

    [HttpGet("databases/{database}/collections/{collection}/count")]
    public async Task<IActionResult> CountRecords(string database, string collection, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).CountAsync(database, collection, null, ct);
        return result.Success ? Ok(new { count = result.Data }) : BadRequest(result);
    }

    [HttpPost("databases/{database}/collections/{collection}/indexes")]
    public async Task<IActionResult> CreateIndex(string database, string collection, [FromBody] CreateIndexRequest request, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).CreateIndexAsync(database, collection, request.Field, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("databases/{database}/collections/{collection}/indexes/{field}")]
    public async Task<IActionResult> DropIndex(string database, string collection, string field, CancellationToken ct)
    {
        var result = await ((ICollectionEngine)_host.Database).DropIndexAsync(database, collection, field, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("storage/{bucket}")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> UploadFile(string bucket, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await _host.Storage.UploadAsync(bucket, file.FileName, stream, false, ct);
        return result.Success ? Created($"/api/v1/storage/{bucket}/{file.FileName}", result) : BadRequest(result);
    }

    [HttpGet("storage/{bucket}/{**name}")]
    public async Task<IActionResult> DownloadFile(string bucket, string name, CancellationToken ct)
    {
        var result = await _host.Storage.DownloadAsync(bucket, name, ct);
        if (!result.Success || result.Data == null) return NotFound(result);
        return File(result.Data, "application/octet-stream", name);
    }

    [HttpDelete("storage/{bucket}/{**name}")]
    public async Task<IActionResult> DeleteFile(string bucket, string name, CancellationToken ct)
    {
        var result = await _host.Storage.DeleteAsync(bucket, name, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("backup")]
    public async Task<IActionResult> CreateBackup([FromBody] BackupRequest? request, CancellationToken ct)
    {
        var result = await _host.Backup.CreateBackupAsync(request?.Database, BackupType.Full, ct);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }

    [HttpGet("backup")]
    public async Task<IActionResult> ListBackups(CancellationToken ct)
    {
        var result = await _host.Backup.ListBackupsAsync(ct);
        return Ok(result.Data ?? Array.Empty<BackupInfo>());
    }

    [HttpPost("backup/{id}/restore")]
    public async Task<IActionResult> RestoreBackup(string id, CancellationToken ct)
    {
        var result = await _host.Backup.RestoreAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("backup/{id}")]
    public async Task<IActionResult> DeleteBackup(string id, CancellationToken ct)
    {
        var result = await _host.Backup.DeleteBackupAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }
}

public sealed record CreateDatabaseRequest(string Name);
public sealed record CreateCollectionRequest(string Name);
public sealed record CreateIndexRequest(string Field);
public sealed record BackupRequest(string? Database);
public sealed record AggregateRequest(List<AggregateField> Aggregations);
public sealed record AggregateField(string Field, string Type);
