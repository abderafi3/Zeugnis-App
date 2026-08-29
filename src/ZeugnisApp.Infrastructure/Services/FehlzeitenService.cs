using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class FehlzeitenService(IDbContextFactory<ZeugnisDbContext> contextFactory) : IFehlzeitenService
{
    public async Task<IReadOnlyList<FehlzeitZeile>> GetForKlasseAsync(
        Guid klasseId, string schuljahr, Halbjahr halbjahr, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var kinder = await db.Kinder
            .Where(k => k.KlasseId == klasseId)
            .OrderBy(k => k.Nachname).ThenBy(k => k.Vorname)
            .Select(k => new { k.Id, k.Vorname, k.Nachname })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var kindIds = kinder.Select(k => k.Id).ToList();
        var fehlzeiten = await db.Fehlzeiten
            .Where(f => kindIds.Contains(f.KindId) && f.Schuljahr == schuljahr && f.Halbjahr == halbjahr)
            .ToDictionaryAsync(f => f.KindId, cancellationToken)
            .ConfigureAwait(false);

        return kinder
            .Select(k =>
            {
                fehlzeiten.TryGetValue(k.Id, out var f);
                return new FehlzeitZeile(k.Id, $"{k.Vorname} {k.Nachname}".Trim(),
                    f?.StundenGesamt ?? 0, f?.StundenUnentschuldigt ?? 0);
            })
            .ToList();
    }

    public async Task SaveAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, int stundenGesamt, int stundenUnentschuldigt,
        CancellationToken cancellationToken = default)
    {
        if (stundenGesamt < 0 || stundenUnentschuldigt < 0 || stundenUnentschuldigt > stundenGesamt)
        {
            throw new EntityValidationException(new Dictionary<string, string[]>
            {
                [nameof(Fehlzeit.StundenUnentschuldigt)] = ["Unentschuldigte Stunden dürfen die Gesamtstunden nicht übersteigen."],
            });
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var fehlzeit = await db.Fehlzeiten
            .FirstOrDefaultAsync(f => f.KindId == kindId && f.Schuljahr == schuljahr && f.Halbjahr == halbjahr, cancellationToken)
            .ConfigureAwait(false);

        if (fehlzeit is null)
        {
            fehlzeit = new Fehlzeit { KindId = kindId, Schuljahr = schuljahr, Halbjahr = halbjahr };
            db.Fehlzeiten.Add(fehlzeit);
        }

        fehlzeit.StundenGesamt = stundenGesamt;
        fehlzeit.StundenUnentschuldigt = stundenUnentschuldigt;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Fehlzeit?> GetAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Fehlzeiten
            .FirstOrDefaultAsync(f => f.KindId == kindId && f.Schuljahr == schuljahr && f.Halbjahr == halbjahr, cancellationToken)
            .ConfigureAwait(false);
    }
}
