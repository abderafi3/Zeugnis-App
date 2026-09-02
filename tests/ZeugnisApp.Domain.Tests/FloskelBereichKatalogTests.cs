using ZeugnisApp.Domain.Floskeln;

namespace ZeugnisApp.Domain.Tests;

public class FloskelBereichKatalogTests
{
    [Theory]
    [InlineData("ASV", "", "Arbeits- und Sozialverhalten")]
    [InlineData("FSP", "", "Förderschwerpunkt")]
    [InlineData("ZB", "", "Bemerkungen")]
    [InlineData("FACH", "D", "Deutsch")]
    [InlineData("FACH", "SG", "Sprachgebrauch")]
    [InlineData("FACH", "LE", "Lesen")]
    [InlineData("FACH", "RS", "Rechtschreiben")]
    [InlineData("FACH", "M", "Mathematik")]
    [InlineData("FACH", "E", "Englisch")]
    [InlineData("FACH", "SU", "Sachunterricht")]
    [InlineData("FACH", "ER", "Evangelische Religionslehre")]
    [InlineData("FACH", "KR", "Katholische Religionslehre")]
    [InlineData("", "MU", "Musik")]
    public void Resolves_group_and_subject_codes(string gruppe, string fach, string expected)
        => Assert.Equal(expected, FloskelBereichKatalog.Resolve(gruppe, fach));

    [Fact]
    public void Unknown_subject_code_falls_back_to_its_own_text()
        => Assert.Equal("Schwimmen", FloskelBereichKatalog.Resolve("FACH", "Schwimmen"));

    [Fact]
    public void Nothing_to_resolve_yields_empty()
        => Assert.Equal(string.Empty, FloskelBereichKatalog.Resolve(null, null));
}
