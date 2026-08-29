using System.Text;
using Microsoft.Data.Sqlite;
using ZeugnisApp.Infrastructure;

namespace ZeugnisApp.Infrastructure.Tests;

public sealed class EncryptionTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "zeugnis-enc", Guid.NewGuid().ToString("N"));

    public EncryptionTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    [Fact]
    public void A_keyed_database_is_not_readable_as_plain_sqlite_and_the_wrong_key_is_rejected()
    {
        var path = Path.Combine(_dir, "daten.zeugnisapp");
        const string key = "aGVsbG8td29ybGQtc2VjcmV0LWtleS0zMi1ieXRlcw==";

        using (var conn = new SqliteConnection(ZeugnisAppPaths.BuildConnectionString(path, key)))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE t (v TEXT); INSERT INTO t VALUES ('geheim');";
            cmd.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();

        var header = new byte[16];
        using (var fs = File.OpenRead(path))
        {
            _ = fs.Read(header, 0, header.Length);
        }

        Assert.DoesNotContain("SQLite format 3", Encoding.ASCII.GetString(header), StringComparison.Ordinal);

        Assert.Throws<SqliteException>(() =>
        {
            using var wrong = new SqliteConnection(
                ZeugnisAppPaths.BuildConnectionString(path, "d3Jvbmcta2V5LWNvbXBsZXRlbHktZGlmZmVyZW50LW9uZQ=="));
            wrong.Open();
            using var check = wrong.CreateCommand();
            check.CommandText = "SELECT count(*) FROM t;";
            check.ExecuteScalar();
        });

        SqliteConnection.ClearAllPools();
    }
}
