using ZeugnisApp.Domain.Floskeln;

namespace ZeugnisApp.Domain.Tests;

public class FloskelVariantExpanderTests
{
    [Fact]
    public void A_phrase_without_a_choice_is_returned_unchanged()
    {
        var result = FloskelVariantExpander.Expand("$Vorname$ arbeitet konzentriert und zuverlässig.");

        Assert.Equal(["$Vorname$ arbeitet konzentriert und zuverlässig."], result);
    }

    [Fact]
    public void A_tight_three_way_adverb_scale_expands()
    {
        var result = FloskelVariantExpander.Expand(
            "$VORNAME$ stellte sich freudig/oft/teilweise neuen Lernaufgaben.");

        Assert.Equal(
        [
            "$VORNAME$ stellte sich freudig neuen Lernaufgaben.",
            "$VORNAME$ stellte sich oft neuen Lernaufgaben.",
            "$VORNAME$ stellte sich teilweise neuen Lernaufgaben.",
        ],
            result);
    }

    [Fact]
    public void A_parenthesised_choice_expands_and_drops_the_parentheses()
    {
        var result = FloskelVariantExpander.Expand(
            "&Er%Sie& erledigte Aufgaben (oft/meist/teilweise) zügig.");

        Assert.Equal(
        [
            "&Er%Sie& erledigte Aufgaben oft zügig.",
            "&Er%Sie& erledigte Aufgaben meist zügig.",
            "&Er%Sie& erledigte Aufgaben teilweise zügig.",
        ],
            result);
    }

    [Fact]
    public void A_parenthesised_choice_may_carry_multi_word_alternatives()
    {
        var result = FloskelVariantExpander.Expand(
            "$Vorname$ erfasste Sachverhalte (sicher/stets/mit einiger Übung) zügig.");

        Assert.Equal(
        [
            "$Vorname$ erfasste Sachverhalte sicher zügig.",
            "$Vorname$ erfasste Sachverhalte stets zügig.",
            "$Vorname$ erfasste Sachverhalte mit einiger Übung zügig.",
        ],
            result);
    }

    [Fact]
    public void Several_tight_scales_expand_to_the_cartesian_product()
    {
        var result = FloskelVariantExpander.Expand(
            "$Vorname$ arbeitete meist/oft/teilweise aufmerksam und trug stets/oft/vereinzelt bei.");

        Assert.Equal(9, result.Count);
        Assert.Contains("$Vorname$ arbeitete meist aufmerksam und trug stets bei.", result);
        Assert.Contains("$Vorname$ arbeitete teilweise aufmerksam und trug vereinzelt bei.", result);
        Assert.All(result, v => Assert.DoesNotContain("/", v));
    }

    [Theory]
    [InlineData("$Vorname$ kann die Rechtschreibung mit dem Wörterbuch/PC überprüfen.")]
    [InlineData("Geübte Texte/Lernwörter schreibt $Vorname$ sicher ab.")]
    [InlineData("$Vorname$ kann Wörter wiedererkennen (z.B. Bild/Satz-Wort-Zuordnung).")]
    public void Compound_terms_with_a_capital_are_left_intact(string phrase)
    {
        Assert.Equal([phrase], FloskelVariantExpander.Expand(phrase));
    }

    [Theory]
    [InlineData("&Er%Sie& war in der Lage, ihre/seine Leistung einzuschätzen.")]
    [InlineData("$Vorname$ versteht Sätze, die sie/er richtig einsetzt.")]
    public void Gender_pairs_are_never_treated_as_a_choice(string phrase)
    {
        Assert.Equal([phrase], FloskelVariantExpander.Expand(phrase));
    }

    [Theory]
    [InlineData("Es gelingt $Vorname$ Wörter lautgetreu/fehlerfrei aufzuschreiben.")]        // only two options
    [InlineData("$Vorname$ arbeitete über längere/ über kurze Zeiträume ausdauernd.")]       // spaced, multi-word
    public void Ambiguous_two_way_or_spaced_runs_are_left_intact(string phrase)
    {
        Assert.Equal([phrase], FloskelVariantExpander.Expand(phrase));
    }

    [Fact]
    public void A_pathological_phrase_is_left_unsplit()
    {
        // 5 tight groups of 3 => 243 variants, well over the safety cap.
        const string phrase =
            "a/b/c dann d/e/f und g/h/i sowie j/k/l schließlich m/n/o fertig.";

        Assert.Equal([phrase], FloskelVariantExpander.Expand(phrase));
    }
}
