namespace ZeugnisApp.Application.Abstractions;

/// <summary>
/// One-file encrypted backup bundle: the (still-encrypted) data file + the template folder +
/// a checksum manifest, zipped (plan §Sprint 11). Restoring needs the recovery key on the target
/// machine, exactly like moving to a new laptop.
/// </summary>
public interface IBackupService
{
    /// <summary>Writes a <c>.zeugnisapp-backup</c> bundle to <paramref name="targetBundlePath"/>.</summary>
    Task CreateBundleAsync(string targetBundlePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the bundle's checksums and stages its contents. The caller must restart the app;
    /// the staged files are moved into place on the next launch.
    /// </summary>
    Task<BackupRestoreResult> StageRestoreAsync(string bundlePath, CancellationToken cancellationToken = default);
}

public sealed record BackupRestoreResult(bool Success, string? Error);

/// <summary>The SQLCipher key, wrapped so it can be injected without leaking into config.</summary>
public sealed record DatabaseKey(string Value);
