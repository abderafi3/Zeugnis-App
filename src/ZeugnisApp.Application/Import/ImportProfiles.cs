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

        // Mapped by header only — no content heuristic, so a leading row-number column
        // (values 1, 2, 3 …) is never mistaken for the Schulbesuchsjahr.
        new ImportField(
            ImportFieldKeys.Schulbesuchsjahr, "Schulbesuchsjahr", Required: false,
            HeaderAliases: ["schulbesuchsjahr", "sbj", "besuchsjahr"]),
    ]);

    /// <summary>
    /// Absence-totals import (plan §Sprint 7). Matches a class list export — SchILD's
    /// "Schüler-Abwesenheit – Zusammenfassung" PDF or an Excel sheet — onto the children already in
    /// the selected class. Only the name and the total missed hours are needed; the school's PDF has
    /// no excused/unexcused split, so "unentschuldigt" is optional and stays a manual entry.
    /// </summary>
    public static ImportProfile Fehlzeiten { get; } = new("Fehlzeiten",
    [
        new ImportField(
            ImportFieldKeys.FehlzeitName, "Name", Required: true,
            HeaderAliases: ["name", "schüler", "schueler", "schülerin", "schuelerin", "kind", "vorname nachname", "nachname vorname"],
            Looks: ImportValueParser.LooksLikeName),

        new ImportField(
            ImportFieldKeys.FehlzeitGesamt, "Fehlstunden gesamt", Required: true,
            HeaderAliases: ["abwesend", "abwese", "fehlstunden", "fehltage", "fehlzeiten", "versäumt", "versaeumt", "versäumnisse", "stunden", "gesamt"],
            Looks: ImportValueParser.LooksLikeCount),

        new ImportField(
            ImportFieldKeys.FehlzeitUnentschuldigt, "Davon unentschuldigt", Required: false,
            HeaderAliases: ["unentschuldigt", "unentsch", "unentschuldigte", "u"],
            Looks: ImportValueParser.LooksLikeCount),
    ]);

    /// <summary>
    /// Phrase-library import (plan §Sprint 4). Only <c>Text</c> is required; the school's real file
    /// carries <c>Kürzel · Floskeltext · Gruppe · Fach · Niveau · Jahrgang</c>, where Bereich is
    /// derived from <c>Gruppe</c> + <c>Fach</c> and Klassengruppe from <c>Jahrgang</c>.
    /// </summary>
    public static ImportProfile Floskeln { get; } = new("Floskeln",
    [
        new ImportField(
            ImportFieldKeys.FloskelText, "Text", Required: true,
            HeaderAliases: ["text", "floskeltext", "floskel", "formulierung", "baustein", "satz", "inhalt"]),

        new ImportField(
            ImportFieldKeys.FloskelKuerzel, "Kürzel", Required: false,
            HeaderAliases: ["kürzel", "kuerzel", "kennung", "kennziffer", "id", "nr", "nr.", "nummer", "code"]),

        new ImportField(
            ImportFieldKeys.FloskelBereich, "Bereich", Required: false,
            HeaderAliases: ["bereich", "kategorie", "thema", "category"]),

        new ImportField(
            ImportFieldKeys.FloskelGruppe, "Gruppe", Required: false,
            HeaderAliases: ["gruppe", "gruppierung", "art", "typ"]),

        new ImportField(
            ImportFieldKeys.FloskelFach, "Fach", Required: false,
            HeaderAliases: ["fach", "unterrichtsfach", "lernbereich", "fachbereich"]),

        new ImportField(
            ImportFieldKeys.FloskelNiveau, "Niveau", Required: false,
            HeaderAliases: ["niveau", "level", "differenzierung", "anforderungsniveau"],
            Looks: ImportValueParser.LooksLikeNiveau),

        new ImportField(
            ImportFieldKeys.FloskelStufe, "Stufe / Jahrgang", Required: false,
            HeaderAliases: ["stufe", "jahrgang", "klassenstufe", "klasse", "jg", "schulbesuchsjahr", "sbj"],
            Looks: ImportValueParser.LooksLikeStufe),

        new ImportField(
            ImportFieldKeys.FloskelKlassengruppe, "Klassengruppe", Required: false,
            HeaderAliases: ["klassengruppe", "e/f", "eingangsstufe", "stufengruppe", "band"],
            Looks: ImportValueParser.LooksLikeKlassengruppe),
    ]);
}
