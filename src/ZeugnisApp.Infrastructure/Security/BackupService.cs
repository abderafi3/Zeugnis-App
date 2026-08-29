using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Security;

internal sealed class BackupService(
    IDbContextFactory<ZeugnisDbContext> contextFactory, DatabaseKey key) : IBackupService
{
    private const string DbEntryName = "daten.zeugnisapp";
    private const string ManifestName = "manifest.json";
    private const string TemplatesPrefix = "Vorlagen/";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task CreateBundleAsync(string targetBundlePath, CancellationToken cancellationToken = default)
    {
        // Consistent, still-encrypted snapshot via the online-backup API (plan §2 — never a raw copy).
        await using (var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);", cancellationToken).ConfigureAwait(false);
        }

        var snapshot = Path.Combine(Path.GetTempPath(), $"zeugnis-backup-{Guid.NewGuid():N}.db");
        try
        {
            using (var source = OpenKeyed(ZeugnisAppPaths.DatabaseFile))
            using (var dest = OpenKeyed(snapshot))
            {
                source.BackupDatabase(dest);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(targetBundlePath))!);
            if (File.Exists(targetBundlePath))
            {
                File.Delete(targetBundlePath);
            }

            using var zip = ZipFile.Open(targetBundlePath, ZipArchiveMode.Create);
            var manifest = new Dictionary<string, string>(StringComparer.Ordinal);

            AddEntry(zip, DbEntryName, snapshot, manifest);
            foreach (var template in Directory.EnumerateFiles(ZeugnisAppPaths.TemplatesDirectory))
            {
                AddEntry(zip, TemplatesPrefix + Path.GetFileName(template), template, manifest);
            }

            var manifestJson = JsonSerializer.Serialize(new BundleManifest(1, DateTime.UtcNow, manifest), JsonOptions);
            var entry = zip.CreateEntry(ManifestName, CompressionLevel.Optimal);
            await using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            await writer.WriteAsync(manifestJson).ConfigureAwait(false);
        }
        finally
        {
            TryDelete(snapshot);
        }
    }

    public async Task<BackupRestoreResult> StageRestoreAsync(string bundlePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var zip = ZipFile.OpenRead(bundlePath);

            var manifestEntry = zip.GetEntry(ManifestName)
                ?? throw new InvalidOperationException("Die Sicherung enthält kein manifest.json.");
            BundleManifest manifest;
            await using (var ms = manifestEntry.Open())
            {
                manifest = await JsonSerializer.DeserializeAsync<BundleManifest>(ms, cancellationToken: cancellationToken).ConfigureAwait(false)
                           ?? throw new InvalidOperationException("Ungültiges manifest.json.");
            }

            foreach (var (name, expectedHash) in manifest.Sha256)
            {
                var e = zip.GetEntry(name) ?? throw new InvalidOperationException($"Datei fehlt in der Sicherung: {name}");
                await using var s = e.Open();
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(s, cancellationToken).ConfigureAwait(false));
                if (!string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    return new BackupRestoreResult(false, $"Prüfsumme stimmt nicht: {name}");
                }
            }

            var staging = ZeugnisAppPaths.RestoreStagingDirectory;
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }

            Directory.CreateDirectory(staging);
            Directory.CreateDirectory(Path.Combine(staging, "Vorlagen"));

            foreach (var e in zip.Entries.Where(x => x.Name.Length > 0))
            {
                var target = e.FullName == DbEntryName
                    ? Path.Combine(staging, DbEntryName)
                    : e.FullName.StartsWith(TemplatesPrefix, StringComparison.Ordinal)
                        ? Path.Combine(staging, "Vorlagen", e.Name)
                        : null;

                if (target is not null)
                {
                    e.ExtractToFile(target, overwrite: true);
                }
            }

            await File.WriteAllTextAsync(ZeugnisAppPaths.RestoreMarkerFile, DateTime.UtcNow.ToString("O"), cancellationToken).ConfigureAwait(false);
            return new BackupRestoreResult(true, null);
        }
        catch (Exception ex)
        {
            return new BackupRestoreResult(false, ex.Message);
        }
    }

    private SqliteConnection OpenKeyed(string path)
    {
        var conn = new SqliteConnection(ZeugnisAppPaths.BuildConnectionString(path, key.Value));
        conn.Open();
        return conn;
    }

    private static void AddEntry(ZipArchive zip, string entryName, string sourceFile, Dictionary<string, string> manifest)
    {
        zip.CreateEntryFromFile(sourceFile, entryName, CompressionLevel.Optimal);
        using var fs = File.OpenRead(sourceFile);
        manifest[entryName] = Convert.ToHexString(SHA256.HashData(fs));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // best effort
        }
    }

    private sealed record BundleManifest(int Version, DateTime CreatedUtc, Dictionary<string, string> Sha256);
}
