using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Domain.Floskeln;

namespace ZeugnisApp.Domain.Tests;

public class PlaceholderEngineTests
{
    [Fact]
    public void First_Name_is_the_real_name_then_later_ones_become_pronouns()
    {
        const string template = "[Name] arbeitet konzentriert. [Name] hilft anderen Kindern.";

        var male = PlaceholderEngine.Resolve(template, "Max", Geschlecht.Maennlich);
        Assert.Equal("Max arbeitet konzentriert. Er hilft anderen Kindern.", male);

        var female = PlaceholderEngine.Resolve(template, "Mia", Geschlecht.Weiblich);
        Assert.Equal("Mia arbeitet konzentriert. Sie hilft anderen Kindern.", female);
    }

    [Fact]
    public void Possessive_and_object_pronouns_follow_gender()
    {
        const string template = "[Name] bringt [sein/ihr] Heft mit. Das Lob gilt [ihm/ihr]. [seine/ihre] Arbeit ist sauber.";

        Assert.Equal(
            "Max bringt sein Heft mit. Das Lob gilt ihm. Seine Arbeit ist sauber.",
            PlaceholderEngine.Resolve(template, "Max", Geschlecht.Maennlich));

        Assert.Equal(
            "Mia bringt ihr Heft mit. Das Lob gilt ihr. Ihre Arbeit ist sauber.",
            PlaceholderEngine.Resolve(template, "Mia", Geschlecht.Weiblich));
    }

    [Fact]
    public void Pronoun_at_sentence_start_is_capitalised_automatically()
    {
        var text = PlaceholderEngine.Resolve("[Name] ist fleißig. [er/sie] meldet sich oft.", "Max", Geschlecht.Maennlich);
        Assert.Equal("Max ist fleißig. Er meldet sich oft.", text);
    }

    [Fact]
    public void Divers_falls_back_to_the_name_for_every_placeholder()
    {
        var text = PlaceholderEngine.Resolve("[Name] zeigt Einsatz. [Name] bringt [sein/ihr] Material mit.", "Alex", Geschlecht.Divers);
        Assert.Equal("Alex zeigt Einsatz. Alex bringt Alex Material mit.", text);
    }

    [Fact]
    public void Unknown_placeholders_are_left_untouched()
    {
        var text = PlaceholderEngine.Resolve("[Name] nutzt [Werkzeug] sicher.", "Max", Geschlecht.Maennlich);
        Assert.Equal("Max nutzt [Werkzeug] sicher.", text);
    }

    // ---- School's native syntax ($Vorname$ / &m%w&), as used in Grundschule Wehringhausen's phrase file ----

    [Fact]
    public void Dollar_Vorname_is_replaced_every_time_with_case_control()
    {
        const string template = "Dabei gelingt es $Vorname$ sicher. $VORNAME$ liest. $vorname$ schreibt.";
        Assert.Equal(
            "Dabei gelingt es Mia sicher. MIA liest. mia schreibt.",
            PlaceholderEngine.Resolve(template, "Mia", Geschlecht.Weiblich));
    }

    [Theory]
    [InlineData("&Er%Sie& beteiligt sich aktiv.", "Er beteiligt sich aktiv.", "Sie beteiligt sich aktiv.")]
    [InlineData("Auch fremde Texte trägt &er/sie& sicher vor.", "Auch fremde Texte trägt er sicher vor.", "Auch fremde Texte trägt sie sicher vor.")]
    [InlineData("$Vorname$ bringt &seine%ihre& Materialien mit.", "Max bringt seine Materialien mit.", "Lena bringt ihre Materialien mit.")]
    public void Gender_pairs_pick_the_matching_side(string template, string male, string female)
    {
        Assert.Equal(male, PlaceholderEngine.Resolve(template, "Max", Geschlecht.Maennlich));
        Assert.Equal(female, PlaceholderEngine.Resolve(template, "Lena", Geschlecht.Weiblich));
    }

    [Fact]
    public void Gender_pair_at_sentence_start_is_capitalised()
    {
        Assert.Equal(
            "Sie meldet sich oft.",
            PlaceholderEngine.Resolve("&er%sie& meldet sich oft.", "Lena", Geschlecht.Weiblich));
    }

    [Fact]
    public void Gender_pairs_support_arbitrary_role_nouns()
    {
        const string template = "$Vorname$ war erfolgreich als &Klassensprecher%Klassensprecherin& tätig.";
        Assert.Equal(
            "Max war erfolgreich als Klassensprecher tätig.",
            PlaceholderEngine.Resolve(template, "Max", Geschlecht.Maennlich));
        Assert.Equal(
            "Lena war erfolgreich als Klassensprecherin tätig.",
            PlaceholderEngine.Resolve(template, "Lena", Geschlecht.Weiblich));
    }

    [Fact]
    public void Divers_falls_back_to_the_name_for_school_syntax_too()
    {
        var text = PlaceholderEngine.Resolve("&Er%Sie& bringt $Vorname$ Material mit.", "Alex", Geschlecht.Divers);
        Assert.Equal("Alex bringt Alex Material mit.", text);
    }

    [Fact]
    public void A_literal_ampersand_in_running_text_is_left_alone()
    {
        var text = PlaceholderEngine.Resolve("Eltern & Kind sind eingeladen.", "Max", Geschlecht.Maennlich);
        Assert.Equal("Eltern & Kind sind eingeladen.", text);
    }
}
