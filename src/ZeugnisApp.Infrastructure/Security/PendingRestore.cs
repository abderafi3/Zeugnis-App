using Serilog;

namespace ZeugnisApp.Infrastructure.Security;

/// <summary>
/// Applies a restore that <see cref="BackupService.StageRestoreAsync"/> staged: run this before the
/// database is opened on startup (plan §Sprint 11). Moves the staged data file and templates into
/// place, then drops the local key file so the recovery-key flow unlocks the restored data.
/// </summary>
public static class PendingRestore
{
    public static bool ApplyIfPresent()
    {
        if (!File.Exists(ZeugnisAppPaths.RestoreMarkerFile) || !Directory.Exists(ZeugnisAppPaths.RestoreStagingDirectory))
        {
            return false;
        }

        try
        {
            var stagedDb = Path.Combine(ZeugnisAppPaths.RestoreStagingDirectory, "daten.zeugnisapp");
            if (File.Exists(stagedDb))
            {
                foreach (var side in new[] { "", "-wal", "-shm" })
                {
                    var live = ZeugnisAppPaths.DatabaseFile + side;
                    if (File.Exists(live))
                    {
                        File.Delete(live);
                    }
                }

                File.Copy(stagedDb, ZeugnisAppPaths.DatabaseFile, overwrite: true);
            }

            var stagedTemplates = Path.Combine(ZeugnisAppPaths.RestoreStagingDirectory, "Vorlagen");
            if (Directory.Exists(stagedTemplates))
            {
                foreach (var file in Directory.EnumerateFiles(stagedTemplates))
                {
                    File.Copy(file, Path.Combine(ZeugnisAppPaths.TemplatesDirectory, Path.GetFileName(file)), overwrite: true);
                }
            }

            // The restored data was encrypted with the source machine's key; force the recovery flow.
            if (File.Exists(ZeugnisAppPaths.KeyFile))
            {
                File.Delete(ZeugnisAppPaths.KeyFile);
            }

            Directory.Delete(ZeugnisAppPaths.RestoreStagingDirectory, recursive: true);
            File.Delete(ZeugnisAppPaths.RestoreMarkerFile);
            Log.Information("Sicherung wurde wiederhergestellt");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Wiederherstellung der Sicherung fehlgeschlagen");
            return false;
        }
    }
}
