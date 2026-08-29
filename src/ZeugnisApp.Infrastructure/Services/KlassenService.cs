using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Validation;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class KlassenService(IDbContextFactory<ZeugnisDbContext> contextFactory, Application.Abstractions.IClock clock)
    : IKlassenService
{
    private static readonly KlasseValidator KlasseRules = new();
    private static readonly KindValidator KindRules = new();

    public async Task<IReadOnlyList<KlasseListItem>> GetKlassenAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var klassen = await db.Klassen
            .OrderBy(k => k.Jahrgang).ThenBy(k => k.Name)
            .Select(k => new { k.Id, k.Name, k.Jahrgang, k.Klassenleitung })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Pull the minimal fields needed to compute "incomplete" per child (Kind.IstVollstaendig
        // is a computed property and cannot be translated to SQL).
        var kinder = await db.Kinder
            .Select(k => new { k.KlasseId, k.Vorname, k.Nachname, k.Geburtsdatum, k.Geschlecht })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byKlasse = kinder
            .GroupBy(k => k.KlasseId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Total: g.Count(),
                    Incomplete: g.Count(x =>
                        string.IsNullOrWhiteSpace(x.Vorname)
                        || string.IsNullOrWhiteSpace(x.Nachname)
                        || x.Geburtsdatum is null
                        || x.Geschlecht is null)));

        return klassen
            .Select(k =>
            {
                byKlasse.TryGetValue(k.Id, out var stats);
                return new KlasseListItem(k.Id, k.Name, k.Jahrgang, k.Klassenleitung, stats.Total, stats.Incomplete);
            })
            .ToList();
    }

    public async Task<Klasse?> GetKlasseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Klassen.FirstOrDefaultAsync(k => k.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Klasse> SaveKlasseAsync(Klasse klasse, CancellationToken cancellationToken = default)
    {
        Validate(KlasseRules, klasse);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var target = await db.Klassen.FirstOrDefaultAsync(k => k.Id == klasse.Id, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            target = new Klasse { Id = klasse.Id };
            db.Klassen.Add(target);
        }

        target.Name = klasse.Name.Trim();
        target.Jahrgang = klasse.Jahrgang;
        target.Klassenleitung = klasse.Klassenleitung.Trim();
        target.Zeugnisdatum = klasse.Zeugnisdatum;
        target.Wochenstundenplan = new Wochenstundenplan
        {
            Montag = klasse.Wochenstundenplan.Montag,
            Dienstag = klasse.Wochenstundenplan.Dienstag,
            Mittwoch = klasse.Wochenstundenplan.Mittwoch,
            Donnerstag = klasse.Wochenstundenplan.Donnerstag,
            Freitag = klasse.Wochenstundenplan.Freitag,
        };

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return target;
    }

    public async Task DeleteKlasseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var klasse = await db.Klassen.FirstOrDefaultAsync(k => k.Id == id, cancellationToken).ConfigureAwait(false);
        if (klasse is null)
        {
            return;
        }

        var now = clock.UtcNow;
        var kindIds = await db.Kinder.Where(x => x.KlasseId == id).Select(x => x.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Cascade the soft-delete: class -> its Kinder -> their Zeugnisse. Fehlzeiten follow the
        // child (hard rows, hidden because the child is filtered out).
        await db.Kinder.Where(x => x.KlasseId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedUtc, now), cancellationToken).ConfigureAwait(false);
        await db.Zeugnisse.Where(z => kindIds.Contains(z.KindId))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedUtc, now), cancellationToken).ConfigureAwait(false);

        klasse.DeletedUtc = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Kind>> GetKinderAsync(Guid klasseId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Kinder
            .Where(k => k.KlasseId == klasseId)
            .OrderBy(k => k.Nachname).ThenBy(k => k.Vorname)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Kind?> GetKindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Kinder.FirstOrDefaultAsync(k => k.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Kind> SaveKindAsync(Kind kind, CancellationToken cancellationToken = default)
    {
        Validate(KindRules, kind);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var target = await db.Kinder.FirstOrDefaultAsync(k => k.Id == kind.Id, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            target = new Kind { Id = kind.Id };
            db.Kinder.Add(target);
        }

        target.Vorname = kind.Vorname.Trim();
        target.Nachname = kind.Nachname.Trim();
        target.Geburtsdatum = kind.Geburtsdatum;
        target.Geschlecht = kind.Geschlecht;
        target.KlasseId = kind.KlasseId;
        target.Foerderstatus = kind.Foerderstatus;
        target.Foerderschwerpunkt = string.IsNullOrWhiteSpace(kind.Foerderschwerpunkt) ? null : kind.Foerderschwerpunkt.Trim();
        target.ManuellesFormularId = kind.ManuellesFormularId;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return target;
    }

    public async Task DeleteKindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var kind = await db.Kinder.FirstOrDefaultAsync(k => k.Id == id, cancellationToken).ConfigureAwait(false);
        if (kind is null)
        {
            return;
        }

        var now = clock.UtcNow;
        await db.Zeugnisse.Where(z => z.KindId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedUtc, now), cancellationToken).ConfigureAwait(false);

        kind.DeletedUtc = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FormularOption>> GetFormularOptionenAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Formulare
            .OrderBy(f => f.Anzeigename)
            .Select(f => new FormularOption(f.Id, f.Anzeigename))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static void Validate<T>(IValidator<T> validator, T instance)
    {
        var result = validator.Validate(instance);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        throw new EntityValidationException(errors);
    }
}
