using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Validation;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class FoerderempfehlungService(IDbContextFactory<ZeugnisDbContext> contextFactory)
    : IFoerderempfehlungService
{
    private static readonly FoerderempfehlungValidator Rules = new();

    public async Task<IReadOnlyList<Foerderempfehlung>> GetForKindAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Foerderempfehlungen
            .Where(e => e.KindId == kindId && e.Schuljahr == schuljahr && e.Halbjahr == halbjahr)
            .OrderBy(e => e.Fach)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Foerderempfehlung?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Foerderempfehlungen
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Foerderempfehlung> GetOrCreateAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, string fach,
        CancellationToken cancellationToken = default)
    {
        var fachTrimmed = (fach ?? string.Empty).Trim();

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await db.Foerderempfehlungen
            .FirstOrDefaultAsync(
                e => e.KindId == kindId && e.Schuljahr == schuljahr
                     && e.Halbjahr == halbjahr && e.Fach == fachTrimmed,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        var neu = new Foerderempfehlung
        {
            KindId = kindId,
            Schuljahr = schuljahr,
            Halbjahr = halbjahr,
            Fach = fachTrimmed,
        };
        Validate(neu);

        db.Foerderempfehlungen.Add(neu);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return neu;
    }

    public async Task SaveAsync(Foerderempfehlung empfehlung, CancellationToken cancellationToken = default)
    {
        empfehlung.Fach = empfehlung.Fach.Trim();
        Validate(empfehlung);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var target = await db.Foerderempfehlungen
            .FirstOrDefaultAsync(e => e.Id == empfehlung.Id, cancellationToken)
            .ConfigureAwait(false);

        if (target is null)
        {
            target = new Foerderempfehlung
            {
                Id = empfehlung.Id,
                KindId = empfehlung.KindId,
                Schuljahr = empfehlung.Schuljahr,
                Halbjahr = empfehlung.Halbjahr,
            };
            db.Foerderempfehlungen.Add(target);
        }

        target.Fach = empfehlung.Fach;
        target.Beobachtungen = Trim(empfehlung.Beobachtungen);
        target.SchulischeMassnahmen = Trim(empfehlung.SchulischeMassnahmen);
        target.EmpfehlungenFuerZuhause = Trim(empfehlung.EmpfehlungenFuerZuhause);
        target.Fertiggestellt = empfehlung.Fertiggestellt;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Foerderempfehlungen
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string? Trim(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Validate(Foerderempfehlung empfehlung)
    {
        var result = Rules.Validate(empfehlung);
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
