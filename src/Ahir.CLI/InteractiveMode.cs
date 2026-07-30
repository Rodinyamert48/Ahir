using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Server;
using Serilog;

namespace Ahir.CLI;

public sealed class InteractiveMode
{
    private readonly AhirServerHost _host;
    private readonly IDatabaseEngine _database;
    private bool _running = true;

    public InteractiveMode(AhirServerHost host)
    {
        _host = host;
        _database = host.Database;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Ahir Interactive Shell");
        Console.WriteLine("Type 'help' for commands, 'exit' to quit.");
        Console.WriteLine();

        while (_running)
        {
            Console.Write("ahir> ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            try
            {
                await ExecuteCommandAsync(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private async Task ExecuteCommandAsync(string input)
    {
        var parts = ParseCommand(input);
        if (parts.Count == 0) return;

        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        switch (cmd)
        {
            case "exit":
            case "quit":
                _running = false;
                Console.WriteLine("Goodbye.");
                break;

            case "help":
                ShowHelp();
                break;

            case "status":
                ShowStatus();
                break;

            case "databases":
                await ListDatabasesAsync();
                break;

            case "use":
                await UseDatabaseAsync(args);
                break;

            case "collections":
                await ListCollectionsAsync();
                break;

            case "insert":
                await InsertRecordAsync(args);
                break;

            case "query":
                await QueryRecordsAsync(args);
                break;

            case "get":
                await GetRecordAsync(args);
                break;

            case "delete":
                await DeleteRecordAsync(args);
                break;

            case "count":
                await CountRecordsAsync(args);
                break;

            case "backup":
                await CreateBackupAsync();
                break;

            case "backups":
                await ListBackupsAsync();
                break;

            case "metrics":
                ShowMetrics();
                break;

            default:
                Console.WriteLine($"Unknown command: {cmd}. Type 'help' for available commands.");
                break;
        }
    }

    private void ShowHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  status                  Show server status");
        Console.WriteLine("  databases               List databases");
        Console.WriteLine("  use <database>           Switch to a database");
        Console.WriteLine("  collections              List collections (use db first)");
        Console.WriteLine("  insert <coll> <json>     Insert a record");
        Console.WriteLine("  query <coll> [filter]    Query records (e.g., query users field=value)");
        Console.WriteLine("  get <coll> <id>          Get record by ID");
        Console.WriteLine("  delete <coll> <id>       Delete a record");
        Console.WriteLine("  count <coll>             Count records");
        Console.WriteLine("  backup                   Create backup");
        Console.WriteLine("  backups                  List backups");
        Console.WriteLine("  metrics                  Show system metrics");
        Console.WriteLine("  exit                     Exit interactive mode");
    }

    private void ShowStatus()
    {
        Console.WriteLine($"State: {_host.State}");
        Console.WriteLine($"Uptime: {DateTime.UtcNow - _host.StartedAt:g}");
        Console.WriteLine($"Instance: {_host.InstanceId}");
    }

    private async Task ListDatabasesAsync()
    {
        var result = await _database.ListAsync();
        if (result.Success && result.Data != null)
        {
            foreach (var db in result.Data)
                Console.WriteLine($"  {db.Name,-20} {db.CollectionCount} collections, {db.RecordCount} records, {db.SizeBytes / 1024} KB");
        }
    }

    private string _currentDb = string.Empty;

    private async Task UseDatabaseAsync(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine("Usage: use <database>"); return; }
        var result = await _database.GetInfoAsync(args[0]);
        if (result.Success)
        {
            _currentDb = args[0];
            Console.WriteLine($"Using database: {_currentDb}");
        }
        else
        {
            Console.WriteLine($"Database '{args[0]}' not found.");
        }
    }

    private async Task ListCollectionsAsync()
    {
        if (string.IsNullOrEmpty(_currentDb)) { Console.WriteLine("No database selected. Use 'use <database>' first."); return; }
        var dbPath = Path.Combine(AppContext.BaseDirectory, "data", _currentDb);
        if (!Directory.Exists(dbPath)) { Console.WriteLine("No collections found."); return; }
        foreach (var dir in Directory.GetDirectories(dbPath))
            Console.WriteLine($"  {Path.GetFileName(dir)}");
    }

    private async Task InsertRecordAsync(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("Usage: insert <collection> <json>"); return; }
        if (string.IsNullOrEmpty(_currentDb)) { Console.WriteLine("No database selected."); return; }
        try
        {
            var fields = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(string.Join(" ", args[1..]));
            if (fields == null) { Console.WriteLine("Invalid JSON."); return; }
            var result = await ((ICollectionEngine)_database).InsertAsync(_currentDb, args[0], fields);
            Console.WriteLine(result.Success ? $"Inserted: {result.Data?.Id}" : $"Error: {result.ErrorMessage}");
        }
        catch { Console.WriteLine("Invalid JSON format."); }
    }

    private async Task QueryRecordsAsync(string[] args)
    {
        if (args.Length < 1) { Console.WriteLine("Usage: query <collection> [field=value]"); return; }
        if (string.IsNullOrEmpty(_currentDb)) { Console.WriteLine("No database selected."); return; }

        var filters = new List<QueryFilter>();
        for (var i = 1; i < args.Length; i++)
        {
            var parts = args[i].Split('=', 2);
            if (parts.Length == 2)
                filters.Add(new QueryFilter { Field = parts[0], Operator = FilterOperator.Equals, Value = parts[1] });
        }

        var options = new QueryOptions { Filters = filters, Page = 1, PageSize = 10 };
        var result = await ((ICollectionEngine)_database).QueryAsync(_currentDb, args[0], options);
        if (result.Success && result.Data != null)
        {
            Console.WriteLine($"Found {result.Data.TotalCount} records (showing {result.Data.Items.Count}):");
            foreach (var record in result.Data.Items)
            {
                var fields = string.Join(", ", record.Fields.Select(f => $"{f.Key}={f.Value}"));
                Console.WriteLine($"  [{record.Id[..8]}] {fields}");
            }
        }
    }

    private async Task GetRecordAsync(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("Usage: get <collection> <id>"); return; }
        if (string.IsNullOrEmpty(_currentDb)) { Console.WriteLine("No database selected."); return; }
        var result = await ((ICollectionEngine)_database).GetAsync(_currentDb, args[0], args[1]);
        if (result.Success && result.Data != null)
        {
            Console.WriteLine($"ID: {result.Data.Id}");
            foreach (var f in result.Data.Fields)
                Console.WriteLine($"  {f.Key}: {f.Value}");
        }
        else Console.WriteLine("Not found.");
    }

    private async Task DeleteRecordAsync(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("Usage: delete <collection> <id>"); return; }
        if (string.IsNullOrEmpty(_currentDb)) { Console.WriteLine("No database selected."); return; }
        var result = await ((ICollectionEngine)_database).DeleteAsync(_currentDb, args[0], args[1]);
        Console.WriteLine(result.Success ? "Deleted." : $"Error: {result.ErrorMessage}");
    }

    private async Task CountRecordsAsync(string[] args)
    {
        if (args.Length < 1) { Console.WriteLine("Usage: count <collection>"); return; }
        if (string.IsNullOrEmpty(_currentDb)) { Console.WriteLine("No database selected."); return; }
        var result = await ((ICollectionEngine)_database).CountAsync(_currentDb, args[0]);
        Console.WriteLine(result.Success ? $"Count: {result.Data}" : $"Error: {result.ErrorMessage}");
    }

    private async Task CreateBackupAsync()
    {
        var result = await _host.Backup.CreateBackupAsync();
        Console.WriteLine(result.Success ? $"Backup created: {result.Data?.Id}" : $"Error: {result.ErrorMessage}");
    }

    private async Task ListBackupsAsync()
    {
        var result = await _host.Backup.ListBackupsAsync();
        if (result.Success && result.Data != null)
        {
            foreach (var b in result.Data)
                Console.WriteLine($"  {b.Id,-20} {b.Type,-12} {b.Status,-12} {b.SizeBytes / 1024} KB");
        }
    }

    private void ShowMetrics()
    {
        var m = _host.MonitorService.GetCurrentMetrics();
        Console.WriteLine($"CPU: {m.CpuUsagePercent}%");
        Console.WriteLine($"Memory: {m.MemoryUsageBytes / 1024 / 1024} MB");
        Console.WriteLine($"Requests: {m.TotalRequests}");
        Console.WriteLine($"Connections: {m.ActiveConnections}");
        Console.WriteLine($"WebSockets: {m.ActiveWebSockets}");
        Console.WriteLine($"DB Size: {m.DatabaseSizeBytes / 1024 / 1024} MB");
    }

    private static List<string> ParseCommand(string input)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); }
            }
            else current.Append(c);
        }
        if (current.Length > 0) result.Add(current.ToString());
        return result;
    }
}
