using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Server.Services;
using Serilog;

namespace Ahir.Server.Middleware;

public sealed class WebSocketHandler
{
    private readonly RequestDelegate _next;
    private readonly IRealtimeEngine _realtime;
    private readonly IServerHost _host;
    private readonly MonitorService? _monitor;
    private static readonly ConcurrentDictionary<string, WebSocket> s_connections = new();

    public WebSocketHandler(RequestDelegate next, IRealtimeEngine realtime, IServerHost host, MonitorService? monitor = null)
    {
        _next = next;
        _realtime = realtime;
        _host = host;
        _monitor = monitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/ws") && context.WebSockets.IsWebSocketRequest)
        {
            var channel = context.Request.Query["channel"].FirstOrDefault() ?? "default";
            var token = context.Request.Query["token"].FirstOrDefault();

            if (!string.IsNullOrEmpty(token))
            {
                var authResult = await _host.Security.ValidateTokenAsync(token);
                if (!authResult.Success)
                {
                    context.Response.StatusCode = 401;
                    return;
                }
            }

            WebSocket webSocket;
            try
            {
                webSocket = await context.WebSockets.AcceptWebSocketAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "WebSocket accept failed");
                context.Response.StatusCode = 500;
                return;
            }

            _monitor?.AddWebSocket();
            var connectionId = Guid.NewGuid().ToString("N");
            s_connections[connectionId] = webSocket;

            Log.Information("WebSocket connected: {Id} on channel {Channel}", connectionId, channel);

            try
            {
                await ReceiveLoop(webSocket, connectionId, channel);
            }
            finally
            {
                s_connections.TryRemove(connectionId, out _);
                _monitor?.RemoveWebSocket();
                Log.Information("WebSocket disconnected: {Id}", connectionId);
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task ReceiveLoop(WebSocket webSocket, string connectionId, string channel)
    {
        var buffer = new byte[1024 * 4];

        await _realtime.SubscribeAsync(channel, async message =>
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        });

        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            catch
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                try
                {
                    var message = JsonSerializer.Deserialize<RealtimeMessage>(json);
                    if (message != null)
                    {
                        await _realtime.PublishAsync(channel, message.EventType, message.Data);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Invalid WebSocket message from {Id}", connectionId);
                }
            }
        }
    }
}
