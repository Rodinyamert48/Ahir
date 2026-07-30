using System.Net;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Security;

namespace Ahir.Server.Middleware;

public sealed class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServerHost _host;
    private static readonly HashSet<string> s_publicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/login", "/api/v1/auth/register", "/api/v1/auth/refresh",
        "/health", "/metrics", "/api/v1/health", "/api/v1/server/info",
        "/ws"
    };

    public AuthMiddleware(RequestDelegate next, IServerHost host)
    {
        _next = next;
        _host = host;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsPublicPath(context.Request.Path) || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(AhirResult<object>.Fail("AUTH_REQUIRED", "Bearer token required."));
            return;
        }

        var token = authHeader["Bearer ".Length..];
        var result = await _host.Security.ValidateTokenAsync(token);

        if (!result.Success || result.Data == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(AhirResult<object>.Fail("TOKEN_INVALID", "Invalid or expired token."));
            return;
        }

        context.Items["User"] = result.Data;

        var requiredPermission = GetRequiredPermission(context);
        if (requiredPermission != null && !HasPermission(result.Data, requiredPermission))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsJsonAsync(AhirResult<object>.Fail("FORBIDDEN", $"Permission '{requiredPermission}' required."));
            return;
        }

        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        foreach (var p in s_publicPaths)
            if (path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static string? GetRequiredPermission(HttpContext context)
    {
        var method = context.Request.Method.ToUpperInvariant();
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        if (path.Contains("/databases"))
        {
            if (method == "GET") return "database.read";
            if (method == "POST" || method == "DELETE") return "database.write";
        }
        if (path.Contains("/collections") && path.Contains("/records"))
        {
            if (method == "GET") return "record.read";
            if (method == "POST" || method == "PUT" || method == "DELETE") return "record.write";
        }
        if (path.Contains("/storage"))
        {
            if (method == "GET") return "storage.read";
            if (method == "POST" || method == "DELETE") return "storage.write";
        }
        if (path.Contains("/backup"))
        {
            if (method == "GET") return "monitor.read";
            return "backup.*";
        }
        if (path.Contains("/config")) return "config.*";
        if (path.Contains("/plugins")) return "plugin.*";
        if (path.Contains("/users") && !path.Contains("/auth")) return "user.*";

        return null;
    }

    private static bool HasPermission(UserInfo user, string permission)
    {
        foreach (var role in user.Roles)
        {
            foreach (var perm in s_rolePermissions.TryGetValue(role, out var perms) ? perms : [])
            {
                if (MatchPermission(perm, permission))
                    return true;
            }
        }
        foreach (var perm in user.Permissions)
        {
            if (MatchPermission(perm, permission))
                return true;
        }
        return false;
    }

    private static readonly Dictionary<string, string[]> s_rolePermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = ["server.*", "database.*", "collection.*", "record.*", "storage.*", "user.*", "plugin.*", "backup.*", "config.*", "monitor.*"],
        ["editor"] = ["database.read", "database.write", "collection.read", "collection.write", "record.*", "storage.read", "storage.write"],
        ["viewer"] = ["database.read", "collection.read", "record.read", "storage.read", "monitor.read"]
    };

    private static bool MatchPermission(string pattern, string required)
    {
        if (pattern.EndsWith(".*"))
            return required.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
        return string.Equals(pattern, required, StringComparison.OrdinalIgnoreCase);
    }
}
