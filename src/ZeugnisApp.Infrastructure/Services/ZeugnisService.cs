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
            : FormularSuggester.Suggest(alleFormulare, kind, kind.EffektivesSchulbesuchsjahr, halbjahr);

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

    public async Task<Zeugnis?> GetWithContextAsync(Guid zeugnisId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Zeugnisse
            .Include(z => z.Freitexte)
            .Include(z => z.Noten)
            .Include(z => z.Kind!).ThenInclude(k => k.Klasse)
            .Include(z => z.Formular!).ThenInclude(f => f.Freitextfelder)
            .FirstOrDefaultAsync(z => z.Id == zeugnisId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(Zeugnis zeugnis, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var target = await db.Zeugnisse
            .Include(z => z.Freitexte)
            .Include(z => z.Noten)
            .FirstOrDefaultAsync(z => z.Id == zeugnis.Id, cancellationToken)
            .ConfigureAwait(false);

        if (target is null)
        {
            return;
        }

        target.Empfehlung = Trim(zeugnis.Empfehlung);
        target.EmpfehlungBegruendung = Trim(zeugnis.EmpfehlungBegruendung);
        target.VersetzungsvermerkText = Trim(zeugnis.VersetzungsvermerkText);
        target.Bemerkungen = Trim(zeugnis.Bemerkungen);
        target.FehlstundenGesamt = zeugnis.FehlstundenGesamt;
        target.FehlstundenUnentschuldigt = zeugnis.FehlstundenUnentschuldigt;
        target.Fertiggestellt = zeugnis.Fertiggestellt;
        target.GeneratedFilePath = Trim(zeugnis.GeneratedFilePath);

        target.Freitexte.Clear();
        target.Freitexte.AddRange(zeugnis.Freitexte.Select(f => new FreitextEintrag { FeldName = f.FeldName, Text = f.Text }));

        target.Noten.Clear();
        target.Noten.AddRange(zeugnis.Noten.Select(n => new NoteEintrag { Fach = n.Fach, Note = n.Note }));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetFertiggestelltAsync(Guid zeugnisId, bool fertiggestellt, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Zeugnisse
            .Where(z => z.Id == zeugnisId)
            .ExecuteUpdateAsync(s => s.SetProperty(z => z.Fertiggestellt, fertiggestellt), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetManuellesFormularAsync(Guid kindId, Guid? formularId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Kinder
            .Where(k => k.Id == kindId)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.ManuellesFormularId, formularId), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ZeugnisUebersichtZeile>> GetUebersichtForKlasseAsync(
        Guid klasseId, string schuljahr, Halbjahr halbjahr, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var kinder = await db.Kinder
            .Where(k => k.KlasseId == klasseId)
            .OrderBy(k => k.Nachname).ThenBy(k => k.Vorname)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var jahrgang = await db.Klassen.Where(k => k.Id == klasseId).Select(k => k.Jahrgang)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        var alleFormulare = await db.Formulare.ToListAsync(cancellationToken).ConfigureAwait(false);

        var kindIds = kinder.Select(k => k.Id).ToList();
        var zeugnisse = await db.Zeugnisse
            .Where(z => kindIds.Contains(z.KindId) && z.Schuljahr == schuljahr && z.Halbjahr == halbjahr)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rows = new List<ZeugnisUebersichtZeile>();
        foreach (var kind in kinder)
        {
            var zeugnis = zeugnisse.FirstOrDefault(z => z.KindId == kind.Id);
            var suggestion = FormularSuggester.Suggest(alleFormulare, kind, jahrgang, halbjahr);
            var formularName = zeugnis is not null
                ? alleFormulare.FirstOrDefault(f => f.Id == zeugnis.FormularId)?.Anzeigename
                : suggestion.Formular?.Anzeigename;

            rows.Add(new ZeugnisUebersichtZeile(
                kind.Id, kind.AnzeigeName, zeugnis?.Id, zeugnis?.Fertiggestellt ?? false,
                formularName, suggestion.Formular is not null || zeugnis is not null,
                kind.ManuellesFormularId));
        }

        return rows;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
