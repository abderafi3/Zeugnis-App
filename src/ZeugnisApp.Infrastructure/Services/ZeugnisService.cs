using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class ZeugnisService(IDbContextFactory<ZeugnisDbContext> contextFactory) : IZeugnisService
{
    public async Task<ZeugnisErgebnis> GetOrCreateAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, Guid? formularIdOverride = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var kind = await db.Kinder.Include(k => k.Klasse)
            .FirstOrDefaultAsync(k => k.Id == kindId, cancellationToken)
            .ConfigureAwait(false);
        if (kind is null)
        {
            return new ZeugnisErgebnis(null, false, null, "Kind nicht gefunden.");
        }

        var alleFormulare = await db.Formulare.ToListAsync(cancellationToken).ConfigureAwait(false);
        var suggestion = formularIdOverride is { } id
            ? new FormularSuggestion(alleFormulare.FirstOrDefault(f => f.Id == id), SuggestionReason.ManuellFestgelegt)
            : FormularSuggester.Suggest(alleFormulare, kind, kind.Klasse?.Jahrgang ?? 0, halbjahr);

        if (suggestion.Formular is null)
        {
            return new ZeugnisErgebnis(null, false, suggestion.Reason, "Kein passendes Formular vorhanden.");
        }

        var existing = await db.Zeugnisse
            .Include(z => z.Freitexte)
            .Include(z => z.Noten)
            .FirstOrDefaultAsync(
                z => z.KindId == kindId && z.FormularId == suggestion.Formular.Id
                     && z.Schuljahr == schuljahr && z.Halbjahr == halbjahr,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return new ZeugnisErgebnis(existing, false, suggestion.Reason, null);
        }

        // Auto-transfer the current absence totals (plan §Sprint 7). Still editable per report.
        var fehlzeit = await db.Fehlzeiten
            .FirstOrDefaultAsync(f => f.KindId == kindId && f.Schuljahr == schuljahr && f.Halbjahr == halbjahr, cancellationToken)
            .ConfigureAwait(false);

        var zeugnis = new Zeugnis
        {
            KindId = kindId,
            FormularId = suggestion.Formular.Id,
            Schuljahr = schuljahr,
            Halbjahr = halbjahr,
            FehlstundenGesamt = fehlzeit?.StundenGesamt,
            FehlstundenUnentschuldigt = fehlzeit?.StundenUnentschuldigt,
        };

        db.Zeugnisse.Add(zeugnis);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ZeugnisErgebnis(zeugnis, true, suggestion.Reason, null);
    }

    public async Task<Zeugnis?> GetAsync(Guid zeugnisId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Zeugnisse
            .Include(z => z.Freitexte)
            .Include(z => z.Noten)
            .FirstOrDefaultAsync(z => z.Id == zeugnisId, cancellationToken)
            .ConfigureAwait(false);
    }
}
