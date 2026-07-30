using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Core.Utilities;
using Serilog;

namespace Ahir.Server.Services;

public sealed class BackupService : IBackupService
{
    private readonly AhirConfig _config;
    private readonly IDatabaseEngine _database;
    private readonly string _backupPath;
    private readonly ConcurrentDictionary<string, BackupInfo> _backups = new();

    public BackupService(AhirConfig config, IDatabaseEngine database)
    {
        _config = config;
        _database = database;
        _backupPath = Path.Combine(AppContext.BaseDirectory, Ahir.Core.Constants.AhirConstants.BackupDirectory);
        Directory.CreateDirectory(_backupPath);
        LoadBackupMetadata();
    }

    public async Task<AhirResult<BackupInfo>> CreateBackupAsync(string? databaseName = null, BackupType type = BackupType.Full, CancellationToken cancellationToken = default)
    {
        var backupId = IdGenerator.NewId();
        var timestamp = DateTime.UtcNow;
        var backupDir = Path.Combine(_backupPath, backupId);
        Directory.CreateDirectory(backupDir);

        try
        {
            var info = new BackupInfo
            {
                Id = backupId,
                Type = type,
                Status = BackupStatus.Running,
                StartedAt = timestamp,
                Encrypted = _config.Database.EnableEncryption
            };

            _backups[backupId] = info;

            if (databaseName != null)
            {
                var dbResult = await _database.GetInfoAsync(databaseName, cancellationToken);
                if (!dbResult.Success)
                    return AhirResult<BackupInfo>.Fail("NOT_FOUND", $"Database '{databaseName}' not found.");

                var dbPath = Path.Combine(_config.Database.DataPath, databaseName);
                if (Directory.Exists(dbPath))
                    await BackupDirectoryAsync(dbPath, backupDir, databaseName, cancellationToken);
            }
            else
            {
                var databases = await _database.ListAsync(cancellationToken);
                if (databases.Success && databases.Data != null)
                {
                    foreach (var db in databases.Data)
                    {
                        var dbPath = Path.Combine(_config.Database.DataPath, db.Name);
                        if (Directory.Exists(dbPath))
                            await BackupDirectoryAsync(dbPath, backupDir, db.Name, cancellationToken);
                    }
                }
            }

            var size = DirectorySize(backupDir);
            var completedInfo = CloneInfo(info, BackupStatus.Completed, DateTime.UtcNow, size, null);
            _backups[backupId] = completedInfo;

            var metaPath = Path.Combine(backupDir, "backup.json");
            var metaJson = JsonSerializer.SerializeToUtf8Bytes(completedInfo, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllBytesAsync(metaPath, metaJson, cancellationToken);

            var zipPath = Path.Combine(_backupPath, $"{backupId}.ahirbak");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(backupDir, zipPath);
            Directory.Delete(backupDir, true);

            var finalInfo = new BackupInfo
            {
                Id = completedInfo.Id,
                Path = zipPath,
                SizeBytes = new FileInfo(zipPath).Length,
                Type = completedInfo.Type,
                Status = completedInfo.Status,
                StartedAt = completedInfo.StartedAt,
                CompletedAt = completedInfo.CompletedAt,
                Checksum = completedInfo.Checksum,
                Encrypted = completedInfo.Encrypted
            };
            _backups[backupId] = finalInfo;

            Log.Information("Backup {BackupId} created at {Path}", backupId, zipPath);
            return AhirResult<BackupInfo>.Ok(finalInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Backup {BackupId} failed", backupId);

            if (_backups.TryGetValue(backupId, out var existing))
                _backups[backupId] = CloneInfo(existing, BackupStatus.Failed, existing.StartedAt, 0, null);
            else
                _backups[backupId] = new BackupInfo { Id = backupId, Status = BackupStatus.Failed };

            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, true);

            return AhirResult<BackupInfo>.Fail("BACKUP_FAILED", ex.Message);
        }
    }

    public async Task<AhirResult<bool>> RestoreAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var zipPath = Path.Combine(_backupPath, $"{backupId}.ahirbak");
        if (!File.Exists(zipPath))
            return AhirResult<bool>.Fail("NOT_FOUND", $"Backup '{backupId}' not found.");

        var restoreDir = Path.Combine(_backupPath, ".restore_" + backupId);
        try
        {
            if (Directory.Exists(restoreDir))
                Directory.Delete(restoreDir, true);
            Directory.CreateDirectory(restoreDir);

            ZipFile.ExtractToDirectory(zipPath, restoreDir);

            foreach (var dbDir in Directory.GetDirectories(restoreDir))
            {
                var dbName = Path.GetFileName(dbDir);
                var targetPath = Path.Combine(_config.Database.DataPath, dbName);

                if (Directory.Exists(targetPath))
                    Directory.Delete(targetPath, true);

                Directory.Move(dbDir, targetPath);
                Log.Information("Restored database {Database} from backup {BackupId}", dbName, backupId);
            }

            if (_backups.TryGetValue(backupId, out var existing))
                _backups[backupId] = CloneInfo(existing, BackupStatus.Restoring, existing.StartedAt, existing.SizeBytes, existing.Path);

            Log.Information("Restore from backup {BackupId} completed", backupId);
            return AhirResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Restore from backup {BackupId} failed", backupId);
            return AhirResult<bool>.Fail("RESTORE_FAILED", ex.Message);
        }
        finally
        {
            if (Directory.Exists(restoreDir))
                Directory.Delete(restoreDir, true);
        }
    }

    public Task<AhirResult<bool>> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var zipPath = Path.Combine(_backupPath, $"{backupId}.ahirbak");
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        _backups.TryRemove(backupId, out _);
        return Task.FromResult(AhirResult<bool>.Ok(true));
    }

    public Task<AhirResult<IReadOnlyList<BackupInfo>>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var list = _backups.Values.OrderByDescending(b => b.StartedAt).ToList() as IReadOnlyList<BackupInfo>;
        return Task.FromResult(AhirResult<IReadOnlyList<BackupInfo>>.Ok(list ?? Array.Empty<BackupInfo>()));
    }

    public Task<AhirResult<BackupInfo>> GetBackupInfoAsync(string backupId, CancellationToken cancellationToken = default)
    {
        if (_backups.TryGetValue(backupId, out var info))
            return Task.FromResult(AhirResult<BackupInfo>.Ok(info));

        return Task.FromResult(AhirResult<BackupInfo>.Fail("NOT_FOUND", $"Backup '{backupId}' not found."));
    }

    private void LoadBackupMetadata()
    {
        if (!Directory.Exists(_backupPath)) return;

        foreach (var file in Directory.GetFiles(_backupPath, "*.ahirbak"))
        {
            try
            {
                var id = Path.GetFileNameWithoutExtension(file);
                var tempDir = Path.Combine(_backupPath, ".meta_" + id);
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(file, tempDir);
                var metaPath = Path.Combine(tempDir, "backup.json");
                if (File.Exists(metaPath))
                {
                    var json = File.ReadAllBytes(metaPath);
                    var info = JsonSerializer.Deserialize<BackupInfo>(json);
                    if (info != null)
                        _backups[id] = new BackupInfo
                        {
                            Id = info.Id, Path = file, SizeBytes = info.SizeBytes,
                            Type = info.Type, Status = info.Status,
                            StartedAt = info.StartedAt, CompletedAt = info.CompletedAt,
                            Checksum = info.Checksum, Encrypted = info.Encrypted
                        };
                }
                Directory.Delete(tempDir, true);
            }
            catch { }
        }
    }

    private static async Task BackupDirectoryAsync(string sourceDir, string targetDir, string name, CancellationToken cancellationToken)
    {
        var dbTarget = Path.Combine(targetDir, name);
        Directory.CreateDirectory(dbTarget);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relPath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(dbTarget, relPath);
            var destDir = Path.GetDirectoryName(destFile);
            if (destDir != null && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            await using var srcStream = File.OpenRead(file);
            await using var dstStream = File.Create(destFile);
            await srcStream.CopyToAsync(dstStream, cancellationToken);
        }
    }

    private static long DirectorySize(string path) =>
        Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);

    private static BackupInfo CloneInfo(BackupInfo source, BackupStatus status, DateTime? completedAt, long size, string? path)
    {
        return new BackupInfo
        {
            Id = source.Id,
            Path = path ?? source.Path,
            SizeBytes = size > 0 ? size : source.SizeBytes,
            Type = source.Type,
            Status = status,
            StartedAt = source.StartedAt,
            CompletedAt = completedAt ?? source.CompletedAt,
            Checksum = source.Checksum,
            Encrypted = source.Encrypted
        };
    }
}
