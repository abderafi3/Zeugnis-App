using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Resources;

/// <summary>Localized display text for domain enums, plus ready-made lists for ComboBox binding.</summary>
public static class EnumText
{
    public static string For(Geschlecht? value) => value switch
    {
        Geschlecht.Maennlich => Strings.Geschlecht_Maennlich,
        Geschlecht.Weiblich => Strings.Geschlecht_Weiblich,
        Geschlecht.Divers => Strings.Geschlecht_Divers,
        _ => Strings.Geschlecht_Unset,
    };

    public static string For(Klassengruppe value) => value switch
    {
        Klassengruppe.Eingangsstufe => Strings.Klassengruppe_E,
        Klassengruppe.Folgestufe => Strings.Klassengruppe_F,
        Klassengruppe.Beide => Strings.Klassengruppe_Beide,
        _ => value.ToString(),
    };

    public static IReadOnlyList<EnumOption<Klassengruppe>> KlassengruppeOptions { get; } =
    [
        new(Klassengruppe.Eingangsstufe, Strings.Klassengruppe_E),
        new(Klassengruppe.Folgestufe, Strings.Klassengruppe_F),
        new(Klassengruppe.Beide, Strings.Klassengruppe_Beide),
    ];

    public static string For(Foerderstatus value) => value switch
    {
        Foerderstatus.Regel => Strings.Foerderstatus_Regel,
        Foerderstatus.Zielgleich => Strings.Foerderstatus_Zielgleich,
        Foerderstatus.ZieldifferentLernen => Strings.Foerderstatus_ZieldifferentLernen,
        Foerderstatus.ZieldifferentGeistigeEntwicklung => Strings.Foerderstatus_ZieldifferentGE,
        _ => value.ToString(),
    };

    public static IReadOnlyList<EnumOption<Geschlecht?>> GeschlechtOptions { get; } =
    [
        new(null, Strings.Geschlecht_Unset),
        new(Geschlecht.Maennlich, Strings.Geschlecht_Maennlich),
        new(Geschlecht.Weiblich, Strings.Geschlecht_Weiblich),
        new(Geschlecht.Divers, Strings.Geschlecht_Divers),
    ];

    public static IReadOnlyList<EnumOption<Foerderstatus>> FoerderstatusOptions { get; } =
    [
        new(Foerderstatus.Regel, Strings.Foerderstatus_Regel),
        new(Foerderstatus.Zielgleich, Strings.Foerderstatus_Zielgleich),
        new(Foerderstatus.ZieldifferentLernen, Strings.Foerderstatus_ZieldifferentLernen),
        new(Foerderstatus.ZieldifferentGeistigeEntwicklung, Strings.Foerderstatus_ZieldifferentGE),
    ];
}

/// <summary>One selectable enum value with its display text.</summary>
public sealed record EnumOption<T>(T Value, string Text);
