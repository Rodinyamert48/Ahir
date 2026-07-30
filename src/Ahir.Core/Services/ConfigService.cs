using System.Text.Json;
using Ahir.Core.Configuration;
using Ahir.Core.Constants;
using Ahir.Core.Interfaces;

namespace Ahir.Core.Services;

public sealed class ConfigService : IConfigService
{
    private readonly string _configPath;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
    private static AhirConfig? s_cachedConfig;

    public ConfigService(string? configPath = null)
    {
        _configPath = configPath ?? Path.Combine(AppContext.BaseDirectory, AhirConstants.ConfigFileName);
    }

    public async Task<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : new()
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            return new T();

        try
        {
            var json = await File.ReadAllBytesAsync(fullPath, cancellationToken);
            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    public async Task SaveAsync<T>(string path, T config, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.SerializeToUtf8Bytes(config, s_jsonOptions);
        await File.WriteAllBytesAsync(fullPath, json, cancellationToken);
    }

    public T GetDefault<T>() where T : new() => new();

    public async Task<AhirConfig> LoadConfigAsync(CancellationToken cancellationToken = default)
    {
        s_cachedConfig = await LoadAsync<AhirConfig>(_configPath, cancellationToken);
        return s_cachedConfig ?? new AhirConfig();
    }

    public async Task SaveConfigAsync(AhirConfig config, CancellationToken cancellationToken = default)
    {
        await SaveAsync(_configPath, config, cancellationToken);
        s_cachedConfig = config;
    }

    public static AhirConfig? CachedConfig => s_cachedConfig;
}
