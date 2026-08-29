using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Validation;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class FloskelnService(IDbContextFactory<ZeugnisDbContext> contextFactory, IClock clock) : IFloskelnService
{
    private static readonly FloskelValidator Rules = new();

    public async Task<IReadOnlyList<Floskel>> GetAsync(FloskelFilter filter, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = db.Floskeln.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Bereich))
        {
            query = query.Where(f => f.Bereich == filter.Bereich);
        }

        if (filter.Stufe is { } stufe)
        {
            query = query.Where(f => f.Stufe == stufe);
        }

        if (filter.Klassengruppe is { } gruppe)
        {
            query = query.Where(f => f.Klassengruppe == gruppe || f.Klassengruppe == Domain.Enums.Klassengruppe.Beide);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(f => EF.Functions.Like(f.Text, $"%{term}%") || EF.Functions.Like(f.Bereich, $"%{term}%"));
        }

        return await query
            .OrderBy(f => f.Bereich).ThenBy(f => f.Stufe).ThenBy(f => f.Text)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Floskel> SaveAsync(Floskel floskel, CancellationToken cancellationToken = default)
    {
        Validate(floskel);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var target = await db.Floskeln.FirstOrDefaultAsync(f => f.Id == floskel.Id, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            target = new Floskel { Id = floskel.Id };
            db.Floskeln.Add(target);
        }

        target.Text = floskel.Text.Trim();
        target.Bereich = floskel.Bereich.Trim();
        target.Stufe = floskel.Stufe;
        target.Klassengruppe = floskel.Klassengruppe;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return target;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var floskel = await db.Floskeln.FirstOrDefaultAsync(f => f.Id == id, cancellationToken).ConfigureAwait(false);
        if (floskel is null)
        {
            return;
        }

        floskel.DeletedUtc = clock.UtcNow;
        var kurz = floskel.Text.Length > 40 ? floskel.Text[..40] + "…" : floskel.Text;
        AuditTrailWriter.Record(db, PapierkorbTypen.Floskel, floskel.Id, $"Floskel: {kurz}", clock);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetBereicheAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var used = await db.Floskeln.Select(f => f.Bereich).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false);

        return used.Concat(FloskelBereiche.Alle)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b, StringComparer.CurrentCulture)
            .ToList();
    }

    private static void Validate(Floskel floskel)
    {
        var result = Rules.Validate(floskel);
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
