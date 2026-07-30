using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Database;
using Ahir.Plugin;
using Ahir.Realtime;
using Ahir.Security;
using Ahir.Server;
using Ahir.Server.Services;
using Ahir.Storage.Engines;

var config = new AhirConfig();
var database = new DatabaseEngine(config.Database);
var storage = new StorageEngine(config.Storage);
var security = new SecurityProvider(config.Security);
var realtime = new RealtimeEngine();
var plugin = new PluginEngine("plugins", null!);
var host = new AhirServerHost(config, database, storage, security, realtime, plugin);

_ = host.StartAsync();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

var apiPort = config.Server.HttpPort;

Console.WriteLine($"Ahir Dashboard running on http://localhost:{config.Server.HttpPort + 1}");
Console.WriteLine($"Connecting to Ahir API at http://localhost:{apiPort}");

await app.RunAsync($"http://0.0.0.0:{config.Server.HttpPort + 1}");
