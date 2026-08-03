using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace ClimateHub.Backup;

internal static class Program
{
    private static readonly string BackupDir = Environment.GetEnvironmentVariable("CLIMATE_HUB_BACKUP_DIR")
        ?? Path.Combine(AppContext.BaseDirectory, "backups");
    private static readonly string PgConnectionString = Environment.GetEnvironmentVariable("CLIMATE_HUB_PG_CONNECTION_STRING")
        ?? throw new InvalidOperationException("CLIMATE_HUB_PG_CONNECTION_STRING is required");
    private static readonly string PgDumpPath = Environment.GetEnvironmentVariable("CLIMATE_HUB_PG_DUMP_PATH") ?? "pg_dump";
    private static readonly int RetentionCount = int.TryParse(Environment.GetEnvironmentVariable("CLIMATE_HUB_BACKUP_RETENTION"), out var r) ? r : 10;
    private static readonly bool DryRun = string.Equals(Environment.GetEnvironmentVariable("CLIMATE_HUB_BACKUP_DRY_RUN"), "true", StringComparison.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: ClimateHub.Backup <backup|restore|list> [args]");
                return 1;
            }

            var command = args[0].ToLowerInvariant();
            return command switch
            {
                "backup" => await ExecuteBackup(),
                "restore" => await ExecuteRestore(args.ElementAtOrDefault(1)),
                "list" => ExecuteList(),
                _ => throw new InvalidOperationException($"Unknown command: {command}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Operation failed: {Message}", ex.Message);
            return 1;
        }
    }

    private static async Task<int> ExecuteBackup()
    {
        Directory.CreateDirectory(BackupDir);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupFile = Path.Combine(BackupDir, $"climate-hub-{timestamp}.dump");
        var metadataFile = backupFile + ".json";

        var psi = new ProcessStartInfo
        {
            FileName = PgDumpPath,
            ArgumentList =
            {
                "--format=custom",
                $"--file={backupFile}",
                $"--dbname={PgConnectionString}"
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (DryRun)
        {
            Console.WriteLine($"[DRY-RUN] Would execute: pg_dump --format=custom --file={backupFile} --dbname=***");
            Console.WriteLine($"[DRY-RUN] Would create metadata: {metadataFile}");
            return 0;
        }

        using var process = Process.Start(psi);
        if (process == null)
        {
            Console.Error.WriteLine("Failed to start pg_dump process");
            return 1;
        }

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine("pg_dump failed (exit code {ExitCode}): {Error}", process.ExitCode, stderr);
            return 1;
        }

        if (!File.Exists(backupFile))
        {
            Console.Error.WriteLine("Backup file was not created");
            return 1;
        }

        var checksum = await ComputeSha256Async(backupFile);
        var fileInfo = new FileInfo(backupFile);
        var schemaVersion = DetectSchemaVersion();

        var metadata = new BackupMetadata
        {
            FileName = Path.GetFileName(backupFile),
            CreatedAt = DateTimeOffset.UtcNow,
            SizeBytes = fileInfo.Length,
            Sha256 = checksum,
            SchemaVersion = schemaVersion
        };

        await File.WriteAllTextAsync(metadataFile, JsonSerializer.Serialize(metadata, JsonOptions));
        Console.WriteLine("Backup completed: {BackupFile} (SHA256: {Checksum})", backupFile, checksum);

        ApplyRetention();
        return 0;
    }

    private static async Task<int> ExecuteRestore(string? backupFileName)
    {
        if (string.IsNullOrWhiteSpace(backupFileName))
        {
            Console.Error.WriteLine("Usage: ClimateHub.Backup restore <backup-file>");
            return 1;
        }

        var backupFile = Path.IsPathRooted(backupFileName)
            ? backupFileName
            : Path.Combine(BackupDir, backupFileName);

        if (!File.Exists(backupFile))
        {
            Console.Error.WriteLine("Backup file not found: {BackupFile}", backupFile);
            return 1;
        }

        var metadataFile = backupFile + ".json";
        if (File.Exists(metadataFile))
        {
            var metadata = JsonSerializer.Deserialize<BackupMetadata>(await File.ReadAllTextAsync(metadataFile));
            if (metadata != null)
            {
                var actualChecksum = await ComputeSha256Async(backupFile);
                if (!string.Equals(actualChecksum, metadata.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("Checksum mismatch: expected {Expected}, actual {Actual}", metadata.Sha256, actualChecksum);
                    return 1;
                }
                Console.WriteLine("Backup integrity verified (SHA256: {Checksum})", actualChecksum);
            }
        }

        if (DryRun)
        {
            Console.WriteLine("[DRY-RUN] Would restore: pg_restore --dbname=*** {BackupFile}", backupFile);
            return 0;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "pg_restore",
            ArgumentList =
            {
                "--clean",
                "--if-exists",
                $"--dbname={PgConnectionString}",
                backupFile
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            Console.Error.WriteLine("Failed to start pg_restore process");
            return 1;
        }

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine("Restore failed (exit code {ExitCode}): {Error}", process.ExitCode, stderr);
            return 1;
        }

        Console.WriteLine("Restore completed successfully from {BackupFile}", backupFile);
        return 0;
    }

    private static int ExecuteList()
    {
        if (!Directory.Exists(BackupDir))
        {
            Console.WriteLine("No backups directory found at {BackupDir}", BackupDir);
            return 0;
        }

        var backupFiles = Directory.GetFiles(BackupDir, "*.dump")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        if (backupFiles.Count == 0)
        {
            Console.WriteLine("No backups found in {BackupDir}", BackupDir);
            return 0;
        }

        Console.WriteLine("{0,-30} {1,12} {2,-20}", "FILE", "SIZE", "CREATED");
        Console.WriteLine(new string('-', 64));

        foreach (var file in backupFiles)
        {
            var metaFile = file.FullName + ".json";
            string checksum = "";
            if (File.Exists(metaFile))
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<BackupMetadata>(File.ReadAllText(metaFile));
                    if (meta != null) checksum = meta.Sha256[..16] + "...";
                }
                catch { }
            }

            Console.WriteLine("{0,-30} {1,12:N0} {2,-20} {3}",
                file.Name,
                file.Length,
                file.CreationTimeUtc.ToString("yyyy-MM-dd HH:mm"),
                checksum);
        }

        return 0;
    }

    private static async Task<string> ComputeSha256Async(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexStringLower(hash);
    }

    private static string DetectSchemaVersion()
    {
        return typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
    }

    private static void ApplyRetention()
    {
        var files = Directory.GetFiles(BackupDir, "*.dump")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        if (files.Count <= RetentionCount) return;

        foreach (var file in files.Skip(RetentionCount))
        {
            try
            {
                file.Delete();
                var meta = file.FullName + ".json";
                if (File.Exists(meta)) File.Delete(meta);
                Console.WriteLine("Removed old backup: {FileName}", file.Name);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to remove old backup {FileName}: {Message}", file.Name, ex.Message);
            }
        }
    }

    private sealed class BackupMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = string.Empty;
        public string SchemaVersion { get; set; } = string.Empty;
    }
}