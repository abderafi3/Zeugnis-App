namespace ZeugnisApp.Application.Import;

/// <summary>Built-in import profiles.</summary>
public static class ImportProfiles
{
    /// <summary>Class-list import: first name, last name (required), birth date and gender (optional).</summary>
    public static ImportProfile Kinder { get; } = new("Kinder",
    [
        new ImportField(
            ImportFieldKeys.Vorname, "Vorname", Required: true,
            HeaderAliases: ["vorname", "first name", "firstname", "given name", "rufname"]),

        new ImportField(
            ImportFieldKeys.Nachname, "Nachname", Required: true,
            HeaderAliases: ["nachname", "name", "surname", "last name", "lastname", "familienname", "family name"]),

        new ImportField(
            ImportFieldKeys.Geburtsdatum, "Geburtsdatum", Required: false,
            HeaderAliases: ["geburtsdatum", "geburtstag", "geb", "geb.", "geb.-datum", "geburt", "dob", "date of birth", "birthdate", "birthday"],
            Looks: ImportValueParser.LooksLikeDate),

        new ImportField(
            ImportFieldKeys.Geschlecht, "Geschlecht", Required: false,
            HeaderAliases: ["geschlecht", "sex", "gender", "m/w", "m/w/d", "g."],
            Looks: ImportValueParser.LooksLikeGeschlecht),
    ]);
}
