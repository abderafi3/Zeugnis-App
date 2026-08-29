using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Entities;
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

        var imported = 0;
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Text) || string.IsNullOrWhiteSpace(row.Bereich))
            {
                uebersprungen++;
                continue;
            }

            db.Floskeln.Add(new Floskel
            {
                Text = row.Text.Trim(),
                Bereich = row.Bereich.Trim(),
                Stufe = Math.Clamp(row.Stufe, 1, 4),
                Klassengruppe = row.Klassengruppe,
            });
            imported++;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ImportSummary(imported, uebersprungen);
    }
}
