using Ahir.CLI;
using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Database;
using Ahir.Plugin;
using Ahir.Realtime;
using Ahir.Security;
using Ahir.Server;
using Ahir.Storage.Engines;

var config = new AhirConfig();
var database = new DatabaseEngine(config.Database);
var storage = new StorageEngine(config.Storage);
var security = new SecurityProvider(config.Security);
var realtime = new RealtimeEngine();
var logger = new ConsoleLogger();
var pluginEngine = new PluginEngine("plugins", null!);

var host = new AhirServerHost(config, database, storage, security, realtime, pluginEngine);

var app = new AhirCliApp(
    start: () => host.StartAsync(),
    stop: () => host.StopAsync(),
    restart: () => host.RestartAsync(),
    status: () =>
    {
        Console.WriteLine($"Instance ID: {host.InstanceId}");
        Console.WriteLine($"State: {host.State}");
        Console.WriteLine($"Started At: {host.StartedAt}");
        Console.WriteLine($"Uptime: {DateTime.UtcNow - host.StartedAt}");
        Console.WriteLine($"Database: {database.Name}");
        Console.WriteLine($"Collections: {database.Info.CollectionCount}");
        Console.WriteLine($"Plugins: {pluginEngine.GetLoadedPlugins().Count}");
    },
    backup: async () =>
    {
        Console.WriteLine("Creating backup...");
        try
        {
            var result = await host.Backup.CreateBackupAsync();
            if (result.Success && result.Data != null)
                Console.WriteLine($"Backup created: {result.Data.Id} ({result.Data.SizeBytes / 1024 / 1024} MB)");
            else
                Console.WriteLine($"Backup failed: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Backup failed: {ex.Message}");
        }
    },
    restore: async (id) =>
    {
        Console.WriteLine($"Restoring from backup {id}...");
        try
        {
            var result = await host.Backup.RestoreAsync(id);
            if (result.Success)
                Console.WriteLine("Restore completed.");
            else
                Console.WriteLine($"Restore failed: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Restore failed: {ex.Message}");
        }
    },
    logs: () => Console.WriteLine("Use the Dashboard to view logs."),
    config: () => Console.WriteLine("Use the Dashboard or Steps to manage configuration."),
    doctor: () =>
    {
        Console.WriteLine("Running system diagnostics...");
        try
        {
            var metrics = host.MonitorService.GetCurrentMetrics();
            Console.WriteLine($"CPU: {metrics.CpuUsagePercent}%");
            Console.WriteLine($"Memory: {metrics.MemoryUsageBytes / 1024 / 1024} MB");
            Console.WriteLine($"Database Size: {metrics.DatabaseSizeBytes / 1024 / 1024} MB");
            Console.WriteLine($"Storage Size: {metrics.StorageSizeBytes / 1024 / 1024} MB");
            Console.WriteLine($"Active Connections: {metrics.ActiveConnections}");
            Console.WriteLine($"Active WebSockets: {metrics.ActiveWebSockets}");
            Console.WriteLine($"Total Requests: {metrics.TotalRequests}");
            Console.WriteLine($"Uptime: {TimeSpan.FromSeconds(metrics.UptimeSeconds):g}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Diagnostics failed: {ex.Message}");
        }
    },
    interactive: () => RunInteractiveAsync()
);

return await app.RunAsync(args);

async Task RunInteractiveAsync()
{
    var interactive = new InteractiveMode(host);
    await interactive.RunAsync();
}

internal sealed class ConsoleLogger : ILogService
{
    public void Debug(string message) => Console.WriteLine($"[DEBUG] {message}");
    public void Info(string message) => Console.WriteLine($"[INFO] {message}");
    public void Warning(string message) => Console.WriteLine($"[WARN] {message}");
    public void Error(string message) => Console.WriteLine($"[ERROR] {message}");
    public void Error(Exception exception, string message) => Console.WriteLine($"[ERROR] {message}: {exception.Message}");
    public void Fatal(string message) => Console.WriteLine($"[FATAL] {message}");
    public void Fatal(Exception exception, string message) => Console.WriteLine($"[FATAL] {message}: {exception.Message}");
    public IDisposable BeginScope(string key, object value) => new DummyScope();
    private sealed class DummyScope : IDisposable { public void Dispose() { } }
}
