using System.Text.Json;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Core.Utilities;

namespace Ahir.Core.Services;

public sealed class MigrationService
{
    private readonly IDatabaseEngine _database;
    private readonly ICollectionEngine _collection;
    private const string MigrationCollection = "_migrations";
    private const string SystemDb = "_system";

    public MigrationService(IDatabaseEngine database)
    {
        _database = database;
        _collection = (ICollectionEngine)database;
    }

    public async Task<AhirResult<bool>> ApplyAsync(Migration migration)
    {
        try
        {
            await EnsureSystemDbAsync();

            var existing = await _collection.QueryAsync(SystemDb, MigrationCollection,
                new QueryOptions { Filters = new[] { new QueryFilter { Field = "id", Operator = FilterOperator.Equals, Value = migration.Id } } });

            if (existing.Success && existing.Data != null && existing.Data.TotalCount > 0)
                return AhirResult<bool>.Ok(true);

            migration.AppliedAt = DateTime.UtcNow;

            await _collection.InsertAsync(SystemDb, MigrationCollection, new Dictionary<string, object?>
            {
                ["id"] = migration.Id,
                ["description"] = migration.Description,
                ["script"] = migration.Script,
                ["appliedAt"] = migration.AppliedAt?.ToString("O") ?? DateTime.UtcNow.ToString("O")
            });

            Console.WriteLine($"[Migration] Applied: {migration.Id} — {migration.Description}");
            return AhirResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return AhirResult<bool>.Fail("MIGRATION_FAILED", ex.Message);
        }
    }

    public async Task<AhirResult<IReadOnlyList<Migration>>> ListAsync()
    {
        await EnsureSystemDbAsync();
        var result = await _collection.QueryAsync(SystemDb, MigrationCollection, new QueryOptions { PageSize = 1000 });
        if (!result.Success || result.Data == null)
            return AhirResult<IReadOnlyList<Migration>>.Ok(Array.Empty<Migration>());

        var migrations = result.Data.Items.Select(r => new Migration
        {
            Id = r.Fields.GetValueOrDefault("id")?.ToString() ?? string.Empty,
            Description = r.Fields.GetValueOrDefault("description")?.ToString() ?? string.Empty,
            Script = r.Fields.GetValueOrDefault("script")?.ToString() ?? string.Empty,
            AppliedAt = DateTime.TryParse(r.Fields.GetValueOrDefault("appliedAt")?.ToString(), out var dt) ? dt : null
        }).ToList() as IReadOnlyList<Migration>;

        return AhirResult<IReadOnlyList<Migration>>.Ok(migrations ?? Array.Empty<Migration>());
    }

    public async Task RunPendingMigrationsAsync(List<Migration> migrations)
    {
        var applied = await ListAsync();
        var appliedIds = applied.Success && applied.Data != null
            ? new HashSet<string>(applied.Data.Select(m => m.Id))
            : new HashSet<string>();

        foreach (var migration in migrations.OrderBy(m => m.Id))
        {
            if (!appliedIds.Contains(migration.Id))
                await ApplyAsync(migration);
        }
    }

    private async Task EnsureSystemDbAsync()
    {
        var dbResult = await _database.CreateAsync(SystemDb);
        if (!dbResult.Success && dbResult.ErrorCode != "ALREADY_EXISTS")
            throw new InvalidOperationException($"Failed to create system database: {dbResult.ErrorMessage}");

        await _database.OpenAsync(SystemDb);
        var colResult = await _collection.CreateAsync(SystemDb, MigrationCollection);
        if (!colResult.Success && colResult.ErrorCode != "ALREADY_EXISTS")
            throw new InvalidOperationException($"Failed to create migrations collection: {colResult.ErrorMessage}");
    }
}

public sealed class Migration
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }
}
