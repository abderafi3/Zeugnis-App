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
    public static string Nav_Papierkorb => Get(nameof(Nav_Papierkorb));
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

    // ---- Import-Assistent ----
    public static string Import_Title => Get(nameof(Import_Title));
    public static string Import_Step1_Heading => Get(nameof(Import_Step1_Heading));
    public static string Import_Step2_Heading => Get(nameof(Import_Step2_Heading));
    public static string Import_Step3_Heading => Get(nameof(Import_Step3_Heading));
    public static string Import_Step1_Intro => Get(nameof(Import_Step1_Intro));
    public static string Import_FromExcel => Get(nameof(Import_FromExcel));
    public static string Import_FromWord => Get(nameof(Import_FromWord));
    public static string Import_FromCsv => Get(nameof(Import_FromCsv));
    public static string Import_FromClipboard => Get(nameof(Import_FromClipboard));
    public static string Import_DropHint => Get(nameof(Import_DropHint));
    public static string Import_Error_Empty => Get(nameof(Import_Error_Empty));
    public static string Import_Error_Read => Get(nameof(Import_Error_Read));
    public static string Import_HasHeaderRow => Get(nameof(Import_HasHeaderRow));
    public static string Import_Role_Ignore => Get(nameof(Import_Role_Ignore));
    public static string Import_MappingOk => Get(nameof(Import_MappingOk));
    public static string Import_MappingMissing => Get(nameof(Import_MappingMissing));
    public static string Import_Next => Get(nameof(Import_Next));
    public static string Import_PreviewSummary => Get(nameof(Import_PreviewSummary));
    public static string Import_Col_Include => Get(nameof(Import_Col_Include));
    public static string Import_Col_SourceRow => Get(nameof(Import_Col_SourceRow));
    public static string Import_Col_Errors => Get(nameof(Import_Col_Errors));
    public static string Import_DoImport => Get(nameof(Import_DoImport));
    public static string Import_Done => Get(nameof(Import_Done));
    public static string Import_ResultSummary => Get(nameof(Import_ResultSummary));

    // ---- Floskeln ----
    public static string Floskeln_Title => Get(nameof(Floskeln_Title));
    public static string Floskeln_Intro => Get(nameof(Floskeln_Intro));
    public static string Floskeln_New => Get(nameof(Floskeln_New));
    public static string Floskeln_Import => Get(nameof(Floskeln_Import));
    public static string Floskeln_TestPicker => Get(nameof(Floskeln_TestPicker));
    public static string Floskeln_Empty => Get(nameof(Floskeln_Empty));
    public static string Floskeln_Filter_Bereich => Get(nameof(Floskeln_Filter_Bereich));
    public static string Floskeln_Filter_Stufe => Get(nameof(Floskeln_Filter_Stufe));
    public static string Floskeln_Filter_Gruppe => Get(nameof(Floskeln_Filter_Gruppe));
    public static string Floskeln_Filter_Search => Get(nameof(Floskeln_Filter_Search));
    public static string Floskeln_Filter_All => Get(nameof(Floskeln_Filter_All));
    public static string Floskeln_Col_Text => Get(nameof(Floskeln_Col_Text));
    public static string Floskeln_Col_Bereich => Get(nameof(Floskeln_Col_Bereich));
    public static string Floskeln_Col_Stufe => Get(nameof(Floskeln_Col_Stufe));
    public static string Floskeln_Col_Gruppe => Get(nameof(Floskeln_Col_Gruppe));
    public static string Floskeln_DeleteConfirm_Title => Get(nameof(Floskeln_DeleteConfirm_Title));
    public static string Floskeln_DeleteConfirm_Body => Get(nameof(Floskeln_DeleteConfirm_Body));
    public static string Floskeln_PickerResult => Get(nameof(Floskeln_PickerResult));
    public static string Klassengruppe_E => Get(nameof(Klassengruppe_E));
    public static string Klassengruppe_F => Get(nameof(Klassengruppe_F));
    public static string Klassengruppe_Beide => Get(nameof(Klassengruppe_Beide));

    // ---- Floskel-Auswahl (Picker) ----
    public static string Picker_Title => Get(nameof(Picker_Title));
    public static string Picker_SampleName => Get(nameof(Picker_SampleName));
    public static string Picker_SampleGeschlecht => Get(nameof(Picker_SampleGeschlecht));
    public static string Picker_Preview => Get(nameof(Picker_Preview));
    public static string Picker_Insert => Get(nameof(Picker_Insert));

    // ---- Formulare ----
    public static string Formulare_Title => Get(nameof(Formulare_Title));
    public static string Formulare_Intro => Get(nameof(Formulare_Intro));
    public static string Formulare_New => Get(nameof(Formulare_New));
    public static string Formulare_Empty => Get(nameof(Formulare_Empty));
    public static string Formulare_StatusReady => Get(nameof(Formulare_StatusReady));
    public static string Formulare_StatusMissing => Get(nameof(Formulare_StatusMissing));
    public static string Formulare_JahrgangHalbjahr => Get(nameof(Formulare_JahrgangHalbjahr));
    public static string Formulare_HalbjahrBoth => Get(nameof(Formulare_HalbjahrBoth));
    public static string Formulare_DeleteConfirm_Title => Get(nameof(Formulare_DeleteConfirm_Title));
    public static string Formulare_DeleteConfirm_Body => Get(nameof(Formulare_DeleteConfirm_Body));
    public static string FormularEdit_TitleNew => Get(nameof(FormularEdit_TitleNew));
    public static string FormularEdit_TitleEdit => Get(nameof(FormularEdit_TitleEdit));
    public static string FormularEdit_Anzeigename => Get(nameof(FormularEdit_Anzeigename));
    public static string FormularEdit_Ueberschrift => Get(nameof(FormularEdit_Ueberschrift));
    public static string FormularEdit_Jahrgang => Get(nameof(FormularEdit_Jahrgang));
    public static string FormularEdit_Halbjahr => Get(nameof(FormularEdit_Halbjahr));
    public static string FormularEdit_Schalter => Get(nameof(FormularEdit_Schalter));
    public static string FormularEdit_Notenblock => Get(nameof(FormularEdit_Notenblock));
    public static string FormularEdit_Empfehlung => Get(nameof(FormularEdit_Empfehlung));
    public static string FormularEdit_Versetzung => Get(nameof(FormularEdit_Versetzung));
    public static string FormularEdit_Zieldifferent => Get(nameof(FormularEdit_Zieldifferent));
    public static string FormularEdit_Faecher => Get(nameof(FormularEdit_Faecher));
    public static string FormularEdit_Freitextfelder => Get(nameof(FormularEdit_Freitextfelder));
    public static string FormularEdit_Feld_Name => Get(nameof(FormularEdit_Feld_Name));
    public static string FormularEdit_Feld_Hinweis => Get(nameof(FormularEdit_Feld_Hinweis));
    public static string FormularEdit_Feld_MaxZeichen => Get(nameof(FormularEdit_Feld_MaxZeichen));
    public static string FormularEdit_Feld_Add => Get(nameof(FormularEdit_Feld_Add));
    public static string FormularEdit_WordFile => Get(nameof(FormularEdit_WordFile));
    public static string FormularEdit_WordFile_None => Get(nameof(FormularEdit_WordFile_None));
    public static string FormularEdit_WordFile_Current => Get(nameof(FormularEdit_WordFile_Current));
    public static string FormularEdit_WordFile_Upload => Get(nameof(FormularEdit_WordFile_Upload));
    public static string Zuordnung_Title => Get(nameof(Zuordnung_Title));
    public static string Zuordnung_Intro => Get(nameof(Zuordnung_Intro));
    public static string Zuordnung_Hint_NoControls => Get(nameof(Zuordnung_Hint_NoControls));
    public static string Zuordnung_Column_Feld => Get(nameof(Zuordnung_Column_Feld));
    public static string Zuordnung_Column_Steuerelement => Get(nameof(Zuordnung_Column_Steuerelement));
    public static string Zuordnung_None => Get(nameof(Zuordnung_None));
    public static string Zuordnung_Group_Schule => Get(nameof(Zuordnung_Group_Schule));
    public static string Zuordnung_Group_Klasse => Get(nameof(Zuordnung_Group_Klasse));
    public static string Zuordnung_Group_Kind => Get(nameof(Zuordnung_Group_Kind));
    public static string Zuordnung_Group_Zeugnis => Get(nameof(Zuordnung_Group_Zeugnis));
    public static string Zuordnung_Group_Noten => Get(nameof(Zuordnung_Group_Noten));
    public static string Zuordnung_Group_Freitexte => Get(nameof(Zuordnung_Group_Freitexte));
    public static string Zuordnung_Missing => Get(nameof(Zuordnung_Missing));
    public static string Zuordnung_Apply => Get(nameof(Zuordnung_Apply));
    public static string Zuordnung_Error_Invalid => Get(nameof(Zuordnung_Error_Invalid));
    public static string Feld_Schule_Name => Get(nameof(Feld_Schule_Name));
    public static string Feld_Schule_Zusatz => Get(nameof(Feld_Schule_Zusatz));
    public static string Feld_Schule_Adresse => Get(nameof(Feld_Schule_Adresse));
    public static string Feld_Schule_Leitung => Get(nameof(Feld_Schule_Leitung));
    public static string Feld_Schule_Schuljahr => Get(nameof(Feld_Schule_Schuljahr));
    public static string Feld_Schule_Halbjahr => Get(nameof(Feld_Schule_Halbjahr));
    public static string Feld_Klasse_Name => Get(nameof(Feld_Klasse_Name));
    public static string Feld_Klasse_Leitung => Get(nameof(Feld_Klasse_Leitung));
    public static string Feld_Klasse_Zeugnisdatum => Get(nameof(Feld_Klasse_Zeugnisdatum));
    public static string Feld_Kind_Vorname => Get(nameof(Feld_Kind_Vorname));
    public static string Feld_Kind_Nachname => Get(nameof(Feld_Kind_Nachname));
    public static string Feld_Kind_Name => Get(nameof(Feld_Kind_Name));
    public static string Feld_Kind_Geburtsdatum => Get(nameof(Feld_Kind_Geburtsdatum));
    public static string Feld_Zeugnis_Empfehlung => Get(nameof(Feld_Zeugnis_Empfehlung));
    public static string Feld_Zeugnis_Begruendung => Get(nameof(Feld_Zeugnis_Begruendung));
    public static string Feld_Zeugnis_Versetzung => Get(nameof(Feld_Zeugnis_Versetzung));
    public static string Feld_Zeugnis_Bemerkungen => Get(nameof(Feld_Zeugnis_Bemerkungen));
    public static string Feld_Zeugnis_FehlGesamt => Get(nameof(Feld_Zeugnis_FehlGesamt));
    public static string Feld_Zeugnis_FehlUnentschuldigt => Get(nameof(Feld_Zeugnis_FehlUnentschuldigt));
    public static string FormularEdit_Error_NameRequired => Get(nameof(FormularEdit_Error_NameRequired));
    public static string FormularEdit_Error_FeldNamesUnique => Get(nameof(FormularEdit_Error_FeldNamesUnique));

    // ---- Fehlzeiten ----
    public static string Fehlzeiten_Title => Get(nameof(Fehlzeiten_Title));
    public static string Fehlzeiten_Intro => Get(nameof(Fehlzeiten_Intro));
    public static string Fehlzeiten_Klasse => Get(nameof(Fehlzeiten_Klasse));
    public static string Fehlzeiten_Term => Get(nameof(Fehlzeiten_Term));
    public static string Fehlzeiten_Empty => Get(nameof(Fehlzeiten_Empty));
    public static string Fehlzeiten_NoKlasse => Get(nameof(Fehlzeiten_NoKlasse));
    public static string Fehlzeiten_Col_Name => Get(nameof(Fehlzeiten_Col_Name));
    public static string Fehlzeiten_Col_Gesamt => Get(nameof(Fehlzeiten_Col_Gesamt));
    public static string Fehlzeiten_Col_Unentschuldigt => Get(nameof(Fehlzeiten_Col_Unentschuldigt));
    public static string Fehlzeiten_Col_Hinweis => Get(nameof(Fehlzeiten_Col_Hinweis));
    public static string Fehlzeiten_Error => Get(nameof(Fehlzeiten_Error));

    // ---- Zeugnisse ----
    public static string Zeugnisse_Title => Get(nameof(Zeugnisse_Title));
    public static string Zeugnisse_Intro => Get(nameof(Zeugnisse_Intro));
    public static string Zeugnisse_Klasse => Get(nameof(Zeugnisse_Klasse));
    public static string Zeugnisse_Term => Get(nameof(Zeugnisse_Term));
    public static string Zeugnisse_NoKlasse => Get(nameof(Zeugnisse_NoKlasse));
    public static string Zeugnisse_Empty => Get(nameof(Zeugnisse_Empty));
    public static string Zeugnisse_Col_Name => Get(nameof(Zeugnisse_Col_Name));
    public static string Zeugnisse_Col_Formular => Get(nameof(Zeugnisse_Col_Formular));
    public static string Zeugnisse_Col_Status => Get(nameof(Zeugnisse_Col_Status));
    public static string Zeugnisse_Status_Entwurf => Get(nameof(Zeugnisse_Status_Entwurf));
    public static string Zeugnisse_Status_Fertig => Get(nameof(Zeugnisse_Status_Fertig));
    public static string Zeugnisse_Status_KeinFormular => Get(nameof(Zeugnisse_Status_KeinFormular));
    public static string Zeugnisse_Open => Get(nameof(Zeugnisse_Open));
    public static string Zeugnisse_MarkDone => Get(nameof(Zeugnisse_MarkDone));
    public static string Zeugnisse_Reopen => Get(nameof(Zeugnisse_Reopen));
    public static string ZeugnisEditor_Title => Get(nameof(ZeugnisEditor_Title));
    public static string ZeugnisEditor_FormularLabel => Get(nameof(ZeugnisEditor_FormularLabel));
    public static string ZeugnisEditor_Section_Freitexte => Get(nameof(ZeugnisEditor_Section_Freitexte));
    public static string ZeugnisEditor_Section_Noten => Get(nameof(ZeugnisEditor_Section_Noten));
    public static string ZeugnisEditor_Section_Empfehlung => Get(nameof(ZeugnisEditor_Section_Empfehlung));
    public static string ZeugnisEditor_Section_Versetzung => Get(nameof(ZeugnisEditor_Section_Versetzung));
    public static string ZeugnisEditor_Section_Fehlstunden => Get(nameof(ZeugnisEditor_Section_Fehlstunden));
    public static string ZeugnisEditor_Section_Bemerkungen => Get(nameof(ZeugnisEditor_Section_Bemerkungen));
    public static string ZeugnisEditor_Empfehlung => Get(nameof(ZeugnisEditor_Empfehlung));
    public static string ZeugnisEditor_Begruendung => Get(nameof(ZeugnisEditor_Begruendung));
    public static string ZeugnisEditor_Fehlstunden_Gesamt => Get(nameof(ZeugnisEditor_Fehlstunden_Gesamt));
    public static string ZeugnisEditor_Fehlstunden_Unentschuldigt => Get(nameof(ZeugnisEditor_Fehlstunden_Unentschuldigt));
    public static string ZeugnisEditor_InsertFloskel => Get(nameof(ZeugnisEditor_InsertFloskel));
    public static string ZeugnisEditor_CharCount => Get(nameof(ZeugnisEditor_CharCount));
    public static string ZeugnisEditor_CharCountNoLimit => Get(nameof(ZeugnisEditor_CharCountNoLimit));
    public static string ZeugnisEditor_Overflow => Get(nameof(ZeugnisEditor_Overflow));
    public static string ZeugnisEditor_Fertiggestellt => Get(nameof(ZeugnisEditor_Fertiggestellt));
    public static string ZeugnisEditor_ZieldifferentAdded => Get(nameof(ZeugnisEditor_ZieldifferentAdded));
    public static string ZeugnisEditor_Erzeugen => Get(nameof(ZeugnisEditor_Erzeugen));
    public static string ZeugnisEditor_Erzeugen_Running => Get(nameof(ZeugnisEditor_Erzeugen_Running));
    public static string ZeugnisEditor_Erzeugen_Ok => Get(nameof(ZeugnisEditor_Erzeugen_Ok));
    public static string ZeugnisEditor_Erzeugen_OkOverflow => Get(nameof(ZeugnisEditor_Erzeugen_OkOverflow));
    public static string ZeugnisEditor_Erzeugen_Dialog_Title => Get(nameof(ZeugnisEditor_Erzeugen_Dialog_Title));
    public static string ZeugnisEditor_Erzeugen_NoTemplate => Get(nameof(ZeugnisEditor_Erzeugen_NoTemplate));
    public static string ZeugnisEditor_LastGenerated => Get(nameof(ZeugnisEditor_LastGenerated));

    // ---- Recovery ----
    public static string Recovery_Title => Get(nameof(Recovery_Title));
    public static string Recovery_ShowIntro => Get(nameof(Recovery_ShowIntro));
    public static string Recovery_EnterIntro => Get(nameof(Recovery_EnterIntro));
    public static string Recovery_Copy => Get(nameof(Recovery_Copy));
    public static string Recovery_Continue => Get(nameof(Recovery_Continue));
    public static string Recovery_Unlock => Get(nameof(Recovery_Unlock));
    public static string Recovery_Empty => Get(nameof(Recovery_Empty));

    // ---- Papierkorb ----
    public static string Papierkorb_Title => Get(nameof(Papierkorb_Title));
    public static string Papierkorb_Intro => Get(nameof(Papierkorb_Intro));
    public static string Papierkorb_Empty => Get(nameof(Papierkorb_Empty));
    public static string Papierkorb_Col_Typ => Get(nameof(Papierkorb_Col_Typ));
    public static string Papierkorb_Col_Bezeichnung => Get(nameof(Papierkorb_Col_Bezeichnung));
    public static string Papierkorb_Col_Datum => Get(nameof(Papierkorb_Col_Datum));
    public static string Papierkorb_Restore => Get(nameof(Papierkorb_Restore));

    // ---- Einstellungen: Sicherung ----
    public static string Settings_Backup_Section => Get(nameof(Settings_Backup_Section));
    public static string Settings_Backup_Intro => Get(nameof(Settings_Backup_Intro));
    public static string Settings_Backup_Create => Get(nameof(Settings_Backup_Create));
    public static string Settings_Backup_Restore => Get(nameof(Settings_Backup_Restore));
    public static string Settings_Backup_Ok => Get(nameof(Settings_Backup_Ok));
    public static string Settings_Backup_RestoreOk => Get(nameof(Settings_Backup_RestoreOk));
    public static string Settings_Backup_Error => Get(nameof(Settings_Backup_Error));
    public static string Settings_Backup_Filter => Get(nameof(Settings_Backup_Filter));
    public static string Settings_Backup_DefaultName => Get(nameof(Settings_Backup_DefaultName));

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
