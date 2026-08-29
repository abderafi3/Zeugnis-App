using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Application.Validation;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Persistence;
using ZeugnisAppPaths = ZeugnisApp.Infrastructure.ZeugnisAppPaths;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class FormulareService(IDbContextFactory<ZeugnisDbContext> contextFactory, IClock clock) : IFormulareService
{
    private static readonly FormularValidator Rules = new();

    public async Task<IReadOnlyList<FormularListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var formulare = await db.Formulare
            .OrderBy(f => f.Jahrgang).ThenBy(f => f.GiltFuerHalbjahr).ThenBy(f => f.Anzeigename)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return formulare
            .Select(f => new FormularListItem(
                f.Id, f.Anzeigename, f.Jahrgang, f.GiltFuerHalbjahr, f.IstZieldifferent, f.IstGenerierungsbereit))
            .ToList();
    }

    public async Task<Formular?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Formulare
            .Include(f => f.Freitextfelder)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Formular> SaveAsync(Formular formular, CancellationToken cancellationToken = default)
    {
        Validate(formular);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var target = await db.Formulare
            .Include(f => f.Freitextfelder)
            .FirstOrDefaultAsync(f => f.Id == formular.Id, cancellationToken)
            .ConfigureAwait(false);

        if (target is null)
        {
            target = new Formular { Id = formular.Id };
            db.Formulare.Add(target);
        }

        target.Anzeigename = formular.Anzeigename.Trim();
        target.Ueberschrift = string.IsNullOrWhiteSpace(formular.Ueberschrift) ? null : formular.Ueberschrift.Trim();
        target.Jahrgang = formular.Jahrgang;
        target.GiltFuerHalbjahr = formular.GiltFuerHalbjahr;
        target.HatNotenblock = formular.HatNotenblock;
        target.HatBegruendeteEmpfehlung = formular.HatBegruendeteEmpfehlung;
        target.HatVersetzungsvermerk = formular.HatVersetzungsvermerk;
        target.IstZieldifferent = formular.IstZieldifferent;
        target.WordFilePath = string.IsNullOrWhiteSpace(formular.WordFilePath) ? null : formular.WordFilePath.Trim();
        target.FieldMappingJson = formular.FieldMappingJson;
        target.Faecher = [.. formular.Faecher.Select(x => x.Trim()).Where(x => x.Length > 0)];

        target.Freitextfelder.Clear();
        target.Freitextfelder.AddRange(formular.Freitextfelder
            .Where(f => !string.IsNullOrWhiteSpace(f.Name))
            .Select((f, i) => new FreitextFeldDefinition
            {
                Name = f.Name.Trim(),
                Hinweistext = string.IsNullOrWhiteSpace(f.Hinweistext) ? null : f.Hinweistext.Trim(),
                Reihenfolge = i,
                MaxZeichen = f.MaxZeichen,
            }));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return target;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var formular = await db.Formulare.FirstOrDefaultAsync(f => f.Id == id, cancellationToken).ConfigureAwait(false);
        if (formular is null)
        {
            return;
        }

        formular.DeletedUtc = clock.UtcNow;
        AuditTrailWriter.Record(db, PapierkorbTypen.Formular, formular.Id, formular.Anzeigename, clock);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> LinkTemplateAsync(
        Guid formularId, string sourceDocxPath, string fieldMappingJson, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceDocxPath))
        {
            throw new FileNotFoundException("Die Vorlagendatei wurde nicht gefunden.", sourceDocxPath);
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var formular = await db.Formulare.FirstOrDefaultAsync(f => f.Id == formularId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Formular {formularId} existiert nicht.");

        var safeName = string.Concat(formular.Anzeigename.Select(c =>
            char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '_')).Trim();
        var target = Path.Combine(ZeugnisAppPaths.TemplatesDirectory, $"{(safeName.Length == 0 ? "Formular" : safeName)}.docx");

        if (!string.Equals(Path.GetFullPath(sourceDocxPath), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourceDocxPath, target, overwrite: true);
        }

        formular.WordFilePath = target;
        formular.FieldMappingJson = fieldMappingJson;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return target;
    }

    public async Task<FormularSuggestion> SuggestAsync(Guid kindId, Halbjahr halbjahr, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var kind = await db.Kinder
            .Include(k => k.Klasse)
            .FirstOrDefaultAsync(k => k.Id == kindId, cancellationToken)
            .ConfigureAwait(false);

        if (kind is null)
        {
            return new FormularSuggestion(null, SuggestionReason.KeinPassendesFormular);
        }

        var alle = await db.Formulare.ToListAsync(cancellationToken).ConfigureAwait(false);
        return FormularSuggester.Suggest(alle, kind, kind.Klasse?.Jahrgang ?? 0, halbjahr);
    }

    private static void Validate(Formular formular)
    {
        var result = Rules.Validate(formular);
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
