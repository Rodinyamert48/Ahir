using Ahir.Core.Interfaces;
using Ahir.Core.Models;

namespace Ahir.Plugin.Samples;

public sealed class StatusPlugin : AhirPlugin
{
    private Timer? _timer;

    public StatusPlugin()
    {
        Id = "plugin.status";
        Name = "Status Reporter";
        Version = "1.0.0";
        Author = "Ahir";
        Description = "Periodically reports server status to console.";
    }

    public override Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[StatusPlugin] Loaded.");
        return Task.CompletedTask;
    }

    public override Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _timer = new Timer(ReportStatus, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
        Console.WriteLine("[StatusPlugin] Started — reporting every 5 minutes.");
        return Task.CompletedTask;
    }

    public override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _timer?.Dispose();
        _timer = null;
        Console.WriteLine("[StatusPlugin] Stopped.");
        return Task.CompletedTask;
    }

    public override Task OnUnloadAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[StatusPlugin] Unloaded.");
        return Task.CompletedTask;
    }

    private void ReportStatus(object? state)
    {
        if (Host == null) return;
        try
        {
            var db = Host.Database;
            Console.WriteLine($"[StatusPlugin] DB: {db.Name}, Collections: {db.Info.CollectionCount}, Records: {db.Info.RecordCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StatusPlugin] Error: {ex.Message}");
        }
    }
}
