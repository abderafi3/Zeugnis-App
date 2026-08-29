using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class SchemaAndServiceTests
{
    [Fact]
    public async Task Migrations_apply_cleanly_and_all_tables_are_queryable()
    {
        await using var testDb = new SqliteTestDatabase();
        await testDb.MigrateAsync();

        await using var db = testDb.NewContext();

        Assert.Empty(await db.Klassen.ToListAsync());
        Assert.Empty(await db.Kinder.ToListAsync());
        Assert.Empty(await db.Floskeln.ToListAsync());
        Assert.Empty(await db.Formulare.ToListAsync());
        Assert.Empty(await db.Fehlzeiten.ToListAsync());
        Assert.Empty(await db.Zeugnisse.ToListAsync());
        Assert.Empty(await db.AuditTrail.ToListAsync());

        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(applied);
    }

    [Fact]
    public async Task SchoolSettingsService_creates_the_singleton_on_first_read()
    {
        await using var testDb = new SqliteTestDatabase();
        await testDb.MigrateAsync();

        await using (var db = testDb.NewContext())
        {
            var service = new SchoolSettingsService(db);
            var settings = await service.GetAsync();

            Assert.Equal(SchoolSettings.WellKnownId, settings.Id);
            Assert.Equal(30, settings.PapierkorbAufbewahrungTage);
        }

        await using (var db = testDb.NewContext())
        {
            Assert.Single(await db.SchoolSettings.ToListAsync());
        }
    }

    [Fact]
    public async Task SchoolSettingsService_updates_stay_on_the_single_row()
    {
        await using var testDb = new SqliteTestDatabase();
        await testDb.MigrateAsync();

        await using (var db = testDb.NewContext())
        {
            var service = new SchoolSettingsService(db);
            var settings = await service.GetAsync();
            settings.Name = "Grundschule Wehringhausen";
            settings.Schuljahr = "2026/27";
            settings.AktuellesHalbjahr = Halbjahr.Zweites;
            await service.SaveAsync(settings);
        }

        await using (var db = testDb.NewContext())
        {
            var all = await db.SchoolSettings.ToListAsync();
            Assert.Single(all);
            Assert.Equal("Grundschule Wehringhausen", all[0].Name);
            Assert.Equal(Halbjahr.Zweites, all[0].AktuellesHalbjahr);
        }
    }

    [Fact]
    public async Task Deleting_a_Klasse_cascades_to_its_Kinder()
    {
        await using var testDb = new SqliteTestDatabase();
        await testDb.MigrateAsync();

        Guid klasseId;
        await using (var db = testDb.NewContext())
        {
            var klasse = new Klasse { Name = "E1", Jahrgang = 1 };
            klasse.Kinder.Add(new Kind { Vorname = "Max", Nachname = "Mustermann" });
            klasse.Kinder.Add(new Kind { Vorname = "Erika", Nachname = "Musterfrau" });
            db.Klassen.Add(klasse);
            await db.SaveChangesAsync();
            klasseId = klasse.Id;
        }

        await using (var db = testDb.NewContext())
        {
            db.Klassen.Remove(await db.Klassen.SingleAsync(k => k.Id == klasseId));
            await db.SaveChangesAsync();
        }

        await using (var db = testDb.NewContext())
        {
            Assert.Empty(await db.Klassen.ToListAsync());
            Assert.Empty(await db.Kinder.IgnoreQueryFilters().ToListAsync());
        }
    }

    [Fact]
    public async Task Zeugnis_unique_index_blocks_a_duplicate_for_same_child_form_and_term()
    {
        await using var testDb = new SqliteTestDatabase();
        await testDb.MigrateAsync();

        await using var db = testDb.NewContext();

        var klasse = new Klasse { Name = "3a", Jahrgang = 3 };
        var kind = new Kind { Vorname = "Max", Nachname = "Mustermann", Klasse = klasse };
        var formular = new Formular { Anzeigename = "Klasse 3 – 1. HJ" };
        db.AddRange(klasse, kind, formular);
        await db.SaveChangesAsync();

        db.Zeugnisse.Add(new Zeugnis { KindId = kind.Id, FormularId = formular.Id, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes });
        await db.SaveChangesAsync();

        db.Zeugnisse.Add(new Zeugnis { KindId = kind.Id, FormularId = formular.Id, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
