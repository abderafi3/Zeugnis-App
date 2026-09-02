using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Tests;

public class EntityRuleTests
{
    [Fact]
    public void Kind_IstVollstaendig_when_all_mandatory_fields_present()
    {
        var kind = new Kind
        {
            Vorname = "Max",
            Nachname = "Mustermann",
            Geburtsdatum = new DateOnly(2018, 5, 1),
            Geschlecht = Geschlecht.Maennlich,
        };

        Assert.True(kind.IstVollstaendig);
        Assert.Equal("Max Mustermann", kind.AnzeigeName);
    }

    [Theory]
    [InlineData(2, 3, 2)]   // own Schulbesuchsjahr wins over the class year
    [InlineData(0, 3, 3)]   // unset → falls back to the class's Jahrgang
    [InlineData(0, 0, 0)]   // neither known
    [InlineData(4, 0, 4)]   // own year, class year unknown
    public void Kind_EffektivesSchulbesuchsjahr_prefers_the_childs_own_year(int sbj, int klasseJahrgang, int expected)
    {
        var kind = new Kind
        {
            Schulbesuchsjahr = sbj,
            Klasse = new Klasse { Name = "E7", Jahrgang = klasseJahrgang },
        };

        Assert.Equal(expected, kind.EffektivesSchulbesuchsjahr);
    }

    [Fact]
    public void Kind_EffektivesSchulbesuchsjahr_is_zero_when_class_not_loaded_and_year_unset()
        => Assert.Equal(0, new Kind().EffektivesSchulbesuchsjahr);

    [Theory]
    [InlineData("", "Mustermann", true, true)]
    [InlineData("Max", "", true, true)]
    [InlineData("Max", "Mustermann", false, true)]
    [InlineData("Max", "Mustermann", true, false)]
    public void Kind_IstUnvollstaendig_when_a_mandatory_field_missing(
        string vorname, string nachname, bool hasBirthDate, bool hasGender)
    {
        var kind = new Kind
        {
            Vorname = vorname,
            Nachname = nachname,
            Geburtsdatum = hasBirthDate ? new DateOnly(2018, 5, 1) : null,
            Geschlecht = hasGender ? Geschlecht.Weiblich : null,
        };

        Assert.False(kind.IstVollstaendig);
    }

    [Theory]
    [InlineData(10, 4, true)]
    [InlineData(4, 4, true)]
    [InlineData(0, 0, true)]
    [InlineData(4, 5, false)]   // unexcused must not exceed total
    [InlineData(-1, 0, false)]
    public void Fehlzeit_IstGueltig_enforces_unentschuldigt_le_gesamt(int gesamt, int unentschuldigt, bool expected)
    {
        var fehlzeit = new Fehlzeit { StundenGesamt = gesamt, StundenUnentschuldigt = unentschuldigt };
        Assert.Equal(expected, fehlzeit.IstGueltig);
    }

    [Fact]
    public void Formular_IstGenerierungsbereit_only_once_a_word_file_is_linked()
    {
        var formular = new Formular { Anzeigename = "Klasse 3 – 1. Halbjahr" };
        Assert.False(formular.IstGenerierungsbereit);

        formular.WordFilePath = @"C:\Vorlagen\klasse3-hj1.docx";
        Assert.True(formular.IstGenerierungsbereit);
    }
}
