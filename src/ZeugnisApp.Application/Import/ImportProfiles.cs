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

    /// <summary>Phrase-library import (plan §Sprint 4): text + Bereich required, Stufe/Klassengruppe optional.</summary>
    public static ImportProfile Floskeln { get; } = new("Floskeln",
    [
        new ImportField(
            ImportFieldKeys.FloskelText, "Text", Required: true,
            HeaderAliases: ["text", "floskel", "formulierung", "baustein", "satz", "inhalt"]),

        new ImportField(
            ImportFieldKeys.FloskelBereich, "Bereich", Required: true,
            HeaderAliases: ["bereich", "kategorie", "fach", "thema", "category"]),

        new ImportField(
            ImportFieldKeys.FloskelStufe, "Stufe", Required: false,
            HeaderAliases: ["stufe", "jahrgang", "klassenstufe", "klasse", "jg"],
            Looks: ImportValueParser.LooksLikeStufe),

        new ImportField(
            ImportFieldKeys.FloskelKlassengruppe, "Klassengruppe", Required: false,
            HeaderAliases: ["klassengruppe", "gruppe", "e/f", "eingangsstufe", "stufengruppe"],
            Looks: ImportValueParser.LooksLikeKlassengruppe),
    ]);
}
