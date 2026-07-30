using System.Text.Json;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Ahir.Server.Controllers;

[ApiController]
[Route("api/v1/events")]
public sealed class SseController : ControllerBase
{
    private readonly IRealtimeEngine _realtime;

    public SseController(IRealtimeEngine realtime)
    {
        _realtime = realtime;
    }

    [HttpGet("stream")]
    public async Task Stream([FromQuery] string? channel = "default", CancellationToken ct = default)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await _realtime.SubscribeAsync(channel ?? "default", async message =>
        {
            var json = JsonSerializer.Serialize(message);
            await Response.WriteAsync($"event: {message.EventType}\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        });

        // Keep connection open
        await Task.Delay(Timeout.Infinite, ct);
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request, CancellationToken ct)
    {
        await _realtime.PublishAsync(request.Channel, request.EventType, request.Data, ct);
        return Ok(new { sent = true });
    }
}

public sealed record PublishRequest(string Channel, string EventType, object? Data);
