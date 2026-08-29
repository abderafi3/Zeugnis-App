using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Import;

internal sealed class KinderImportService(IDbContextFactory<ZeugnisDbContext> contextFactory) : IKinderImportService
{
    public async Task<ImportSummary> CommitAsync(
        Guid klasseId,
        IReadOnlyList<KindImportData> rows,
        int uebersprungen,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var klasseExists = await db.Klassen.AnyAsync(k => k.Id == klasseId, cancellationToken).ConfigureAwait(false);
        if (!klasseExists)
        {
            throw new InvalidOperationException($"Klasse {klasseId} existiert nicht.");
        }

        var imported = 0;
        foreach (var row in rows)
        {
            // Defensive: the wizard already filters these out.
            if (string.IsNullOrWhiteSpace(row.Vorname) || string.IsNullOrWhiteSpace(row.Nachname))
            {
                uebersprungen++;
                continue;
            }

            db.Kinder.Add(new Kind
            {
                KlasseId = klasseId,
                Vorname = row.Vorname.Trim(),
                Nachname = row.Nachname.Trim(),
                Geburtsdatum = row.Geburtsdatum,
                Geschlecht = row.Geschlecht,
            });
            imported++;
        }

        // One transaction (plan §6, step 4).
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ImportSummary(imported, uebersprungen);
    }
}
