using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Formulare;

/// <summary>Why a particular template was proposed for a child.</summary>
public enum SuggestionReason
{
    /// <summary>The teacher pinned a template on the child (<see cref="Kind.ManuellesFormularId"/>).</summary>
    ManuellFestgelegt,

    /// <summary>Matched by year group + term (+ zieldifferent flag).</summary>
    AutomatischNachJahrgang,

    /// <summary>No template fits — nothing to generate from yet.</summary>
    KeinPassendesFormular,
}

/// <summary>Outcome of template auto-suggestion (plan §5.1 / §Sprint 5).</summary>
public sealed record FormularSuggestion(Formular? Formular, SuggestionReason Reason)
{
    public bool IstManuell => Reason == SuggestionReason.ManuellFestgelegt;
}

/// <summary>
/// Picks the report template that fits a child (plan §Sprint 5). Pure so it is unit-tested directly;
/// the infrastructure service just supplies the loaded entities.
/// </summary>
public static class FormularSuggester
{
    public static FormularSuggestion Suggest(
        IReadOnlyCollection<Formular> alleFormulare,
        Kind kind,
        int jahrgang,
        Halbjahr halbjahr)
    {
        // 1. Manual override always wins (plan §4).
        if (kind.ManuellesFormularId is { } manualId)
        {
            var manual = alleFormulare.FirstOrDefault(f => f.Id == manualId);
            if (manual is not null)
            {
                return new FormularSuggestion(manual, SuggestionReason.ManuellFestgelegt);
            }
        }

        var wantsZieldifferent = kind.Foerderstatus is
            Foerderstatus.ZieldifferentLernen or Foerderstatus.ZieldifferentGeistigeEntwicklung;

        // 2. Match by year group + term. A form with Jahrgang 0 ("any year", e.g. the Zeugnis zum
        // Gemeinsamen Lernen) is only a candidate for a zieldifferent child.
        var candidates = alleFormulare
            .Where(f => f.Jahrgang == jahrgang || (f.Jahrgang == 0 && wantsZieldifferent))
            .Where(f => f.GiltFuerHalbjahr is null || f.GiltFuerHalbjahr == halbjahr)
            .ToList();

        var best = candidates.FirstOrDefault(f => f.IstZieldifferent == wantsZieldifferent)
                   ?? candidates.FirstOrDefault();

        return best is null
            ? new FormularSuggestion(null, SuggestionReason.KeinPassendesFormular)
            : new FormularSuggestion(best, SuggestionReason.AutomatischNachJahrgang);
    }
}
