namespace ZeugnisApp.Domain.Enums;

/// <summary>
/// Which year-group band a phrase (Floskel) applies to (plan §4).
/// E = Schuleingangsphase (Jahrgänge 1–2), F = Jahrgänge 3–4.
/// </summary>
public enum Klassengruppe
{
    Eingangsstufe = 1,
    Folgestufe = 2,
    Beide = 3,
}
