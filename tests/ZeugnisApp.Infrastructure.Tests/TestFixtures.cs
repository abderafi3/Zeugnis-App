using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Tests;

internal sealed class FakeClock(DateTime? utcNow = null) : IClock
{
    public DateTime UtcNow { get; } = utcNow ?? new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}

/// <summary>
/// Spins up a real file-based SQLite database, applies the migrations, and cleans up.
/// File-based (not in-memory) so migrations run exactly as they will in production.
/// </summary>
internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"zeugnis-test-{Guid.NewGuid():N}.db");

    public IClock Clock { get; } = new FakeClock();

    public ZeugnisDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ZeugnisDbContext>()
            .UseSqlite($"Data Source={_path}")
            .Options;
        return new ZeugnisDbContext(options, Clock);
    }

    /// <summary>A minimal <see cref="IDbContextFactory{T}"/> over this database for testing services.</summary>
    public IDbContextFactory<ZeugnisDbContext> ContextFactory => new Factory(this);

    private sealed class Factory(SqliteTestDatabase owner) : IDbContextFactory<ZeugnisDbContext>
    {
        public ZeugnisDbContext CreateDbContext() => owner.NewContext();
    }

    public async Task MigrateAsync()
    {
        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using (var db = NewContext())
        {
            await db.Database.EnsureDeletedAsync();
        }

        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + "*"))
        {
            try { File.Delete(file); } catch (IOException) { /* best effort */ }
        }
    }
}
