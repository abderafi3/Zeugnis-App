// -----------------------------------------------------------------------------
//  Spike B — plan §Sprint 0
//  Goal: prove the encrypted-database stack the plan mandates for Sprint 11:
//    Microsoft.EntityFrameworkCore.Sqlite.Core
//  + Microsoft.Data.Sqlite.Core
//  + SQLitePCLRaw.bundle_e_sqlcipher          (SQLCipher native library)
//  + PRAGMA key on the open connection
//  + DPAPI-wrapped key material (CurrentUser scope)
//
//  Checks:
//   1. A DB created with a key is NOT a readable plain SQLite file on disk.
//   2. Opening it with the wrong key fails.
//   3. Opening it with the right key round-trips data — including through EF Core.
//   4. The key can be DPAPI-protected to the Windows account and recovered.
// -----------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

SQLitePCL.Batteries_V2.Init();

var work = Path.Combine(Path.GetTempPath(), "zeugnis-spike-sqlcipher");
if (Directory.Exists(work))
{
    Directory.Delete(work, recursive: true);
}

Directory.CreateDirectory(work);
var dbPath = Path.Combine(work, "daten.zeugnisapp");
const string passphrase = "Correct-Horse-Battery-Staple-2026";

var failures = new List<string>();

// --- 1. create + write via a keyed connection -------------------------------
using (var conn = OpenKeyed(dbPath, passphrase))
{
    Exec(conn, "CREATE TABLE Probe (Id INTEGER PRIMARY KEY, Wert TEXT NOT NULL);");
    Exec(conn, "INSERT INTO Probe (Wert) VALUES ('Kind: Erika Mustermann');");
}

// --- 2. the file on disk must not look like a plain SQLite database ---------
var header = new byte[16];
using (var fs = File.OpenRead(dbPath))
{
    _ = fs.Read(header, 0, header.Length);
}

var headerText = Encoding.ASCII.GetString(header);
Console.WriteLine($"On-disk header bytes: {BitConverter.ToString(header)}");
if (headerText.StartsWith("SQLite format 3", StringComparison.Ordinal))
{
    failures.Add("Database file is NOT encrypted — plain SQLite header present.");
}
else
{
    Console.WriteLine("  [OK]   file header is not 'SQLite format 3' → encrypted at rest");
}

// --- 3a. wrong key must fail ----------------------------------------------------
try
{
    using var wrong = OpenKeyed(dbPath, "this-is-the-wrong-key");
    using var cmd = wrong.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM Probe;";
    _ = cmd.ExecuteScalar();
    failures.Add("Wrong key was accepted — encryption not enforced.");
}
catch (SqliteException)
{
    Console.WriteLine("  [OK]   wrong key rejected (SqliteException 'file is not a database')");
}

// --- 3b. right key round-trips, and EF Core works over the keyed connection ---
using (var conn = OpenKeyed(dbPath, passphrase))
{
    var options = new DbContextOptionsBuilder<ProbeContext>()
        .UseSqlite(conn)
        .Options;

    using var db = new ProbeContext(options);

    // The file already exists (it holds the 'Probe' table), so EnsureCreated() would
    // no-op. Force the EF-managed table to be created over the keyed connection —
    // this is the part that proves the provider stack works end to end.
    db.Database.GetService<IRelationalDatabaseCreator>().CreateTables();

    db.Proben.Add(new Probe { Wert = "Über EF Core geschrieben" });
    db.SaveChanges();

    var viaEf = db.Proben.OrderBy(p => p.Id).Select(p => p.Wert).ToList();
    var viaAdo = new List<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT Wert FROM Probe ORDER BY Id;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            viaAdo.Add(reader.GetString(0));
        }
    }

    Console.WriteLine($"  [OK]   EF Core rows:  {string.Join(" | ", viaEf)}");
    Console.WriteLine($"  [OK]   ADO.NET rows:  {string.Join(" | ", viaAdo)}");
    if (viaEf.Count == 0 || viaAdo.Count == 0)
    {
        failures.Add("Keyed round-trip returned no rows.");
    }
}

// --- 4. DPAPI-wrap the key material -----------------------------------------------
var keyBytes = Encoding.UTF8.GetBytes(passphrase);
var protectedBlob = ProtectedData.Protect(keyBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
var keyFile = Path.Combine(work, "key.dpapi");
File.WriteAllBytes(keyFile, protectedBlob);

var recovered = Encoding.UTF8.GetString(
    ProtectedData.Unprotect(File.ReadAllBytes(keyFile), optionalEntropy: null, DataProtectionScope.CurrentUser));

if (recovered == passphrase)
{
    Console.WriteLine("  [OK]   key DPAPI-protected (CurrentUser) and recovered — no prompt on the owner's machine");
}
else
{
    failures.Add("DPAPI round-trip did not recover the original key.");
}

// --- verdict --------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("==== Spike B result ====");
if (failures.Count == 0)
{
    Console.WriteLine("PASS — the *.Core + bundle_e_sqlcipher + PRAGMA key + DPAPI stack works.");
    Console.WriteLine("Sprint 11 can adopt it by swapping the two package references and adding a");
    Console.WriteLine("DbConnection interceptor that issues PRAGMA key (plan §2 integration caveat).");
    Environment.Exit(0);
}

Console.WriteLine("FAIL:");
failures.ForEach(f => Console.WriteLine("  - " + f));
Environment.Exit(1);

// -----------------------------------------------------------------------------

static SqliteConnection OpenKeyed(string path, string passphrase)
{
    var conn = new SqliteConnection(new SqliteConnectionStringBuilder
    {
        DataSource = path,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Pooling = false,
    }.ToString());

    conn.Open();

    // Issue PRAGMA key as the very first statement — this is what the Sprint 11
    // connection interceptor will do. PRAGMA does not accept bound parameters,
    // so the passphrase is inlined as a SQL string literal with quotes escaped.
    using (var pragma = conn.CreateCommand())
    {
        var literal = "'" + passphrase.Replace("'", "''", StringComparison.Ordinal) + "'";
        pragma.CommandText = $"PRAGMA key = {literal};";
        pragma.ExecuteNonQuery();
    }

    return conn;
}

static void Exec(SqliteConnection conn, string sql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    cmd.ExecuteNonQuery();
}

internal sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options)
{
    public DbSet<Probe> Proben => Set<Probe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Probe>().ToTable("Probe_Ef");
}

internal sealed class Probe
{
    public int Id { get; set; }
    public string Wert { get; set; } = string.Empty;
}
