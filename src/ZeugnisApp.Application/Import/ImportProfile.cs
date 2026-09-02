namespace ZeugnisApp.Application.Import;

/// <summary>Sentinel role meaning "do not import this column".</summary>
public static class ImportFieldKeys
{
    public const string Ignore = "";
    public const string Vorname = "vorname";
    public const string Nachname = "nachname";
    public const string Geburtsdatum = "geburtsdatum";
    public const string Geschlecht = "geschlecht";

    // Floskeln import
    public const string FloskelText = "text";
    public const string FloskelKuerzel = "kuerzel";
    public const string FloskelBereich = "bereich";
    public const string FloskelGruppe = "gruppe";
    public const string FloskelFach = "fach";
    public const string FloskelStufe = "stufe";
    public const string FloskelNiveau = "niveau";
    public const string FloskelKlassengruppe = "klassengruppe";
}

/// <summary>One target column of an import (plan §6, step 2).</summary>
/// <param name="Key">Stable identifier, also the dictionary key on a mapped row.</param>
/// <param name="DisplayName">Shown in the column-role picker.</param>
/// <param name="Required">A row missing this value is flagged red and skipped on commit.</param>
/// <param name="HeaderAliases">Lower-case header texts that map to this field.</param>
/// <param name="Looks">Content heuristic used when headers are missing/ambiguous.</param>
public sealed record ImportField(
    string Key,
    string DisplayName,
    bool Required,
    IReadOnlyList<string> HeaderAliases,
    Func<string, bool>? Looks = null);

/// <summary>
/// The set of target fields for one kind of import. The same pipeline is reused for the
/// Floskeln import (plan §6) with a different profile.
/// </summary>
public sealed class ImportProfile(string name, IReadOnlyList<ImportField> fields)
{
    public string Name { get; } = name;

    public IReadOnlyList<ImportField> Fields { get; } = fields;

    public IEnumerable<ImportField> RequiredFields => Fields.Where(f => f.Required);

    public ImportField? ByKey(string key) => Fields.FirstOrDefault(f => f.Key == key);
}
