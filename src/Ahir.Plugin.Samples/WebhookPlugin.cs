using System.Net.Http.Json;
using System.Text.Json;
using Ahir.Core.Events;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;

namespace Ahir.Plugin.Samples;

public sealed class WebhookPlugin : AhirPlugin, IDisposable
{
    private HttpClient _http = new();
    private CancellationTokenSource? _cts;

    public WebhookPlugin()
    {
        Id = "plugin.webhook";
        Name = "Webhook Forwarder";
        Version = "1.0.0";
        Author = "Ahir";
        Description = "Forwards database events to a webhook URL.";
    }

    public override async Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[WebhookPlugin] Loaded.");
        await Task.CompletedTask;
    }

    public override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _cts = new CancellationTokenSource();
        Console.WriteLine("[WebhookPlugin] Started. Listening for database events...");
        await Task.CompletedTask;
    }

    public override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        Console.WriteLine("[WebhookPlugin] Stopped.");
        return Task.CompletedTask;
    }

    public override Task OnUnloadAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[WebhookPlugin] Unloaded.");
        return Task.CompletedTask;
    }

    public void SetWebhookUrl(string url)
    {
        _http.BaseAddress = new Uri(url);
    }

    public async Task SendEventAsync(string eventType, object? data)
    {
        try
        {
            var payload = new { eventType, data, timestamp = DateTime.UtcNow };
            var response = await _http.PostAsJsonAsync("/webhook", payload, _cts?.Token ?? CancellationToken.None);
            Console.WriteLine($"[WebhookPlugin] Event {eventType} sent: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebhookPlugin] Failed to send webhook: {ex.Message}");
        }
    }

    public new void Dispose()
    {
        _http.Dispose();
        _cts?.Dispose();
        base.Dispose();
    }
}
