using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time to build a context for scaffolding migrations.
/// Points at a throwaway local file; never touched at runtime.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ZeugnisDbContext>
{
    public ZeugnisDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ZeugnisDbContext>()
            .UseSqlite("Data Source=design-time.zeugnisapp")
            .Options;

        return new ZeugnisDbContext(options, new DesignTimeClock());
    }

    private sealed class DesignTimeClock : IClock
    {
        public DateTime UtcNow => new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 1, 1);
    }
}
