using System.Diagnostics;

namespace ZeugnisApp.App.Services;

/// <summary>Opens a file with the OS default handler (normally Word for a <c>.docx</c>, plan §5.2).</summary>
public interface IFileLauncher
{
    void Open(string path);
}

public sealed class FileLauncher : IFileLauncher
{
    public void Open(string path)
        => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
