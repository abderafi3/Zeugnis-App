namespace ZeugnisApp.Domain.Enums;

/// <summary>
/// Support status of a child. Drives automatic template suggestion (plan §5.1 / §Sprint 5)
/// and the legally-required special-support text for zieldifferente children (plan §Sprint 8).
/// </summary>
public enum Foerderstatus
{
    /// <summary>Regelschüler:in — no special educational support.</summary>
    Regel = 0,

    /// <summary>Sonderpädagogische Unterstützung, zielgleich unterrichtet.</summary>
    Zielgleich = 1,

    /// <summary>Zieldifferent, Förderschwerpunkt Lernen.</summary>
    ZieldifferentLernen = 2,

    /// <summary>Zieldifferent, Förderschwerpunkt Geistige Entwicklung.</summary>
    ZieldifferentGeistigeEntwicklung = 3,
}
