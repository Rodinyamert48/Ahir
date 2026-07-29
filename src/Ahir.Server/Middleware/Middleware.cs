using System.Net;
using Ahir.Core.Configuration;

namespace Ahir.Server.Middleware;

public sealed class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ServerConfig _config;
    private readonly Dictionary<string, RateLimitEntry> _clients = new();
    private readonly Lock _lock = new();

    public RateLimiterMiddleware(RequestDelegate next, ServerConfig config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_config.EnableRateLimiting)
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        lock (_lock)
        {
            if (!_clients.TryGetValue(clientIp, out var entry))
            {
                entry = new RateLimitEntry();
                _clients[clientIp] = entry;
            }

            // Reset window if expired
            if (now - entry.WindowStart > 60)
            {
                entry.WindowStart = now;
                entry.RequestCount = 0;
            }

            entry.RequestCount++;

            if (entry.RequestCount > _config.MaxConnections)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.RetryAfter = "60";
                return;
            }
        }

        await _next(context);
    }

    private sealed class RateLimitEntry
    {
        public long WindowStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public int RequestCount;
    }
}

public sealed class IpFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityConfig _config;

    public IpFilterMiddleware(RequestDelegate next, SecurityConfig config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        if (_config.BlacklistedIps?.Contains(ip) == true)
        {
            context.Response.StatusCode = 403;
            return;
        }

        if (_config.WhitelistedIps?.Count > 0 && _config.WhitelistedIps?.Contains(ip) != true)
        {
            context.Response.StatusCode = 403;
            return;
        }

        await _next(context);
    }
}

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.XXSSProtection = "0";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";

        await _next(context);
    }
}

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = DateTimeOffset.UtcNow;
        await _next(context);
        var elapsed = DateTimeOffset.UtcNow - start;

        _logger.LogInformation(
            "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsed.TotalMilliseconds);
    }
}