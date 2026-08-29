using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

internal sealed class SchoolSettingsService(ZeugnisDbContext db) : ISchoolSettingsService
{
    private static readonly Guid Id = SchoolSettings.WellKnownId;

    public async Task<SchoolSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await db.SchoolSettings
            .FirstOrDefaultAsync(s => s.Id == Id, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
        {
            settings = new SchoolSettings { Id = Id };
            db.SchoolSettings.Add(settings);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return settings;
    }

    public async Task SaveAsync(SchoolSettings settings, CancellationToken cancellationToken = default)
    {
        var tracked = await db.SchoolSettings
            .FirstOrDefaultAsync(s => s.Id == Id, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            tracked = new SchoolSettings { Id = Id };
            db.SchoolSettings.Add(tracked);
        }

        CopyEditableValues(from: settings, to: tracked);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void CopyEditableValues(SchoolSettings from, SchoolSettings to)
    {
        to.Name = from.Name;
        to.Zusatzbezeichnung = from.Zusatzbezeichnung;
        to.Adresse = from.Adresse;
        to.Schulleitung = from.Schulleitung;
        to.Schuljahr = from.Schuljahr;
        to.AktuellesHalbjahr = from.AktuellesHalbjahr;
        to.Zeugnisausgabedatum = from.Zeugnisausgabedatum;
        to.Konferenzdatum = from.Konferenzdatum;
        to.DatumUnterrichtsWiederbeginn = from.DatumUnterrichtsWiederbeginn;
        to.PapierkorbAufbewahrungTage = from.PapierkorbAufbewahrungTage;
        to.Design = from.Design;
    }
}
