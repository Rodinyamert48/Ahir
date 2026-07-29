using Ahir.Core.Interfaces;
using Ahir.Core.Models;

namespace Ahir.Plugin;

public abstract class AhirPlugin : IDisposable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PluginState State { get; set; } = PluginState.Loaded;

    public IServerHost? Host { get; internal set; }

    public abstract Task OnLoadAsync(CancellationToken cancellationToken = default);
    public abstract Task OnStartAsync(CancellationToken cancellationToken = default);
    public abstract Task OnStopAsync(CancellationToken cancellationToken = default);
    public abstract Task OnUnloadAsync(CancellationToken cancellationToken = default);

    public virtual void Dispose() { }
}
