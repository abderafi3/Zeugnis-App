namespace ZeugnisApp.Infrastructure;

/// <summary>
/// Resolves the per-user, on-disk locations the app uses. Everything lives under
/// <c>%LOCALAPPDATA%\Zeugnis-App\</c> so the app needs no elevation and the data is
/// bound to the Windows user account (plan §2, §8).
/// </summary>
public static class ZeugnisAppPaths
{
    public const string ProductFolderName = "Zeugnis-App";

    /// <summary>Root data directory, created if missing.</summary>
    public static string DataDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ProductFolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>The single SQLite data file (plan §2 — "one file, no server").</summary>
    public static string DatabaseFile => Path.Combine(DataDirectory, "daten.zeugnisapp");

    /// <summary>DPAPI-protected SQLCipher key (plan §Sprint 11).</summary>
    public static string KeyFile => Path.Combine(DataDirectory, "daten.key");

    /// <summary>Where a restore stages the bundle's files until the next launch applies them.</summary>
    public static string RestoreStagingDirectory => Path.Combine(DataDirectory, "restore-pending");

    /// <summary>Marker file whose presence tells the next launch to apply a staged restore.</summary>
    public static string RestoreMarkerFile => Path.Combine(DataDirectory, "restore.pending");

    /// <summary>Folder holding the official <c>.docx</c> templates (plan §3 data layer).</summary>
    public static string TemplatesDirectory
    {
        get
        {
            var dir = Path.Combine(DataDirectory, "Vorlagen");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Rolling Serilog log files (plan §2 — local only, no telemetry).</summary>
    public static string LogsDirectory
    {
        get
        {
            var dir = Path.Combine(DataDirectory, "logs");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>
    /// SQLite connection string for the data file. When <paramref name="key"/> is given, it is
    /// carried as <c>Password</c> so the SQLCipher provider issues <c>PRAGMA key</c> on open.
    /// </summary>
    public static string BuildConnectionString(string? databaseFile = null, string? key = null)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = databaseFile ?? DatabaseFile,
        };

        if (!string.IsNullOrEmpty(key))
        {
            builder.Password = key;
        }

        return builder.ToString();
    }
}
