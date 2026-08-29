namespace ZeugnisApp.Domain.Enums;

/// <summary>
/// Grammatical gender used for phrase placeholder resolution (<c>[sein/ihr]</c>, plan §Sprint 4).
/// </summary>
public enum Geschlecht
{
    /// <summary>männlich — pronoun form "sein".</summary>
    Maennlich = 1,

    /// <summary>weiblich — pronoun form "ihr".</summary>
    Weiblich = 2,

    /// <summary>
    /// divers — no grammatical pronoun form. The placeholder engine falls back to repeating
    /// the child's name (behaviour configurable). Open question for the school (plan §4).
    /// </summary>
    Divers = 3,
}
