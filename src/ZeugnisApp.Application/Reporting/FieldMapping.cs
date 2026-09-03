using System.Text.Json;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Reporting;

/// <summary>
/// Maps the app's value keys to content-control tags in a template's <c>.docx</c>
/// (stored as <see cref="Formular.FieldMappingJson"/>, plan §4/§5). Populated with sensible
/// defaults in Sprint 6; a teacher re-maps it in the Sprint 10 wizard.
/// </summary>
public sealed class FieldMapping
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>value key (see <see cref="ValueKeys"/>) → content-control <c>Tag</c>.</summary>
    public Dictionary<string, string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? TagFor(string valueKey)
        => Tags.TryGetValue(valueKey, out var tag) && !string.IsNullOrWhiteSpace(tag) ? tag : null;

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static FieldMapping FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FieldMapping();
        }

        try
        {
            return JsonSerializer.Deserialize<FieldMapping>(json, JsonOptions) ?? new FieldMapping();
        }
        catch (JsonException)
        {
            return new FieldMapping();
        }
    }

    /// <summary>
    /// A default mapping for a template: one tag per value key, tag = value key with the separators
    /// turned into PascalCase_WithUnderscores (e.g. <c>freitext.Sozialverhalten → Freitext_Sozialverhalten</c>).
    /// The template preparation step inserts exactly these tags.
    /// </summary>
    public static FieldMapping Default(Formular formular)
    {
        var mapping = new FieldMapping();

        foreach (var key in ValueKeys.Standard)
        {
            mapping.Tags[key] = ToTag(key);
        }

        foreach (var fach in formular.Faecher)
        {
            var key = ValueKeys.Note(fach);
            mapping.Tags[key] = ToTag(key);
        }

        foreach (var feld in formular.Freitextfelder)
        {
            var key = ValueKeys.Freitext(feld.Name);
            mapping.Tags[key] = ToTag(key);
        }

        return mapping;
    }

    private static string ToTag(string valueKey)
    {
        var parts = valueKey.Split('.', '_', ' ', '/');
        return string.Join('_', parts.Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
    }
}

/// <summary>The value keys the engine understands.</summary>
public static class ValueKeys
{
    public const string SchuleName = "schule.name";
    public const string SchuleZusatz = "schule.zusatz";
    public const string SchuleAdresse = "schule.adresse";
    public const string SchuleLeitung = "schule.leitung";
    public const string SchuleSchuljahr = "schule.schuljahr";
    public const string SchuleHalbjahr = "schule.halbjahr";
    public const string SchuleKonferenzdatum = "schule.konferenzdatum";
    public const string SchuleWiederbeginn = "schule.wiederbeginn";

    public const string KlasseName = "klasse.name";
    public const string KlasseLeitung = "klasse.leitung";
    public const string KlasseZeugnisdatum = "klasse.zeugnisdatum";

    public const string KindVorname = "kind.vorname";
    public const string KindNachname = "kind.nachname";
    public const string KindName = "kind.name";
    public const string KindGeburtsdatum = "kind.geburtsdatum";

    public const string ZeugnisEmpfehlung = "zeugnis.empfehlung";
    public const string ZeugnisEmpfehlungBegruendung = "zeugnis.empfehlung_begruendung";
    public const string ZeugnisVersetzungsvermerk = "zeugnis.versetzungsvermerk";
    public const string ZeugnisBemerkungen = "zeugnis.bemerkungen";
    public const string ZeugnisFehlstundenGesamt = "zeugnis.fehlstunden_gesamt";
    public const string ZeugnisFehlstundenUnentschuldigt = "zeugnis.fehlstunden_unentschuldigt";

    public static readonly IReadOnlyList<string> Standard =
    [
        SchuleName, SchuleZusatz, SchuleAdresse, SchuleLeitung, SchuleSchuljahr, SchuleHalbjahr,
        SchuleKonferenzdatum, SchuleWiederbeginn,
        KlasseName, KlasseLeitung, KlasseZeugnisdatum,
        KindVorname, KindNachname, KindName, KindGeburtsdatum,
        ZeugnisEmpfehlung, ZeugnisEmpfehlungBegruendung, ZeugnisVersetzungsvermerk, ZeugnisBemerkungen,
        ZeugnisFehlstundenGesamt, ZeugnisFehlstundenUnentschuldigt,
    ];

    public static string Note(string fach) => $"note.{fach}";
    public static string Freitext(string feldName) => $"freitext.{feldName}";
}
