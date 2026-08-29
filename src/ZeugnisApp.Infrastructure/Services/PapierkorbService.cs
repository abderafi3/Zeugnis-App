using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class PapierkorbService(IDbContextFactory<ZeugnisDbContext> contextFactory, IClock clock) : IPapierkorbService
{
    public async Task<IReadOnlyList<PapierkorbEintrag>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.AuditTrail
            .OrderByDescending(a => a.DeletedAtUtc)
            .Select(a => new PapierkorbEintrag(a.Id, a.EntityType, a.Bezeichnung, a.DeletedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RestoreAsync(Guid auditEntryId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entry = await db.AuditTrail.FirstOrDefaultAsync(a => a.Id == auditEntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
        {
            return;
        }

        switch (entry.EntityType)
        {
            case PapierkorbTypen.Klasse:
                var kindIds = await db.Kinder.IgnoreQueryFilters()
                    .Where(k => k.KlasseId == entry.EntityId).Select(k => k.Id)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                await ClearDeleted(db.Klassen, k => k.Id == entry.EntityId, cancellationToken).ConfigureAwait(false);
                await ClearDeleted(db.Kinder, k => k.KlasseId == entry.EntityId, cancellationToken).ConfigureAwait(false);
                await ClearDeleted(db.Zeugnisse, z => kindIds.Contains(z.KindId), cancellationToken).ConfigureAwait(false);
                break;

            case PapierkorbTypen.Kind:
                await ClearDeleted(db.Kinder, k => k.Id == entry.EntityId, cancellationToken).ConfigureAwait(false);
                await ClearDeleted(db.Zeugnisse, z => z.KindId == entry.EntityId, cancellationToken).ConfigureAwait(false);
                break;

            case PapierkorbTypen.Zeugnis:
                await ClearDeleted(db.Zeugnisse, z => z.Id == entry.EntityId, cancellationToken).ConfigureAwait(false);
                break;

            case PapierkorbTypen.Floskel:
                await ClearDeleted(db.Floskeln, f => f.Id == entry.EntityId, cancellationToken).ConfigureAwait(false);
                break;

            case PapierkorbTypen.Formular:
                await ClearDeleted(db.Formulare, f => f.Id == entry.EntityId, cancellationToken).ConfigureAwait(false);
                break;
        }

        db.AuditTrail.Remove(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> PurgeExpiredAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var cutoff = clock.UtcNow.AddDays(-Math.Max(1, retentionDays));
        var expired = await db.AuditTrail
            .Where(a => a.DeletedAtUtc < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var entry in expired)
        {
            switch (entry.EntityType)
            {
                case PapierkorbTypen.Klasse:
                    // ON DELETE CASCADE takes the Kinder / Fehlzeiten / Zeugnisse with it.
                    await db.Klassen.IgnoreQueryFilters().Where(k => k.Id == entry.EntityId)
                        .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case PapierkorbTypen.Kind:
                    await db.Kinder.IgnoreQueryFilters().Where(k => k.Id == entry.EntityId)
                        .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case PapierkorbTypen.Zeugnis:
                    await db.Zeugnisse.IgnoreQueryFilters().Where(z => z.Id == entry.EntityId)
                        .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case PapierkorbTypen.Floskel:
                    await db.Floskeln.IgnoreQueryFilters().Where(f => f.Id == entry.EntityId)
                        .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case PapierkorbTypen.Formular:
                    await db.Formulare.IgnoreQueryFilters().Where(f => f.Id == entry.EntityId)
                        .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        db.AuditTrail.RemoveRange(expired);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return expired.Count;
    }

    private static async Task ClearDeleted<T>(
        DbSet<T> set, System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        where T : class, ZeugnisApp.Domain.Common.ISoftDeletable
        => await set.IgnoreQueryFilters().Where(predicate)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedUtc, (DateTime?)null), cancellationToken)
            .ConfigureAwait(false);
}
