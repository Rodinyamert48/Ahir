using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;

namespace Ahir.Plugin;

public sealed class PluginEngine : IPluginEngine
{
    private readonly ConcurrentDictionary<string, PluginInstance> _plugins = new();
    private readonly string _pluginPath;
    private readonly IServerHost _host;

    public PluginEngine(string pluginPath, IServerHost host)
    {
        _pluginPath = pluginPath;
        _host = host;
        Directory.CreateDirectory(_pluginPath);
    }

    public async Task<AhirResult<PluginInfo>> LoadAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.IsPathRooted(pluginPath) ? pluginPath : Path.Combine(_pluginPath, pluginPath);
            if (!File.Exists(fullPath))
                return AhirResult<PluginInfo>.Fail("NOT_FOUND", $"Plugin file not found: {pluginPath}");

            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
            var pluginType = assembly.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(AhirPlugin)));
            if (pluginType == null)
                return AhirResult<PluginInfo>.Fail("PLUGIN_INVALID", "No valid plugin type found in assembly.");

            var plugin = (AhirPlugin)Activator.CreateInstance(pluginType)!;
            plugin.Host = _host;

            var id = plugin.Id;
            if (string.IsNullOrEmpty(id))
                id = Path.GetFileNameWithoutExtension(pluginPath);

            var instance = new PluginInstance
            {
                Plugin = plugin,
                Assembly = assembly,
                LoadContext = AssemblyLoadContext.Default
            };

            _plugins[id] = instance;
            await plugin.OnLoadAsync(cancellationToken);

            return AhirResult<PluginInfo>.Ok(new PluginInfo
            {
                Id = id,
                Name = plugin.Name,
                Version = plugin.Version,
                Author = plugin.Author,
                Description = plugin.Description,
                State = PluginState.Loaded
            });
        }
        catch (Exception ex)
        {
            return AhirResult<PluginInfo>.Fail("PLUGIN_LOAD_FAILED", $"Failed to load plugin: {ex.Message}");
        }
    }

    public async Task<AhirResult<bool>> UnloadAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_plugins.TryRemove(pluginId, out var instance))
        {
            await instance.Plugin.OnUnloadAsync(cancellationToken);
            instance.Plugin.Dispose();
            return AhirResult<bool>.Ok(true);
        }
        return AhirResult<bool>.Fail("NOT_FOUND", $"Plugin '{pluginId}' not loaded.");
    }

    public async Task<AhirResult<bool>> StartAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_plugins.TryGetValue(pluginId, out var instance))
        {
            await instance.Plugin.OnStartAsync(cancellationToken);
            return AhirResult<bool>.Ok(true);
        }
        return AhirResult<bool>.Fail("NOT_FOUND", $"Plugin '{pluginId}' not loaded.");
    }

    public async Task<AhirResult<bool>> StopAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_plugins.TryGetValue(pluginId, out var instance))
        {
            await instance.Plugin.OnStopAsync(cancellationToken);
            return AhirResult<bool>.Ok(true);
        }
        return AhirResult<bool>.Fail("NOT_FOUND", $"Plugin '{pluginId}' not loaded.");
    }

    public async Task<AhirResult<bool>> ReloadAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_plugins.TryGetValue(pluginId, out var instance))
        {
            var path = instance.Assembly.Location;
            await UnloadAsync(pluginId, cancellationToken);
            var loadResult = await LoadAsync(path, cancellationToken);
            return AhirResult<bool>.Ok(loadResult.Success);
        }
        return AhirResult<bool>.Fail("NOT_FOUND", $"Plugin '{pluginId}' not loaded.");
    }

    public IReadOnlyList<PluginInfo> GetLoadedPlugins()
    {
        return _plugins.Values.Select(p => new PluginInfo
        {
            Id = p.Plugin.Id,
            Name = p.Plugin.Name,
            Version = p.Plugin.Version,
            Author = p.Plugin.Author,
            Description = p.Plugin.Description,
            State = p.Plugin.State
        }).ToList();
    }

    public PluginInfo? GetPlugin(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var instance))
        {
            return new PluginInfo
            {
                Id = instance.Plugin.Id,
                Name = instance.Plugin.Name,
                Version = instance.Plugin.Version,
                Author = instance.Plugin.Author,
                Description = instance.Plugin.Description,
                State = instance.Plugin.State
            };
        }
        return null;
    }
}

internal sealed class PluginInstance
{
    public AhirPlugin Plugin { get; init; } = null!;
    public Assembly Assembly { get; init; } = null!;
    public AssemblyLoadContext LoadContext { get; init; } = null!;
}
