using System.Collections.Concurrent;
using System.Diagnostics;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Serilog;

namespace Ahir.Server.Services;

public sealed class MonitorService : IMonitorService, IDisposable
{
    private readonly IServerHost _host;
    private readonly Timer _timer;
    private readonly Stopwatch _uptime = Stopwatch.StartNew();
    private long _totalRequests;
    private long _activeConnections;
    private long _activeWebSockets;
    private long _diskReadBytes;
    private long _diskWriteBytes;
    private bool _disposed;

    public event Action<AhirMetrics>? OnMetricsUpdated;

    public MonitorService(IServerHost host)
    {
        _host = host;
        _timer = new Timer(PublishMetrics, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public AhirMetrics GetCurrentMetrics()
    {
        var process = Process.GetCurrentProcess();
        var totalMemory = process.WorkingSet64;
        var cpuTime = process.TotalProcessorTime.TotalSeconds;

        return new AhirMetrics
        {
            UptimeSeconds = (long)_uptime.Elapsed.TotalSeconds,
            TotalRequests = Interlocked.Read(ref _totalRequests),
            ActiveConnections = Interlocked.Read(ref _activeConnections),
            ActiveWebSockets = Interlocked.Read(ref _activeWebSockets),
            DatabaseSizeBytes = GetDirectorySize(_host.Database?.Name != null
                ? Path.Combine(AppContext.BaseDirectory, "data", _host.Database.Name)
                : Path.Combine(AppContext.BaseDirectory, "data")),
            StorageSizeBytes = GetDirectorySize(Path.Combine(AppContext.BaseDirectory, "storage")),
            CpuUsagePercent = Math.Round(cpuTime / Math.Max(1, _uptime.Elapsed.TotalSeconds) * 100, 2),
            MemoryUsageBytes = totalMemory,
            DiskReadBytes = Interlocked.Read(ref _diskReadBytes),
            DiskWriteBytes = Interlocked.Read(ref _diskWriteBytes),
            PluginCount = _host.Plugin?.GetLoadedPlugins().Count ?? 0,
            DatabaseCount = 1,
            ServerTime = DateTime.UtcNow
        };
    }

    public async IAsyncEnumerable<AhirMetrics> StreamMetricsAsync(TimeSpan interval, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return GetCurrentMetrics();
            await Task.Delay(interval, cancellationToken);
        }
    }

    public void IncrementRequests() => Interlocked.Increment(ref _totalRequests);
    public void AddConnection() => Interlocked.Increment(ref _activeConnections);
    public void RemoveConnection() => Interlocked.Decrement(ref _activeConnections);
    public void AddWebSocket() => Interlocked.Increment(ref _activeWebSockets);
    public void RemoveWebSocket() => Interlocked.Decrement(ref _activeWebSockets);
    public void AddDiskRead(long bytes) => Interlocked.Add(ref _diskReadBytes, bytes);
    public void AddDiskWrite(long bytes) => Interlocked.Add(ref _diskWriteBytes, bytes);

    private void PublishMetrics(object? state)
    {
        try
        {
            var metrics = GetCurrentMetrics();
            OnMetricsUpdated?.Invoke(metrics);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to publish metrics");
        }
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        try { return Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length); }
        catch { return 0; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
    }
}
