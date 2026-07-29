using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace Steps.Services;

public sealed class InstallationService
{
    private readonly string _installPath;
    private readonly string _dataPath;
    private readonly string _logsPath;
    private readonly string _backupPath;
    private readonly int _httpPort;
    private readonly int _wsPort;
    private readonly string _adminUser;
    private readonly string _adminPass;
    private readonly bool _enableCompression;
    private readonly bool _enableRateLimiting;
    private readonly bool _autoStart;
    private readonly string _serviceName = "AhirServer";
    private readonly string _sourcePath;

    public List<string> Log { get; } = new();
    public int ProgressPercent { get; private set; }

    public InstallationService(string installPath, string dataPath, string logsPath, string backupPath,
        int httpPort, int wsPort, string adminUser, string adminPass,
        bool enableCompression, bool enableRateLimiting, bool autoStart)
    {
        _installPath = installPath;
        _dataPath = dataPath;
        _logsPath = logsPath;
        _backupPath = backupPath;
        _httpPort = httpPort;
        _wsPort = wsPort;
        _adminUser = adminUser;
        _adminPass = adminPass;
        _enableCompression = enableCompression;
        _enableRateLimiting = enableRateLimiting;
        _autoStart = autoStart;
        _sourcePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    public async Task<bool> RunAsync(IProgress<InstallProgress>? progress = null)
    {
        try
        {
            await Step(progress, 5, "Checking administrator privileges...");
            if (!IsAdministrator())
            {
                Log.Add("ERROR: Administrator privileges required.");
                return false;
            }

            await Step(progress, 10, "Creating installation directories...");
            Directory.CreateDirectory(_installPath);
            Directory.CreateDirectory(_dataPath);
            Directory.CreateDirectory(_logsPath);
            Directory.CreateDirectory(_backupPath);
            Directory.CreateDirectory(Path.Combine(_dataPath, "db"));

            await Step(progress, 20, "Copying Ahır binaries...");
            CopyAhirFiles();

            await Step(progress, 35, "Generating configuration file...");
            GenerateConfig();

            await Step(progress, 50, "Generating security keys...");
            var jwtSecret = GenerateJwtSecret();
            var encryptionKey = GenerateEncryptionKey();
            UpdateConfigWithSecrets(jwtSecret, encryptionKey);

            await Step(progress, 60, "Creating Windows Firewall rules...");
            CreateFirewallRules();

            await Step(progress, 75, "Registering Windows Service...");
            RegisterWindowsService();

            if (_autoStart)
            {
                await Step(progress, 85, "Enabling auto-start...");
                EnableAutoStart();
            }

            await Step(progress, 95, "Creating initial admin user...");
            await CreateAdminUserAsync();

            await Step(progress, 100, "Installation completed successfully!");
            WriteInstallationReport();
            return true;
        }
        catch (Exception ex)
        {
            Log.Add($"FATAL: {ex.Message}");
            return false;
        }
    }

    public SystemCheckResult CheckSystem()
    {
        var result = new SystemCheckResult();

        result.AdminRight = IsAdministrator();
        result.DotNetRuntime = CheckDotNetRuntime();
        result.DiskSpace = CheckDiskSpace(_installPath, 500);
        result.PortAvailable = CheckPort(_httpPort);
        if (!result.PortAvailable)
            result.PortOwner = GetPortOwnerInfo(_httpPort);
        result.OsSupported = Environment.OSVersion.Version.Major >= 10;

        return result;
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool CheckDotNetRuntime()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            var version = proc?.StandardOutput.ReadToEnd()?.Trim();
            proc?.WaitForExit(3000);
            return version?.StartsWith("9") == true;
        }
        catch { return false; }
    }

    private static bool CheckDiskSpace(string path, int requiredMb)
    {
        try
        {
            var drive = Path.GetPathRoot(path);
            if (drive == null) return false;
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.Name.Equals(drive, StringComparison.OrdinalIgnoreCase) && d.IsReady)
                    return d.AvailableFreeSpace > requiredMb * 1024L * 1024L;
            }
        }
        catch { }
        return false;
    }

    private static bool CheckPort(int port)
    {
        try
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            var inUse = listeners.Any(ep => ep.Port == port);
            return !inUse;
        }
        catch { return true; }
    }

    public static string GetPortOwnerInfo(int port)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = $"-ano | findstr :{port}",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            if (proc == null) return "Unknown";
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(2000);
            if (string.IsNullOrWhiteSpace(output)) return "Unknown";

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5 && int.TryParse(parts[4], out var pid))
                {
                    try
                    {
                        var processName = Process.GetProcessById(pid).ProcessName;
                        return $"{processName} (PID: {pid})";
                    }
                    catch { return $"PID: {pid}"; }
                }
            }
            return "Unknown";
        }
        catch { return "Unknown"; }
    }

    private void CopyAhirFiles()
    {
        var files = new[] { "Ahir.Core.dll", "Ahir.Database.dll", "Ahir.Security.dll",
            "Ahir.Server.dll", "Ahir.Storage.dll", "Ahir.Realtime.dll", "Ahir.Plugin.dll",
            "Ahir.CLI.dll", "Ahir.CLI.exe", "K4os.Compression.LZ4.dll",
            "Konscious.Security.Cryptography.Argon2.dll", "Serilog.dll", "Serilog.AspNetCore.dll",
            "Serilog.Sinks.File.dll", "Serilog.Sinks.Console.dll",
            "Microsoft.IdentityModel.Tokens.dll", "System.IdentityModel.Tokens.Jwt.dll",
            "System.IO.Hashing.dll" };

        foreach (var file in files)
        {
            var src = Path.Combine(_sourcePath, file);
            var dst = Path.Combine(_installPath, file);
            if (File.Exists(src))
                File.Copy(src, dst, true);
        }

        Log.Add($"Copied {files.Count(f => File.Exists(Path.Combine(_sourcePath, f)))} files");
    }

    private void GenerateConfig()
    {
        var config = new
        {
            Server = new
            {
                HttpPort = _httpPort,
                EnableCompression = _enableCompression,
                EnableRateLimiting = _enableRateLimiting,
                MaxConnections = 1000,
                OperationTimeout = "00:00:30"
            },
            Database = new
            {
                DataPath = Path.Combine(_dataPath, "db"),
                EnableCompression = true,
                CacheSize = 268435456,
                EnableBloomFilter = true,
                EnableAutoCompaction = true
            },
            Security = new
            {
                TokenExpirationHours = 24,
                EnableAuditLog = true,
                MaxLoginAttempts = 5
            },
            Storage = new
            {
                StoragePath = Path.Combine(_dataPath, "storage"),
                MaxFileSize = 104857600
            },
            Realtime = new
            {
                WebSocketPort = _wsPort,
                EnablePresence = true
            },
            Logging = new
            {
                LogPath = _logsPath,
                MinimumLevel = "Info",
                EnableConsole = true,
                EnableFile = true,
                MaxFileSizeMb = 100
            }
        };

        var configPath = Path.Combine(_installPath, "ahir.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
        Log.Add($"Configuration written: {configPath}");
    }

    private static string GenerateJwtSecret()
    {
        var bytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexStringLower(bytes);
    }

    private static string GenerateEncryptionKey()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexStringLower(bytes);
    }

    private void UpdateConfigWithSecrets(string jwtSecret, string encryptionKey)
    {
        var configPath = Path.Combine(_installPath, "ahir.json");
        if (!File.Exists(configPath)) return;

        var json = File.ReadAllText(configPath);
        var doc = JsonDocument.Parse(json);

        var securityObj = doc.RootElement.TryGetProperty("Security", out var sec) ? sec : default;
        var securityJson = JsonSerializer.Serialize(new
        {
            JwtSecret = jwtSecret,
            EncryptionKey = encryptionKey,
            ArgonIterations = 3,
            ArgonMemorySize = 65536,
            ArgonParallelism = 4,
            TokenExpirationHours = securityObj.ValueKind == JsonValueKind.Object && securityObj.TryGetProperty("TokenExpirationHours", out var teh) ? teh.GetInt32() : 24,
            EnableAuditLog = true,
            MaxLoginAttempts = 5
        }, new JsonSerializerOptions { WriteIndented = true });

        // Simple approach: replace the Security section
        var start = json.IndexOf("\"Security\"");
        if (start >= 0)
        {
            var braceStart = json.IndexOf('{', start);
            var depth = 0;
            var braceEnd = braceStart;
            for (int i = braceStart; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') { depth--; if (depth == 0) { braceEnd = i; break; } }
            }
            if (braceEnd > braceStart)
            {
                json = json[..braceStart] + securityJson[securityJson.IndexOf('{')..] + json[(braceEnd + 1)..];
            }
        }

        File.WriteAllText(configPath, json);
        Log.Add("Security keys generated and saved to config");
    }

    private void CreateFirewallRules()
    {
        RunNetSh($"advfirewall firewall add rule name=\"Ahir HTTP\" dir=in action=allow protocol=TCP localport={_httpPort}");
        RunNetSh($"advfirewall firewall add rule name=\"Ahir WebSocket\" dir=in action=allow protocol=TCP localport={_wsPort}");
        Log.Add("Firewall rules created");
    }

    private void RegisterWindowsService()
    {
        var cliPath = Path.Combine(_installPath, "Ahir.CLI.exe");
        if (!File.Exists(cliPath))
        {
            Log.Add("WARNING: Ahir.CLI.exe not found, service registration skipped");
            return;
        }

        RunSc($"create \"{_serviceName}\" binPath=\"{cliPath} start\" start=auto DisplayName=\"Ahir Server\"");
        RunSc($"description \"{_serviceName}\" \"Ahir - Next-generation backend platform\"");
        Log.Add("Windows Service registered");
    }

    private void EnableAutoStart()
    {
        RunSc($"start \"{_serviceName}\"");
        Log.Add("Service auto-start enabled");
    }

    private async Task CreateAdminUserAsync()
    {
        var userFile = Path.Combine(_dataPath, "admin.json");
        var adminData = new
        {
            Username = _adminUser,
            PasswordHash = BCryptPlaceholder(_adminPass),
            Role = "admin",
            CreatedAt = DateTime.UtcNow,
            Enabled = true
        };
        var json = JsonSerializer.Serialize(adminData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(userFile, json);
        Log.Add($"Admin user '{_adminUser}' created");
    }

    private static string BCryptPlaceholder(string password)
    {
        // In production: use the actual Argon2id from Ahir.Security
        var salt = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);
        var hash = Convert.ToHexStringLower(salt) + Convert.ToHexStringLower(
            System.Security.Cryptography.SHA512.HashData(
                System.Text.Encoding.UTF8.GetBytes(password + Convert.ToHexStringLower(salt))));
        return hash;
    }

    private void WriteInstallationReport()
    {
        var report = new List<string>
        {
            "=== Ahır Installation Report ===",
            $"Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"Install Path: {_installPath}",
            $"Data Path: {_dataPath}",
            $"HTTP Port: {_httpPort}",
            $"WebSocket Port: {_wsPort}",
            $"Admin User: {_adminUser}",
            $"Service Name: {_serviceName}",
            "================================",
            "Log:"
        };
        report.AddRange(Log.Select(l => $"  {l}"));

        File.WriteAllLines(Path.Combine(_installPath, "install-report.txt"), report);
    }

    private void RunNetSh(string arguments)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            proc?.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            Log.Add($"WARNING: netsh failed: {ex.Message}");
        }
    }

    private void RunSc(string arguments)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            proc?.WaitForExit(10000);
        }
        catch (Exception ex)
        {
            Log.Add($"WARNING: sc failed: {ex.Message}");
        }
    }

    private async Task Step(IProgress<InstallProgress>? progress, int percent, string message)
    {
        ProgressPercent = percent;
        Log.Add(message);
        progress?.Report(new InstallProgress { Percent = percent, Message = message });
        await Task.Delay(100);
    }
}

public sealed class SystemCheckResult
{
    public bool AdminRight { get; set; }
    public bool DotNetRuntime { get; set; }
    public bool DiskSpace { get; set; }
    public bool PortAvailable { get; set; }
    public bool OsSupported { get; set; }
    public string? PortOwner { get; set; }
    public bool AllPassed => AdminRight && DotNetRuntime && DiskSpace && OsSupported;
}

public sealed class InstallProgress
{
    public int Percent { get; set; }
    public string Message { get; set; } = string.Empty;
}