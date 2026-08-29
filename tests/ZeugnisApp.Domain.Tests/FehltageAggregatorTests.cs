using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Fehlzeiten;

namespace ZeugnisApp.Domain.Tests;

public class FehltageAggregatorTests
{
    private static Fehltag Tag(int day, int stunden, bool unentschuldigt) => new()
    {
        Datum = new DateOnly(2026, 11, day),
        Stunden = stunden,
        Unentschuldigt = unentschuldigt,
    };

    [Fact]
    public void Sums_hours_in_range_and_splits_out_the_unexcused_ones()
    {
        Fehltag[] tage =
        [
            Tag(3, 4, unentschuldigt: false),
            Tag(10, 6, unentschuldigt: true),
            Tag(17, 2, unentschuldigt: false),
        ];

        var summe = FehltageAggregator.Summarize(tage, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        Assert.Equal(12, summe.StundenGesamt);
        Assert.Equal(6, summe.StundenUnentschuldigt);
    }

    [Fact]
    public void Ignores_days_outside_the_date_range()
    {
        Fehltag[] tage = [Tag(1, 5, true), Tag(30, 5, true)];

        var summe = FehltageAggregator.Summarize(tage, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 20));

        Assert.Equal(0, summe.StundenGesamt);
        Assert.Equal(0, summe.StundenUnentschuldigt);
    }

    [Fact]
    public void Treats_negative_hours_as_zero()
    {
        Fehltag[] tage = [Tag(10, -3, false), Tag(11, 4, false)];

        var summe = FehltageAggregator.Summarize(tage, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        Assert.Equal(4, summe.StundenGesamt);
    }
}
