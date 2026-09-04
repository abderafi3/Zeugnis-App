using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Import;

internal sealed class FehlzeitenImportService(IDbContextFactory<ZeugnisDbContext> contextFactory)
    : IFehlzeitenImportService
{
    public async Task<FehlzeitenImportErgebnis> CommitAsync(
        Guid klasseId,
        string schuljahr,
        Halbjahr halbjahr,
        IReadOnlyList<FehlzeitImportData> rows,
        int uebersprungen,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var kinder = await db.Kinder
            .Where(k => k.KlasseId == klasseId)
            .Select(k => new { k.Id, k.Vorname, k.Nachname })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // "Vorname Nachname" and "Nachname Vorname" both resolve to the same child; a name shared by
        // two children is left unresolved rather than guessed.
        var byName = new Dictionary<string, Guid?>(StringComparer.Ordinal);
        void Register(string key, Guid id)
        {
            if (key.Length == 0)
            {
                return;
            }

            byName[key] = byName.TryGetValue(key, out var existing) && existing != id ? null : id;
        }

        foreach (var k in kinder)
        {
            Register(Normalize($"{k.Vorname} {k.Nachname}"), k.Id);
            Register(Normalize($"{k.Nachname} {k.Vorname}"), k.Id);
        }

        var fehlzeiten = await db.Fehlzeiten
            .Where(f => f.Schuljahr == schuljahr && f.Halbjahr == halbjahr
                        && kinder.Select(k => k.Id).Contains(f.KindId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var imported = 0;
        var nichtZugeordnet = new List<string>();

        foreach (var row in rows)
        {
            var key = Normalize(row.Name);
            if (!byName.TryGetValue(key, out var kindId) || kindId is null)
            {
                nichtZugeordnet.Add(row.Name);
                uebersprungen++;
                continue;
            }

            var gesamt = Math.Max(0, row.StundenGesamt);
            var fehlzeit = fehlzeiten.FirstOrDefault(f => f.KindId == kindId.Value);
            if (fehlzeit is null)
            {
                fehlzeit = new Fehlzeit
                {
                    KindId = kindId.Value,
                    Schuljahr = schuljahr,
                    Halbjahr = halbjahr,
                };
                db.Fehlzeiten.Add(fehlzeit);
                fehlzeiten.Add(fehlzeit);
            }

            fehlzeit.StundenGesamt = gesamt;

            // Excused/unexcused split is a manual entry (the school's PDF has no such column): only
            // overwrite it when the source really carries one; otherwise keep it within the new total.
            fehlzeit.StundenUnentschuldigt = row.StundenUnentschuldigt is int u
                ? Math.Clamp(u, 0, gesamt)
                : Math.Min(fehlzeit.StundenUnentschuldigt, gesamt);

            imported++;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FehlzeitenImportErgebnis(imported, uebersprungen, nichtZugeordnet);
    }

    private static string Normalize(string value)
    {
        var collapsed = string.Join(' ', value
            .Replace(',', ' ')
            .Replace('.', ' ')
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return collapsed.ToLower(CultureInfo.GetCultureInfo("de-DE")).Trim();
    }
}
