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

var host = new AhirServerHost(config, database, storage, security, realtime, new PluginEngine("plugins", null!));

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
    },
    backup: async () =>
    {
        Console.WriteLine("Creating backup...");
        var result = await host.Backup.CreateBackupAsync();
        if (result.Success)
            Console.WriteLine($"Backup created: {result.Data?.Id}");
        else
            Console.WriteLine($"Backup failed: {result.ErrorMessage}");
    },
    restore: async (id) =>
    {
        Console.WriteLine($"Restoring from backup {id}...");
        var result = await host.Backup.RestoreAsync(id);
        if (result.Success)
            Console.WriteLine("Restore completed.");
        else
            Console.WriteLine($"Restore failed: {result.ErrorMessage}");
    },
    logs: () => Console.WriteLine("Use the Dashboard to view logs."),
    config: () => Console.WriteLine("Use the Dashboard or Steps to manage configuration."),
    doctor: () =>
    {
        Console.WriteLine("Running system diagnostics...");
        var metrics = host.Monitor.GetCurrentMetrics();
        Console.WriteLine($"CPU: {metrics.CpuUsagePercent}%");
        Console.WriteLine($"Memory: {metrics.MemoryUsageBytes / 1024 / 1024} MB");
        Console.WriteLine($"Database Size: {metrics.DatabaseSizeBytes / 1024 / 1024} MB");
        Console.WriteLine($"Active Connections: {metrics.ActiveConnections}");
        Console.WriteLine($"Uptime: {TimeSpan.FromSeconds(metrics.UptimeSeconds):g}");
    }
);

return await app.RunAsync(args);

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