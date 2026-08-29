using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Domain.Fehlzeiten;

/// <summary>Absence totals for a date range.</summary>
public readonly record struct FehlzeitenSumme(int StundenGesamt, int StundenUnentschuldigt);

/// <summary>
/// Rolls a set of <see cref="Fehltag"/> entries up into the totals a report card shows (plan §4,
/// calendar variant). Pure — this is the seam the calendar UI would use once it is built.
/// </summary>
public static class FehltageAggregator
{
    public static FehlzeitenSumme Summarize(IEnumerable<Fehltag> fehltage, DateOnly von, DateOnly bis)
    {
        var gesamt = 0;
        var unentschuldigt = 0;

        foreach (var tag in fehltage)
        {
            if (tag.Datum < von || tag.Datum > bis)
            {
                continue;
            }

            var stunden = Math.Max(0, tag.Stunden);
            gesamt += stunden;
            if (tag.Unentschuldigt)
            {
                unentschuldigt += stunden;
            }
        }

        return new FehlzeitenSumme(gesamt, unentschuldigt);
    }
}
