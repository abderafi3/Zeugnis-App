namespace ZeugnisApp.Application.Abstractions;

/// <summary>
/// Supplies the SQLCipher key for the data file (plan §2 / §Sprint 11). The key is generated once,
/// stored DPAPI-protected to the Windows account, and shown to the teacher once as a printable
/// recovery key — there is <em>no</em> other way back in if both are lost.
/// </summary>
public interface IKeyProvider
{
    /// <summary>
    /// Returns the key (an opaque string used as the connection <c>Password</c>), creating and
    /// storing a new one on first run. Throws <see cref="KeyRecoveryRequiredException"/> when the
    /// stored key is gone but a data file exists — the caller must ask for the recovery key.
    /// </summary>
    string GetOrCreateKey();

    /// <summary>True when <see cref="GetOrCreateKey"/> would throw <see cref="KeyRecoveryRequiredException"/>.</summary>
    bool NeedsRecovery { get; }

    /// <summary>True when a brand-new key was just generated (show it to the teacher once).</summary>
    bool WasJustCreated { get; }

    /// <summary>Stores a recovery key the teacher entered (DPAPI-protected) so future launches are silent.</summary>
    void ImportRecoveryKey(string recoveryKey);
}

/// <summary>The DPAPI-protected key is missing but the data file is present — a recovery key is required.</summary>
public sealed class KeyRecoveryRequiredException() : Exception("Der Datenschlüssel wurde nicht gefunden. Bitte den Wiederherstellungsschlüssel eingeben.");
