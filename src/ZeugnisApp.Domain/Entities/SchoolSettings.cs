using ZeugnisApp.Domain.Common;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// School-wide settings (plan §4). Singleton — exactly one row, identified by
/// <see cref="WellKnownId"/>, created on first run.
/// </summary>
public class SchoolSettings : EntityBase
{
    /// <summary>Fixed primary key of the single settings row.</summary>
    public static readonly Guid WellKnownId = new("00000000-0000-0000-0000-000000000001");

    public string Name { get; set; } = string.Empty;

    /// <summary>Optional second line of the school name (e.g. "Städt. Gemeinschaftsgrundschule").</summary>
    public string? Zusatzbezeichnung { get; set; }

    public string Adresse { get; set; } = string.Empty;

    /// <summary>Name of the head teacher, as it should appear on report cards.</summary>
    public string Schulleitung { get; set; } = string.Empty;

    /// <summary>Current school year, e.g. "2026/27".</summary>
    public string Schuljahr { get; set; } = string.Empty;

    public Halbjahr AktuellesHalbjahr { get; set; } = Halbjahr.Erstes;

    /// <summary>Default date printed as the report-card issue date; a class may override it.</summary>
    public DateOnly? Zeugnisausgabedatum { get; set; }

    public DateOnly? Konferenzdatum { get; set; }

    /// <summary>Date lessons resume after the holidays (printed on some templates).</summary>
    public DateOnly? DatumUnterrichtsWiederbeginn { get; set; }

    /// <summary>
    /// Retention window (days) before a soft-deleted item is eligible for permanent purge
    /// (plan §4 / §Sprint 11). Default 30.
    /// </summary>
    public int PapierkorbAufbewahrungTage { get; set; } = 30;
}
