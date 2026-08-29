using ZeugnisApp.Domain.Floskeln;

namespace ZeugnisApp.Domain.Tests;

public class FloskelBereichMatcherTests
{
    [Theory]
    [InlineData("Deutsch", "Deutsch")]
    [InlineData("deutsch", "  Deutsch ")]
    [InlineData("Arbeits- und Sozialverhalten", "Sozialverhalten")]
    [InlineData("Arbeits- und Sozialverhalten", "Arbeitsverhalten")]
    [InlineData("Sachunterricht", "Sachunterricht")]
    [InlineData("Bemerkungen", "Bemerkungen")]
    [InlineData("Englisch", "")]       // empty Bereich = wildcard, fits every field
    [InlineData("Mathematik", "   ")]
    public void Matches_related_labels(string feld, string bereich)
        => Assert.True(FloskelBereichMatcher.Matches(feld, bereich));

    [Theory]
    [InlineData("Deutsch", "Mathematik")]
    [InlineData("Bemerkungen", "Sozialverhalten")]
    [InlineData("", "Deutsch")]
    [InlineData("Musik", "Sport")]
    public void Rejects_unrelated_labels(string feld, string bereich)
        => Assert.False(FloskelBereichMatcher.Matches(feld, bereich));
}
