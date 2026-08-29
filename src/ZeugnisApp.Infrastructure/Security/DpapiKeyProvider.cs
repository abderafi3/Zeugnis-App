using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.Infrastructure.Security;

/// <summary>
/// Stores the SQLCipher key as a DPAPI blob (CurrentUser scope) next to the data file, so the
/// teacher is never prompted on their own machine (plan §2). The key doubles as the printable
/// recovery key (Base64 of 32 random bytes).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiKeyProvider(string keyFilePath, string databaseFilePath) : IKeyProvider
{
    public bool WasJustCreated { get; private set; }

    public bool NeedsRecovery => !File.Exists(keyFilePath) && File.Exists(databaseFilePath) && new FileInfo(databaseFilePath).Length > 0;

    public string GetOrCreateKey()
    {
        if (File.Exists(keyFilePath))
        {
            var blob = File.ReadAllBytes(keyFilePath);
            var key = ProtectedData.Unprotect(blob, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.ASCII.GetString(key);
        }

        if (NeedsRecovery)
        {
            throw new KeyRecoveryRequiredException();
        }

        var recoveryKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Store(recoveryKey);
        WasJustCreated = true;
        return recoveryKey;
    }

    public void ImportRecoveryKey(string recoveryKey)
        => Store(recoveryKey.Trim());

    private void Store(string recoveryKey)
    {
        var dir = Path.GetDirectoryName(keyFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var blob = ProtectedData.Protect(
            Encoding.ASCII.GetBytes(recoveryKey), optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyFilePath, blob);
    }
}
