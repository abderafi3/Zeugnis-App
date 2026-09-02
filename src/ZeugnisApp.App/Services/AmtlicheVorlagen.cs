using System.IO;
using System.Reflection;
using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.App.Services;

/// <summary>
/// Grundschule Wehringhausen's real, content-control-tagged report forms, bundled with the app as
/// embedded resources. <see cref="TemplateBootstrapper"/> extracts the matching file for each
/// standard <see cref="Formular"/> on first run and stores the hand-authored
/// <see cref="FieldMapping"/> — so generation works out of the box and "Vorlage hochladen" is only
/// needed to replace a form.
/// </summary>
internal static class AmtlicheVorlagen
{
    private const string ResourcePrefix = "ZeugnisApp.App.Templates.";

    /// <summary><see cref="Formular.Anzeigename"/> → embedded resource file name.</summary>
    private static readonly Dictionary<string, string> Dateien = new(StringComparer.Ordinal)
    {
        ["Schuleingangsphase – 1. Schulbesuchsjahr"] = "Zeugnis_1_Schulbesuchsjahr.docx",
        ["Schuleingangsphase – 2. Schulbesuchsjahr"] = "Zeugnis_2_Schulbesuchsjahr.docx",
        ["Klasse 3 – 1. Halbjahr"] = "Zeugnis_Klasse3_1Halbjahr.docx",
        ["Klasse 3 – Schuljahresende"] = "Zeugnis_Klasse3_2Halbjahr.docx",
        ["Klasse 4 – 1. Halbjahr"] = "Zeugnis_Klasse4_1Halbjahr_mit_Begruendung.docx",
        ["Klasse 4 – Schuljahresende"] = "Zeugnis_Klasse4_2Halbjahr_mit_Begruendung.docx",
        ["Zeugnis zum Gemeinsamen Lernen"] = "Zeugnis_Gemeinsames_Lernen.docx",
    };

    /// <summary>The Lern- und Förderempfehlung template (not tied to a <see cref="Formular"/>).</summary>
    public const string FoerderempfehlungDatei = "Foerderempfehlung.docx";

    public static bool Hat(string anzeigename) => Dateien.ContainsKey(anzeigename);

    public static string Dateiname(string anzeigename) => Dateien[anzeigename];

    public static Stream Oeffnen(string dateiname)
        => Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePrefix + dateiname)
           ?? throw new InvalidOperationException($"Eingebettete Vorlage '{dateiname}' fehlt.");

    /// <summary>
    /// The field mapping for a bundled form: the auto-generated one (which already produces the
    /// correct <c>Note_&lt;Fach&gt;</c> tags) with the metadata and free-text keys overridden to the
    /// tag names actually used in the school's <c>.docx</c>.
    /// </summary>
    public static FieldMapping Zuordnung(Formular formular)
    {
        var m = FieldMapping.Default(formular);

        void Map(string valueKey, string tag) => m.Tags[valueKey] = tag;

        Map(ValueKeys.KindName, "StudentName");
        Map(ValueKeys.KindGeburtsdatum, "Geburtsdatum");
        Map(ValueKeys.KlasseName, "Klasse");
        Map(ValueKeys.SchuleSchuljahr, "Schuljahr");
        Map(ValueKeys.KlasseZeugnisdatum, "Datum_Zeugnisausstellung");
        Map(ValueKeys.ZeugnisFehlstundenGesamt, "Fehlstunden_Gesamt");
        Map(ValueKeys.ZeugnisFehlstundenUnentschuldigt, "Fehlstunden_Unentschuldigt");
        Map(ValueKeys.ZeugnisBemerkungen, "Freitext_Bemerkungen");
        Map(ValueKeys.ZeugnisEmpfehlungBegruendung, "Freitext_BegruendungSchulformempfehlung");

        Map(ValueKeys.Freitext("Arbeits- und Sozialverhalten"), "Freitext_ArbeitsUndSozialverhalten");
        Map(ValueKeys.Freitext("Lernentwicklung (Teil 1)"), "Freitext_Lernentwicklung_Teil1");
        Map(ValueKeys.Freitext("Lernentwicklung (Teil 2)"), "Freitext_Lernentwicklung_Teil2");
        Map(ValueKeys.Freitext("Lernentwicklung"), "Freitext_Lernentwicklung");

        return m;
    }
}
