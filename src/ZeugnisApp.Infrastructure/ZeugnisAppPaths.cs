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

    /// <summary>SQLite connection string for the data file.</summary>
    public static string BuildConnectionString(string? databaseFile = null)
        => $"Data Source={databaseFile ?? DatabaseFile}";
}
