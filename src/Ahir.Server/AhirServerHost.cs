using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Server.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Ahir.Server;

public sealed class AhirServerHost : IServerHost
{
    private WebApplication? _app;
    private readonly AhirConfig _config;
    private readonly string[] _urls;
    private readonly Lock _lock = new();

    public string InstanceId { get; } = Guid.NewGuid().ToString("N");
    public DateTime StartedAt { get; private set; }
    public ServerState State { get; private set; } = ServerState.Stopped;
    public IDatabaseEngine Database { get; }
    public IStorageEngine Storage { get; }
    public ISecurityProvider Security { get; }
    public IRealtimeEngine Realtime { get; }
    public IPluginEngine Plugin { get; }
    public IBackupService Backup { get; } = null!;
    public IMonitorService Monitor { get; } = null!;

    public AhirServerHost(AhirConfig config, IDatabaseEngine database, IStorageEngine storage,
        ISecurityProvider security, IRealtimeEngine realtime, IPluginEngine plugin)
    {
        _config = config;
        Database = database;
        Storage = storage;
        Security = security;
        Realtime = realtime;
        Plugin = plugin;

        var urls = new List<string>
        {
            $"http://0.0.0.0:{config.Server.HttpPort}"
        };
        if (config.Server.EnableSsl)
            urls.Add($"https://0.0.0.0:{config.Server.HttpsPort}");

        _urls = urls.ToArray();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (State == ServerState.Running) return;
            State = ServerState.Starting;
        }

        Log.Information("Starting Ahir Server v{Version} on {Urls}",
            Ahir.Core.Constants.AhirConstants.Version, string.Join(", ", _urls));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(_urls);

        builder.Services.AddSingleton(_config);
        builder.Services.AddSingleton(Database);
        builder.Services.AddSingleton(Storage);
        builder.Services.AddSingleton(Security);
        builder.Services.AddSingleton(Realtime);
        builder.Services.AddSingleton(Plugin);
        builder.Services.AddSingleton<IServerHost>(this);

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (!string.IsNullOrEmpty(_config.Server.AllowedOrigins))
                    policy.WithOrigins(_config.Server.AllowedOrigins.Split(';'))
                        .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                else
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        builder.Host.UseSerilog();

        _app = builder.Build();

        _app.UseMiddleware<IpFilterMiddleware>();
        _app.UseMiddleware<SecurityHeadersMiddleware>();
        _app.UseMiddleware<RequestLoggingMiddleware>();
        if (_config.Server.EnableRateLimiting)
            _app.UseMiddleware<RateLimiterMiddleware>();

        _app.UseCors();
        _app.MapGet("/health", () => Results.Ok(new { status = "healthy", instance = InstanceId, uptime = DateTime.UtcNow - StartedAt }));
        _app.MapGet("/metrics", () => Results.Ok(Monitor?.GetCurrentMetrics()));

        StartedAt = DateTime.UtcNow;
        State = ServerState.Running;

        Log.Information("Ahir Server started successfully");
        await _app.StartAsync(cancellationToken);
        await _app.WaitForShutdownAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        State = ServerState.Stopping;
        Log.Information("Stopping Ahir Server...");

        if (_app != null)
            await _app.StopAsync(cancellationToken);

        State = ServerState.Stopped;
        Log.Information("Ahir Server stopped");
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        State = ServerState.Restarting;
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    public async Task<AhirResult<bool>> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return AhirResult<bool>.Ok(State == ServerState.Running);
    }
}

public static class AhirServerExtensions
{
    public static IServiceCollection AddAhirServer(this IServiceCollection services, AhirConfig config)
    {
        // Service registration will be done via the host
        return services;
    }
}