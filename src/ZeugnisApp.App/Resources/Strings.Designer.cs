//------------------------------------------------------------------------------
// Strongly-typed accessor for Resources/Strings.resx.
// Hand-maintained (not VS-generated) so it works in plain `dotnet build`.
// German is the neutral culture; add Strings.<culture>.resx satellites to localise.
//------------------------------------------------------------------------------
#nullable enable

using System.Globalization;
using System.Resources;

namespace ZeugnisApp.App.Resources;

public static class Strings
{
    private static readonly ResourceManager Rm =
        new("ZeugnisApp.App.Resources.Strings", typeof(Strings).Assembly);

    /// <summary>Overrides the culture used for lookups. Null = current UI culture.</summary>
    public static CultureInfo? Culture { get; set; }

    public static string Get(string name) => Rm.GetString(name, Culture) ?? name;

    // ---- Shell ----
    public static string App_Title => Get(nameof(App_Title));
    public static string Nav_Start => Get(nameof(Nav_Start));
    public static string Nav_Klassen => Get(nameof(Nav_Klassen));
    public static string Nav_Zeugnisse => Get(nameof(Nav_Zeugnisse));
    public static string Nav_Formulare => Get(nameof(Nav_Formulare));
    public static string Nav_Floskeln => Get(nameof(Nav_Floskeln));
    public static string Nav_Fehlzeiten => Get(nameof(Nav_Fehlzeiten));
    public static string Nav_Einstellungen => Get(nameof(Nav_Einstellungen));
    public static string Save_Active => Get(nameof(Save_Active));
    public static string Save_Unsaved => Get(nameof(Save_Unsaved));
    public static string Save_At => Get(nameof(Save_At));
    public static string Common_Save => Get(nameof(Common_Save));
    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Delete => Get(nameof(Common_Delete));
    public static string Common_Back => Get(nameof(Common_Back));
    public static string Common_Yes => Get(nameof(Common_Yes));
    public static string Common_No => Get(nameof(Common_No));

    // ---- Halbjahr ----
    public static string Halbjahr_First => Get(nameof(Halbjahr_First));
    public static string Halbjahr_Second => Get(nameof(Halbjahr_Second));

    // ---- Dashboard ----
    public static string Dashboard_SchoolNotConfigured => Get(nameof(Dashboard_SchoolNotConfigured));
    public static string Dashboard_Areas => Get(nameof(Dashboard_Areas));
    public static string Dashboard_SchoolYear => Get(nameof(Dashboard_SchoolYear));
    public static string Tile_Klassen => Get(nameof(Tile_Klassen));
    public static string Tile_Klassen_Desc => Get(nameof(Tile_Klassen_Desc));
    public static string Tile_Klassen_Count => Get(nameof(Tile_Klassen_Count));
    public static string Tile_Zeugnisse => Get(nameof(Tile_Zeugnisse));
    public static string Tile_Zeugnisse_Desc => Get(nameof(Tile_Zeugnisse_Desc));
    public static string Tile_Zeugnisse_Count => Get(nameof(Tile_Zeugnisse_Count));
    public static string Tile_Formulare => Get(nameof(Tile_Formulare));
    public static string Tile_Formulare_Desc => Get(nameof(Tile_Formulare_Desc));
    public static string Tile_Formulare_Count => Get(nameof(Tile_Formulare_Count));
    public static string Tile_Floskeln => Get(nameof(Tile_Floskeln));
    public static string Tile_Floskeln_Desc => Get(nameof(Tile_Floskeln_Desc));
    public static string Tile_Floskeln_Count => Get(nameof(Tile_Floskeln_Count));
    public static string Tile_Fehlzeiten => Get(nameof(Tile_Fehlzeiten));
    public static string Tile_Fehlzeiten_Desc => Get(nameof(Tile_Fehlzeiten_Desc));
    public static string Tile_Fehlzeiten_Count => Get(nameof(Tile_Fehlzeiten_Count));

    // ---- Placeholder ----
    public static string Placeholder_NotAvailable => Get(nameof(Placeholder_NotAvailable));
    public static string Placeholder_Zeugnisse => Get(nameof(Placeholder_Zeugnisse));
    public static string Placeholder_Formulare => Get(nameof(Placeholder_Formulare));
    public static string Placeholder_Floskeln => Get(nameof(Placeholder_Floskeln));
    public static string Placeholder_Fehlzeiten => Get(nameof(Placeholder_Fehlzeiten));

    // ---- Einstellungen ----
    public static string Settings_Title => Get(nameof(Settings_Title));
    public static string Settings_Intro => Get(nameof(Settings_Intro));
    public static string Settings_SchoolName => Get(nameof(Settings_SchoolName));
    public static string Settings_SchoolName_Ph => Get(nameof(Settings_SchoolName_Ph));
    public static string Settings_Zusatz => Get(nameof(Settings_Zusatz));
    public static string Settings_Zusatz_Ph => Get(nameof(Settings_Zusatz_Ph));
    public static string Settings_Address => Get(nameof(Settings_Address));
    public static string Settings_Leadership => Get(nameof(Settings_Leadership));
    public static string Settings_Leadership_Ph => Get(nameof(Settings_Leadership_Ph));
    public static string Settings_SchoolYear => Get(nameof(Settings_SchoolYear));
    public static string Settings_SchoolYear_Ph => Get(nameof(Settings_SchoolYear_Ph));
    public static string Settings_CurrentTerm => Get(nameof(Settings_CurrentTerm));
    public static string Settings_IssueDate => Get(nameof(Settings_IssueDate));
    public static string Settings_ConferenceDate => Get(nameof(Settings_ConferenceDate));
    public static string Settings_ResumeDate => Get(nameof(Settings_ResumeDate));
    public static string Settings_RecycleBinDays => Get(nameof(Settings_RecycleBinDays));

    // ---- Klassen ----
    public static string Klassen_Title => Get(nameof(Klassen_Title));
    public static string Klassen_Intro => Get(nameof(Klassen_Intro));
    public static string Klassen_New => Get(nameof(Klassen_New));
    public static string Klassen_Empty => Get(nameof(Klassen_Empty));
    public static string Klassen_OpenChildren => Get(nameof(Klassen_OpenChildren));
    public static string Klassen_ChildCount => Get(nameof(Klassen_ChildCount));
    public static string Klassen_JahrgangBadge => Get(nameof(Klassen_JahrgangBadge));
    public static string Klassen_DeleteConfirm_Title => Get(nameof(Klassen_DeleteConfirm_Title));
    public static string Klassen_DeleteConfirm_Body => Get(nameof(Klassen_DeleteConfirm_Body));

    public static string KlasseEdit_TitleNew => Get(nameof(KlasseEdit_TitleNew));
    public static string KlasseEdit_TitleEdit => Get(nameof(KlasseEdit_TitleEdit));
    public static string KlasseEdit_Name => Get(nameof(KlasseEdit_Name));
    public static string KlasseEdit_Name_Ph => Get(nameof(KlasseEdit_Name_Ph));
    public static string KlasseEdit_Jahrgang => Get(nameof(KlasseEdit_Jahrgang));
    public static string KlasseEdit_Klassenleitung => Get(nameof(KlasseEdit_Klassenleitung));
    public static string KlasseEdit_Zeugnisdatum => Get(nameof(KlasseEdit_Zeugnisdatum));
    public static string KlasseEdit_Wochenstunden => Get(nameof(KlasseEdit_Wochenstunden));
    public static string KlasseEdit_Mo => Get(nameof(KlasseEdit_Mo));
    public static string KlasseEdit_Di => Get(nameof(KlasseEdit_Di));
    public static string KlasseEdit_Mi => Get(nameof(KlasseEdit_Mi));
    public static string KlasseEdit_Do => Get(nameof(KlasseEdit_Do));
    public static string KlasseEdit_Fr => Get(nameof(KlasseEdit_Fr));
    public static string KlasseEdit_Error_NameRequired => Get(nameof(KlasseEdit_Error_NameRequired));
    public static string KlasseEdit_Error_Jahrgang => Get(nameof(KlasseEdit_Error_Jahrgang));

    // ---- Kinder ----
    public static string Kinder_Title => Get(nameof(Kinder_Title));
    public static string Kinder_New => Get(nameof(Kinder_New));
    public static string Kinder_Import => Get(nameof(Kinder_Import));
    public static string Kinder_Empty => Get(nameof(Kinder_Empty));
    public static string Kinder_Col_Name => Get(nameof(Kinder_Col_Name));
    public static string Kinder_Col_Geburtsdatum => Get(nameof(Kinder_Col_Geburtsdatum));
    public static string Kinder_Col_Geschlecht => Get(nameof(Kinder_Col_Geschlecht));
    public static string Kinder_Col_Foerderstatus => Get(nameof(Kinder_Col_Foerderstatus));
    public static string Kinder_Col_Status => Get(nameof(Kinder_Col_Status));
    public static string Kinder_Incomplete => Get(nameof(Kinder_Incomplete));
    public static string Kinder_Complete => Get(nameof(Kinder_Complete));
    public static string Kinder_DeleteConfirm_Title => Get(nameof(Kinder_DeleteConfirm_Title));
    public static string Kinder_DeleteConfirm_Body => Get(nameof(Kinder_DeleteConfirm_Body));

    public static string KindEdit_TitleNew => Get(nameof(KindEdit_TitleNew));
    public static string KindEdit_TitleEdit => Get(nameof(KindEdit_TitleEdit));
    public static string KindEdit_Vorname => Get(nameof(KindEdit_Vorname));
    public static string KindEdit_Nachname => Get(nameof(KindEdit_Nachname));
    public static string KindEdit_Geburtsdatum => Get(nameof(KindEdit_Geburtsdatum));
    public static string KindEdit_Geschlecht => Get(nameof(KindEdit_Geschlecht));
    public static string KindEdit_Foerderstatus => Get(nameof(KindEdit_Foerderstatus));
    public static string KindEdit_Foerderschwerpunkt => Get(nameof(KindEdit_Foerderschwerpunkt));
    public static string KindEdit_Foerderschwerpunkt_Ph => Get(nameof(KindEdit_Foerderschwerpunkt_Ph));
    public static string KindEdit_ManuellesFormular => Get(nameof(KindEdit_ManuellesFormular));
    public static string KindEdit_ManuellesFormular_None => Get(nameof(KindEdit_ManuellesFormular_None));
    public static string KindEdit_Error_VornameRequired => Get(nameof(KindEdit_Error_VornameRequired));
    public static string KindEdit_Error_NachnameRequired => Get(nameof(KindEdit_Error_NachnameRequired));

    // ---- Enums ----
    public static string Geschlecht_Maennlich => Get(nameof(Geschlecht_Maennlich));
    public static string Geschlecht_Weiblich => Get(nameof(Geschlecht_Weiblich));
    public static string Geschlecht_Divers => Get(nameof(Geschlecht_Divers));
    public static string Geschlecht_Unset => Get(nameof(Geschlecht_Unset));
    public static string Foerderstatus_Regel => Get(nameof(Foerderstatus_Regel));
    public static string Foerderstatus_Zielgleich => Get(nameof(Foerderstatus_Zielgleich));
    public static string Foerderstatus_ZieldifferentLernen => Get(nameof(Foerderstatus_ZieldifferentLernen));
    public static string Foerderstatus_ZieldifferentGE => Get(nameof(Foerderstatus_ZieldifferentGE));
}
