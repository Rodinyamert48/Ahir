using Ahir.Core.Interfaces;
using Ahir.Core.Models;

namespace Ahir.Plugin.Samples;

public sealed class LoggerPlugin : AhirPlugin
{
    public LoggerPlugin()
    {
        Id = "plugin.logger";
        Name = "Request Logger";
        Version = "1.0.0";
        Author = "Ahir";
        Description = "Logs all API requests to a file.";
    }

    public override Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[LoggerPlugin] Loaded.");
        return Task.CompletedTask;
    }

    public override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[LoggerPlugin] Started — logging to ahir_requests.log");
        await File.AppendAllTextAsync("ahir_requests.log",
            $"=== LoggerPlugin started at {DateTime.UtcNow:O} ===\n", cancellationToken);
    }

    public override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        await File.AppendAllTextAsync("ahir_requests.log",
            $"=== LoggerPlugin stopped at {DateTime.UtcNow:O} ===\n", cancellationToken);
        Console.WriteLine("[LoggerPlugin] Stopped.");
    }

    public override Task OnUnloadAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[LoggerPlugin] Unloaded.");
        return Task.CompletedTask;
    }
}
