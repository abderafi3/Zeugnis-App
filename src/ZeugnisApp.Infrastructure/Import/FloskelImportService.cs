using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Floskeln;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Import;

internal sealed class FloskelImportService(IDbContextFactory<ZeugnisDbContext> contextFactory) : IFloskelImportService
{
    public async Task<ImportSummary> CommitAsync(
        IReadOnlyList<FloskelImportData> rows,
        int uebersprungen,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // A Kürzel is the school's stable id — skip rows whose Kürzel is already in the library so a
        // re-import of an updated spreadsheet only adds what is new.
        var bekannteKuerzel = await db.Floskeln.IgnoreQueryFilters()
            .Where(f => f.Kuerzel != null && f.Kuerzel != "")
            .Select(f => f.Kuerzel!)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var vorhanden = new HashSet<string>(bekannteKuerzel, StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        foreach (var row in rows)
        {
            var kuerzel = row.Kuerzel?.Trim();

            if (string.IsNullOrWhiteSpace(row.Text)
                || (!string.IsNullOrEmpty(kuerzel) && !vorhanden.Add(kuerzel)))
            {
                uebersprungen++;
                continue;
            }

            // A phrase with a graded slash choice ("freudig/oft/teilweise") lands as one Floskel
            // per variant; the Kürzel is suffixed a/b/c so a re-import still de-dupes on it.
            var varianten = FloskelVariantExpander.Expand(row.Text.Trim());
            for (var i = 0; i < varianten.Count; i++)
            {
                db.Floskeln.Add(new Floskel
                {
                    Text = varianten[i],
                    Kuerzel = VariantenKuerzel(kuerzel, i, varianten.Count),
                    Bereich = row.Bereich.Trim(),
                    Stufe = Math.Clamp(row.Stufe, 0, 4),
                    Niveau = Math.Clamp(row.Niveau, 0, 5),
                    Klassengruppe = row.Klassengruppe,
                });
                imported++;
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ImportSummary(imported, uebersprungen);
    }

    private static string? VariantenKuerzel(string? kuerzel, int index, int count)
    {
        if (string.IsNullOrEmpty(kuerzel))
        {
            return null;
        }

        return count == 1 ? kuerzel : kuerzel + (char)('a' + index);
    }
}
