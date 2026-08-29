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
}
