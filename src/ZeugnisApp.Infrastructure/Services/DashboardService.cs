using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class DashboardService(ZeugnisDbContext db) : IDashboardService
{
    public async Task<DashboardCounts> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        // Soft-delete filters apply automatically, so these are "live" counts.
        var klassen = await db.Klassen.CountAsync(cancellationToken).ConfigureAwait(false);
        var kinder = await db.Kinder.CountAsync(cancellationToken).ConfigureAwait(false);
        var formulare = await db.Formulare.CountAsync(cancellationToken).ConfigureAwait(false);
        var floskeln = await db.Floskeln.CountAsync(cancellationToken).ConfigureAwait(false);
        var offen = await db.Zeugnisse.CountAsync(z => !z.Fertiggestellt, cancellationToken).ConfigureAwait(false);

        return new DashboardCounts(klassen, kinder, formulare, floskeln, offen);
    }
}
