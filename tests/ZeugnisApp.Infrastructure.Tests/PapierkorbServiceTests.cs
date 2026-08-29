using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class PapierkorbServiceTests
{
    private static async Task<(SqliteTestDatabase db, KlassenService klassen, PapierkorbService trash)> NewAsync()
    {
        var db = new SqliteTestDatabase();
        await db.MigrateAsync();
        return (db, new KlassenService(db.ContextFactory, db.Clock), new PapierkorbService(db.ContextFactory, db.Clock));
    }

    [Fact]
    public async Task Deleting_a_class_writes_a_trash_entry_and_restoring_brings_it_and_its_children_back()
    {
        var (db, klassen, trash) = await NewAsync();
        await using var _ = db;

        var klasse = await klassen.SaveKlasseAsync(new Klasse { Name = "E1", Jahrgang = 1 });
        await klassen.SaveKindAsync(new Kind { KlasseId = klasse.Id, Vorname = "Max", Nachname = "M" });
        await klassen.SaveKindAsync(new Kind { KlasseId = klasse.Id, Vorname = "Mia", Nachname = "K" });

        await klassen.DeleteKlasseAsync(klasse.Id);

        var entry = Assert.Single(await trash.GetEntriesAsync());
        Assert.Equal(PapierkorbTypen.Klasse, entry.EntityType);
        Assert.Contains("E1", entry.Bezeichnung, StringComparison.Ordinal);
        Assert.Empty(await klassen.GetKlassenAsync());

        await trash.RestoreAsync(entry.AuditEntryId);

        var restored = Assert.Single(await klassen.GetKlassenAsync());
        Assert.Equal(2, (await klassen.GetKinderAsync(restored.Id)).Count);
        Assert.Empty(await trash.GetEntriesAsync());
    }

    [Fact]
    public async Task Restoring_a_child_also_restores_its_reports()
    {
        var (db, klassen, trash) = await NewAsync();
        await using var _ = db;

        var klasse = await klassen.SaveKlasseAsync(new Klasse { Name = "3a", Jahrgang = 3 });
        var kind = await klassen.SaveKindAsync(new Kind { KlasseId = klasse.Id, Vorname = "Max", Nachname = "M" });

        Guid zeugnisId;
        await using (var ctx = db.NewContext())
        {
            var formular = new Formular { Anzeigename = "F" };
            ctx.Formulare.Add(formular);
            var z = new Zeugnis { KindId = kind.Id, FormularId = formular.Id, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes };
            ctx.Zeugnisse.Add(z);
            await ctx.SaveChangesAsync();
            zeugnisId = z.Id;
        }

        await klassen.DeleteKindAsync(kind.Id);
        var entry = Assert.Single(await trash.GetEntriesAsync());
        await trash.RestoreAsync(entry.AuditEntryId);

        await using (var ctx = db.NewContext())
        {
            Assert.NotNull(await ctx.Zeugnisse.FirstOrDefaultAsync(z => z.Id == zeugnisId)); // not filtered out
        }
    }

    [Fact]
    public async Task PurgeExpired_hard_deletes_entries_older_than_the_retention_window()
    {
        var db = new SqliteTestDatabase();
        await db.MigrateAsync();
        await using var _ = db;

        var clock = new MutableClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var klassen = new KlassenService(db.ContextFactory, clock);
        var trash = new PapierkorbService(db.ContextFactory, clock);

        var alt = await klassen.SaveKlasseAsync(new Klasse { Name = "Alt", Jahrgang = 1 });
        var neu = await klassen.SaveKlasseAsync(new Klasse { Name = "Neu", Jahrgang = 1 });
        await klassen.DeleteKlasseAsync(alt.Id);

        clock.Advance(TimeSpan.FromDays(40));
        await klassen.DeleteKlasseAsync(neu.Id);

        var purged = await trash.PurgeExpiredAsync(retentionDays: 30);

        Assert.Equal(1, purged);
        var remaining = Assert.Single(await trash.GetEntriesAsync());
        Assert.Contains("Neu", remaining.Bezeichnung, StringComparison.Ordinal);

        await using var ctx = db.NewContext();
        Assert.Empty(await ctx.Klassen.IgnoreQueryFilters().Where(k => k.Id == alt.Id).ToListAsync()); // gone for good
    }

    private sealed class MutableClock(DateTime start) : IClock
    {
        private DateTime _now = start;
        public DateTime UtcNow => _now;
        public DateOnly Today => DateOnly.FromDateTime(_now);
        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
