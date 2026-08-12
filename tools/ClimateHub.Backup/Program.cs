using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClimateHub.Backup;

internal static class Program
{
    private static readonly string BackupDir = Environment.GetEnvironmentVariable("CLIMATE_HUB_BACKUP_DIR")
        ?? Path.Combine(AppContext.BaseDirectory, "backups");
    private static readonly string PgConnectionString = Environment.GetEnvironmentVariable("CLIMATE_HUB_PG_CONNECTION_STRING")
        ?? throw new InvalidOperationException("CLIMATE_HUB_PG_CONNECTION_STRING is required");
    private static readonly string PgDumpPath = Environment.GetEnvironmentVariable("CLIMATE_HUB_PG_DUMP_PATH") ?? "pg_dump";
    private static readonly string InfluxUrl = Environment.GetEnvironmentVariable("CLIMATE_HUB_INFLUX_URL") ?? "http://localhost:8086";
    private static readonly string InfluxToken = Environment.GetEnvironmentVariable("CLIMATE_HUB_INFLUX_TOKEN") ?? "";
    private static readonly string InfluxOrg = Environment.GetEnvironmentVariable("CLIMATE_HUB_INFLUX_ORG") ?? "climate-hub";
    private static readonly string MosquittoConfigDir = Environment.GetEnvironmentVariable("CLIMATE_HUB_MOSQUITTO_CONFIG_DIR") ?? "/mosquitto/config";
    private static readonly string MosquittoDataDir = Environment.GetEnvironmentVariable("CLIMATE_HUB_MOSQUITTO_DATA_DIR") ?? "/mosquitto/data";
    private static readonly string CertDir = Environment.GetEnvironmentVariable("CLIMATE_HUB_CERT_DIR") ?? "/certificates";
    private static readonly string FirmwareDir = Environment.GetEnvironmentVariable("CLIMATE_HUB_FIRMWARE_DIR") ?? "/firmware";
    private static readonly string ConfigDir = Environment.GetEnvironmentVariable("CLIMATE_HUB_CONFIG_DIR") ?? "/app/config";
    private static readonly int RetentionCount = int.TryParse(Environment.GetEnvironmentVariable("CLIMATE_HUB_BACKUP_RETENTION"), out var r) ? r : 10;
    private static readonly bool DryRun = string.Equals(Environment.GetEnvironmentVariable("CLIMATE_HUB_BACKUP_DRY_RUN"), "true", StringComparison.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: ClimateHub.Backup <backup|restore|list> [--include-influxdb] [--include-mosquitto] [--include-firmware] [--include-config]");
                return 1;
            }

            var command = args[0].ToLowerInvariant();
            var flags = new HashSet<string>(args.Skip(1), StringComparer.OrdinalIgnoreCase);
            bool includeInfluxDb = flags.Count == 0 || flags.Contains("--include-influxdb");
            bool includeMosquitto = flags.Count == 0 || flags.Contains("--include-mosquitto");
            bool includeFirmware = flags.Count == 0 || flags.Contains("--include-firmware");
            bool includeConfig = flags.Count == 0 || flags.Contains("--include-config");
            bool includePg = flags.Count == 0 || !flags.Contains("--skip-pg");

            return command switch
            {
                "backup" => await ExecuteBackup(includePg, includeInfluxDb, includeMosquitto, includeFirmware, includeConfig),
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

    private static async Task<int> ExecuteBackup(bool includePg, bool includeInfluxDb, bool includeMosquitto, bool includeFirmware, bool includeConfig)
    {
        Directory.CreateDirectory(BackupDir);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var snapshotDir = Path.Combine(BackupDir, $"climate-hub-{timestamp}");
        Directory.CreateDirectory(snapshotDir);
        var metadataFile = snapshotDir + ".json";

        var components = new List<string>();

        if (includeConfig)
        {
            if (await BackupConfig(snapshotDir))
                components.Add("config");
        }

        if (includeMosquitto)
        {
            if (await BackupMosquitto(snapshotDir))
                components.Add("mosquitto");
        }

        if (includePg)
        {
            if (await BackupPostgres(snapshotDir))
                components.Add("postgresql");
        }

        if (includeInfluxDb)
        {
            if (await BackupInfluxDb(snapshotDir))
                components.Add("influxdb");
        }

        if (includeFirmware)
        {
            if (await BackupFirmware(snapshotDir))
                components.Add("firmware");
        }

        if (components.Count == 0)
        {
            Console.Error.WriteLine("No backup components succeeded");
            return 1;
        }

        var checksum = await ComputeSha256DirAsync(snapshotDir);

        var metadata = new BackupMetadata
        {
            FileName = Path.GetFileName(snapshotDir),
            CreatedAt = DateTimeOffset.UtcNow,
            Components = components,
            Sha256 = checksum,
            SchemaVersion = DetectSchemaVersion()
        };

        await File.WriteAllTextAsync(metadataFile, JsonSerializer.Serialize(metadata, JsonOptions));
        Console.WriteLine("Backup completed: {SnapshotDir} (SHA256: {Checksum}) components: {Components}",
            snapshotDir, checksum, string.Join(", ", components));

        ApplyRetention();
        return 0;
    }

    private static async Task<bool> BackupPostgres(string snapshotDir)
    {
        var backupFile = Path.Combine(snapshotDir, "postgresql.dump");

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
            Console.WriteLine("[DRY-RUN] Would execute: pg_dump --format=custom --file={BackupFile} --dbname=***", backupFile);
            return true;
        }

        using var process = Process.Start(psi);
        if (process == null)
        {
            Console.Error.WriteLine("Failed to start pg_dump process");
            return false;
        }

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine("pg_dump failed (exit code {ExitCode}): {Error}", process.ExitCode, stderr);
            return false;
        }

        if (!File.Exists(backupFile))
        {
            Console.Error.WriteLine("PostgreSQL backup file was not created");
            return false;
        }

        Console.WriteLine("PostgreSQL backup completed: {BackupFile}", backupFile);
        return true;
    }

    private static async Task<bool> BackupInfluxDb(string snapshotDir)
    {
        var backupDir = Path.Combine(snapshotDir, "influxdb");
        Directory.CreateDirectory(backupDir);

        if (string.IsNullOrWhiteSpace(InfluxToken))
        {
            Console.Error.WriteLine("InfluxDB backup skipped: CLIMATE_HUB_INFLUX_TOKEN not set");
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "influx",
            ArgumentList =
            {
                "backup",
                $"--host={InfluxUrl}",
                $"--token={InfluxToken}",
                $"--org={InfluxOrg}",
                backupDir
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (DryRun)
        {
            Console.WriteLine("[DRY-RUN] Would execute: influx backup --host={Url} --token=*** --org={Org} {BackupDir}", InfluxUrl, InfluxOrg, backupDir);
            return true;
        }

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.Error.WriteLine("Failed to start influx backup process");
                return false;
            }

            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Console.Error.WriteLine("InfluxDB backup failed (exit code {ExitCode}): {Error}", process.ExitCode, stderr);
                return false;
            }

            Console.WriteLine("InfluxDB backup completed: {BackupDir}", backupDir);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("InfluxDB backup failed: {Message}", ex.Message);
            return false;
        }
    }

    private static async Task<bool> BackupMosquitto(string snapshotDir)
    {
        var mosquittoDir = Path.Combine(snapshotDir, "mosquitto");
        Directory.CreateDirectory(mosquittoDir);

        if (DryRun)
        {
            Console.WriteLine("[DRY-RUN] Would copy {ConfigDir} and {DataDir} to {MosquittoDir}", MosquittoConfigDir, MosquittoDataDir, mosquittoDir);
            return true;
        }

        var copied = false;

        if (Directory.Exists(MosquittoConfigDir))
        {
            await CopyDirectoryAsync(MosquittoConfigDir, Path.Combine(mosquittoDir, "config"));
            Console.WriteLine("Mosquitto config backed up from {ConfigDir}", MosquittoConfigDir);
            copied = true;
        }

        if (Directory.Exists(MosquittoDataDir))
        {
            await CopyDirectoryAsync(MosquittoDataDir, Path.Combine(mosquittoDir, "data"));
            Console.WriteLine("Mosquitto data backed up from {DataDir}", MosquittoDataDir);
            copied = true;
        }

        if (!copied)
        {
            Console.Error.WriteLine("Mosquitto backup: no directories found at {ConfigDir} or {DataDir}", MosquittoConfigDir, MosquittoDataDir);
            return false;
        }

        return true;
    }

    private static async Task<bool> BackupFirmware(string snapshotDir)
    {
        var firmwareDir = Path.Combine(snapshotDir, "firmware");

        if (DryRun)
        {
            Console.WriteLine("[DRY-RUN] Would copy {FirmwareDir} to {BackupDir}", FirmwareDir, firmwareDir);
            return true;
        }

        if (!Directory.Exists(FirmwareDir))
        {
            Console.Error.WriteLine("Firmware directory not found: {FirmwareDir}", FirmwareDir);
            return false;
        }

        await CopyDirectoryAsync(FirmwareDir, firmwareDir);
        Console.WriteLine("Firmware metadata backed up from {FirmwareDir}", FirmwareDir);
        return true;
    }

    private static async Task<bool> BackupConfig(string snapshotDir)
    {
        var configDir = Path.Combine(snapshotDir, "config");

        if (DryRun)
        {
            Console.WriteLine("[DRY-RUN] Would copy {ConfigDir} to {BackupDir}", ConfigDir, configDir);
            return true;
        }

        if (!Directory.Exists(ConfigDir))
        {
            Console.Error.WriteLine("Config directory not found: {ConfigDir}", ConfigDir);
            return false;
        }

        await CopyDirectoryAsync(ConfigDir, configDir);
        Console.WriteLine("Configuration files backed up from {ConfigDir}", ConfigDir);
        return true;
    }

    private static async Task<int> ExecuteRestore(string? backupFileName)
    {
        if (string.IsNullOrWhiteSpace(backupFileName))
        {
            Console.Error.WriteLine("Usage: ClimateHub.Backup restore <backup-name>");
            return 1;
        }

        var snapshotDir = Path.IsPathRooted(backupFileName)
            ? backupFileName
            : Path.Combine(BackupDir, backupFileName);

        if (!Directory.Exists(snapshotDir))
        {
            var withBackups = Path.Combine(BackupDir, backupFileName);
            if (Directory.Exists(withBackups))
                snapshotDir = withBackups;
            else
            {
                var metaFile = snapshotDir + ".json";
                if (File.Exists(metaFile))
                {
                }
                else
                {
                    Console.Error.WriteLine("Backup snapshot not found: {BackupName}", backupFileName);
                    return 1;
                }
            }
        }

        var metadataFile = snapshotDir + ".json";
        if (File.Exists(metadataFile))
        {
            var metadata = JsonSerializer.Deserialize<BackupMetadata>(await File.ReadAllTextAsync(metadataFile));
            if (metadata != null)
            {
                var actualChecksum = await ComputeSha256DirAsync(snapshotDir);
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
            Console.WriteLine("[DRY-RUN] Would restore from {SnapshotDir}", snapshotDir);
            return 0;
        }

        var restoreSteps = new List<(string name, Func<Task<bool>> restore)>();

        if (Directory.Exists(Path.Combine(snapshotDir, "config")))
            restoreSteps.Add(("config", () => RestoreConfig(snapshotDir)));

        if (Directory.Exists(Path.Combine(snapshotDir, "mosquitto")))
            restoreSteps.Add(("mosquitto", () => RestoreMosquitto(snapshotDir)));

        if (File.Exists(Path.Combine(snapshotDir, "postgresql.dump")))
            restoreSteps.Add(("postgresql", () => RestorePostgres(snapshotDir)));

        if (Directory.Exists(Path.Combine(snapshotDir, "influxdb")))
            restoreSteps.Add(("influxdb", () => RestoreInfluxDb(snapshotDir)));

        if (Directory.Exists(Path.Combine(snapshotDir, "firmware")))
            restoreSteps.Add(("firmware", () => RestoreFirmware(snapshotDir)));

        if (restoreSteps.Count == 0)
        {
            Console.Error.WriteLine("No restore data found in {SnapshotDir}", snapshotDir);
            return 1;
        }

        Console.WriteLine("Starting restore of {Count} component(s) in dependency order", restoreSteps.Count);

        foreach (var (name, restore) in restoreSteps)
        {
            Console.WriteLine("Restoring {Component}...", name);
            var success = await restore();
            if (!success)
            {
                Console.Error.WriteLine("Restore failed at component: {Component}", name);
                return 1;
            }
        }

        Console.WriteLine("Restore completed successfully from {SnapshotDir}", snapshotDir);
        return 0;
    }

    private static async Task<bool> RestorePostgres(string snapshotDir)
    {
        var backupFile = Path.Combine(snapshotDir, "postgresql.dump");
        if (!File.Exists(backupFile))
        {
            Console.Error.WriteLine("PostgreSQL backup file not found: {BackupFile}", backupFile);
            return false;
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
            return false;
        }

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine("PostgreSQL restore failed (exit code {ExitCode}): {Error}", process.ExitCode, stderr);
            return false;
        }

        Console.WriteLine("PostgreSQL restore completed");
        return true;
    }

    private static async Task<bool> RestoreInfluxDb(string snapshotDir)
    {
        var influxBackupDir = Path.Combine(snapshotDir, "influxdb");
        if (!Directory.Exists(influxBackupDir))
        {
            Console.Error.WriteLine("InfluxDB backup directory not found: {InfluxBackupDir}", influxBackupDir);
            return false;
        }

        if (string.IsNullOrWhiteSpace(InfluxToken))
        {
            Console.Error.WriteLine("InfluxDB restore skipped: CLIMATE_HUB_INFLUX_TOKEN not set");
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "influx",
            ArgumentList =
            {
                "restore",
                $"--host={InfluxUrl}",
                $"--token={InfluxToken}",
                $"--org={InfluxOrg}",
                influxBackupDir
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.Error.WriteLine("Failed to start influx restore process");
                return false;
            }

            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Console.Error.WriteLine("InfluxDB restore failed (exit code {ExitCode}): {Error}", process.ExitCode, stderr);
                return false;
            }

            Console.WriteLine("InfluxDB restore completed");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("InfluxDB restore failed: {Message}", ex.Message);
            return false;
        }
    }

    private static async Task<bool> RestoreMosquitto(string snapshotDir)
    {
        var mosquittoSource = Path.Combine(snapshotDir, "mosquitto");
        if (!Directory.Exists(mosquittoSource))
        {
            Console.Error.WriteLine("Mosquitto backup directory not found: {MosquittoSource}", mosquittoSource);
            return false;
        }

        var configSource = Path.Combine(mosquittoSource, "config");
        if (Directory.Exists(configSource))
        {
            await CopyDirectoryAsync(configSource, MosquittoConfigDir);
            Console.WriteLine("Mosquitto config restored to {ConfigDir}", MosquittoConfigDir);
        }

        var dataSource = Path.Combine(mosquittoSource, "data");
        if (Directory.Exists(dataSource))
        {
            await CopyDirectoryAsync(dataSource, MosquittoDataDir);
            Console.WriteLine("Mosquitto data restored to {DataDir}", MosquittoDataDir);
        }

        return true;
    }

    private static async Task<bool> RestoreFirmware(string snapshotDir)
    {
        var firmwareSource = Path.Combine(snapshotDir, "firmware");
        if (!Directory.Exists(firmwareSource))
        {
            Console.Error.WriteLine("Firmware backup directory not found: {FirmwareSource}", firmwareSource);
            return false;
        }

        await CopyDirectoryAsync(firmwareSource, FirmwareDir);
        Console.WriteLine("Firmware restored to {FirmwareDir}", FirmwareDir);
        return true;
    }

    private static async Task<bool> RestoreConfig(string snapshotDir)
    {
        var configSource = Path.Combine(snapshotDir, "config");
        if (!Directory.Exists(configSource))
        {
            Console.Error.WriteLine("Config backup directory not found: {ConfigSource}", configSource);
            return false;
        }

        await CopyDirectoryAsync(configSource, ConfigDir);
        Console.WriteLine("Configuration restored to {ConfigDir}", ConfigDir);
        return true;
    }

    private static int ExecuteList()
    {
        if (!Directory.Exists(BackupDir))
        {
            Console.WriteLine("No backups directory found at {BackupDir}", BackupDir);
            return 0;
        }

        var snapshots = Directory.GetDirectories(BackupDir, "climate-hub-*")
            .Select(d => new DirectoryInfo(d))
            .OrderByDescending(d => d.CreationTimeUtc)
            .ToList();

        if (snapshots.Count == 0)
        {
            Console.WriteLine("No backups found in {BackupDir}", BackupDir);
            return 0;
        }

        Console.WriteLine("{0,-35} {1,12} {2,-40}", "SNAPSHOT", "COMPONENTS", "CREATED");
        Console.WriteLine(new string('-', 90));

        foreach (var dir in snapshots)
        {
            var metaFile = dir.FullName + ".json";
            string components = "";
            if (File.Exists(metaFile))
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<BackupMetadata>(File.ReadAllText(metaFile));
                    if (meta?.Components != null)
                        components = string.Join(", ", meta.Components);
                }
                catch { }
            }

            Console.WriteLine("{0,-35} {1,12} {2,-40}",
                dir.Name,
                components,
                dir.CreationTimeUtc.ToString("yyyy-MM-dd HH:mm"));
        }

        return 0;
    }

    private static async Task<string> ComputeSha256Async(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexStringLower(hash);
    }

    private static async Task<string> ComputeSha256DirAsync(string dirPath)
    {
        var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).OrderBy(f => f).ToList();
        using var sha256 = SHA256.Create();
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(dirPath, file);
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);
            await using var stream = File.OpenRead(file);
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
        }
        sha256.TransformFinalBlock([], 0, 0);
        return Convert.ToHexStringLower(sha256.Hash!);
    }

    private static string DetectSchemaVersion()
    {
        return typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
    }

    private static void ApplyRetention()
    {
        var dirs = Directory.GetDirectories(BackupDir, "climate-hub-*")
            .Select(d => new DirectoryInfo(d))
            .OrderByDescending(d => d.CreationTimeUtc)
            .ToList();

        if (dirs.Count <= RetentionCount) return;

        foreach (var dir in dirs.Skip(RetentionCount))
        {
            try
            {
                dir.Delete(true);
                var meta = dir.FullName + ".json";
                if (File.Exists(meta)) File.Delete(meta);
                Console.WriteLine("Removed old backup: {DirName}", dir.Name);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to remove old backup {DirName}: {Message}", dir.Name, ex.Message);
            }
        }
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            await using var srcStream = File.OpenRead(file);
            await using var dstStream = File.Create(destFile);
            await srcStream.CopyToAsync(dstStream);
        }
    }

    private sealed class BackupMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = string.Empty;
        public string SchemaVersion { get; set; } = string.Empty;
        public List<string>? Components { get; set; }
    }
}
