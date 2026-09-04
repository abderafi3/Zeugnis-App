using System.Globalization;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Reporting;

/// <summary>
/// Turns a <see cref="ReportRenderRequest"/> into the flat value-key → text map the engine fills
/// into content controls (plan §5.2). Pure, so it is unit-tested without touching OOXML.
/// </summary>
public static class ReportValues
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static IReadOnlyDictionary<string, string> Build(ReportRenderRequest r)
    {
        var v = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void Put(string key, string? value) => v[key] = value?.Trim() ?? string.Empty;

        Put(ValueKeys.SchuleName, r.Schule.Name);
        Put(ValueKeys.SchuleZusatz, r.Schule.Zusatzbezeichnung);
        Put(ValueKeys.SchuleAdresse, r.Schule.Adresse);
        Put(ValueKeys.SchuleLeitung, r.Schule.Schulleitung);
        // Falls back to the school-wide Schuljahr when the report was created before it was set.
        Put(ValueKeys.SchuleSchuljahr,
            string.IsNullOrWhiteSpace(r.Zeugnis.Schuljahr) ? r.Schule.Schuljahr : r.Zeugnis.Schuljahr);
        Put(ValueKeys.SchuleHalbjahr, r.Zeugnis.Halbjahr == Halbjahr.Erstes ? "1. Halbjahr" : "2. Halbjahr");
        Put(ValueKeys.SchuleKonferenzdatum, FormatDate(r.Schule.Konferenzdatum));
        Put(ValueKeys.SchuleWiederbeginn, FormatDate(r.Schule.DatumUnterrichtsWiederbeginn));

        Put(ValueKeys.KlasseName, r.Klasse.Name);
        Put(ValueKeys.KlasseLeitung, r.Klasse.Klassenleitung);
        Put(ValueKeys.KlasseZeugnisdatum, FormatDate(r.Klasse.Zeugnisdatum ?? r.Schule.Zeugnisausgabedatum));

        Put(ValueKeys.KindVorname, r.Kind.Vorname);
        Put(ValueKeys.KindNachname, r.Kind.Nachname);
        Put(ValueKeys.KindName, r.Kind.AnzeigeName);
        Put(ValueKeys.KindGeburtsdatum, FormatDate(r.Kind.Geburtsdatum));

        Put(ValueKeys.ZeugnisEmpfehlung, r.Zeugnis.Empfehlung);
        Put(ValueKeys.ZeugnisEmpfehlungBegruendung, r.Zeugnis.EmpfehlungBegruendung);
        Put(ValueKeys.ZeugnisVersetzungsvermerk, r.Zeugnis.VersetzungsvermerkText);

        // Grundschule Wehringhausen's year-end forms have no dedicated Versetzungsvermerk control —
        // the decision belongs in the Bemerkungen area. Fold it in so it is never dropped; a
        // template that maps zeugnis.versetzungsvermerk separately still gets it there too.
        Put(ValueKeys.ZeugnisBemerkungen, JoinAbsatz(r.Zeugnis.Bemerkungen, r.Zeugnis.VersetzungsvermerkText));
        Put(ValueKeys.ZeugnisFehlstundenGesamt, r.Zeugnis.FehlstundenGesamt?.ToString(De));
        Put(ValueKeys.ZeugnisFehlstundenUnentschuldigt, r.Zeugnis.FehlstundenUnentschuldigt?.ToString(De));

        foreach (var note in r.Zeugnis.Noten)
        {
            Put(ValueKeys.Note(note.Fach), note.Note);
        }

        foreach (var feld in r.Zeugnis.Freitexte)
        {
            Put(ValueKeys.Freitext(feld.FeldName), feld.Text);
        }

        return v;
    }

    private static string FormatDate(DateOnly? date)
        => date is { } d ? d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;

    /// <summary>Joins two optional text blocks into one, separated by a blank line; skips empties.</summary>
    private static string JoinAbsatz(string? first, string? second)
        => string.Join(
            "\n\n",
            new[] { first, second }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
}
