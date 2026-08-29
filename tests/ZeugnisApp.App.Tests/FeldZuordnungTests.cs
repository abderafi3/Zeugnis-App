using ZeugnisApp.App.ViewModels.Formulare;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.App.Tests;

public class FeldZuordnungTests
{
    private static Formular Bericht() => new()
    {
        Anzeigename = "Klasse 1", Jahrgang = 1, HatNotenblock = false,
        Freitextfelder = [new() { Name = "Deutsch" }, new() { Name = "Bemerkungen" }],
    };

    private static ContentControlInfo Cc(string tag) => new(tag, null, "", "Text");

    [Fact]
    public void Preselects_rows_whose_default_tag_exists_in_the_uploaded_document()
    {
        var formular = Bericht();
        var mapping = FieldMapping.Default(formular);
        var controls = new[]
        {
            Cc(mapping.TagFor(ValueKeys.KindName)!),
            Cc(mapping.TagFor(ValueKeys.Freitext("Deutsch"))!),
            Cc("Ein_Fremder_Tag"),
        };

        var vm = new FeldZuordnungViewModel(formular, controls, bestehendeZuordnungJson: null);

        var kindName = vm.Zeilen.First(z => z.ValueKey == ValueKeys.KindName);
        Assert.Equal(mapping.TagFor(ValueKeys.KindName), kindName.AusgewaehltesTag);

        var adresse = vm.Zeilen.First(z => z.ValueKey == ValueKeys.SchuleAdresse);
        Assert.Null(adresse.AusgewaehltesTag); // no matching control
    }

    [Fact]
    public void Uebernehmen_is_blocked_until_every_required_field_is_mapped()
    {
        var formular = Bericht();
        var mapping = FieldMapping.Default(formular);
        // Only one of the two required Freitext controls is present.
        var controls = new[] { Cc(mapping.TagFor(ValueKeys.Freitext("Deutsch"))!) };

        var vm = new FeldZuordnungViewModel(formular, controls, null);

        Assert.False(vm.AllePflichtZugeordnet);
        Assert.False(vm.UebernehmenCommand.CanExecute(null));
        Assert.Contains("Bemerkungen", vm.Hinweis, StringComparison.Ordinal);

        // Map the missing one to the only control and it unblocks.
        vm.Zeilen.First(z => z.ValueKey == ValueKeys.Freitext("Bemerkungen")).AusgewaehltesTag =
            mapping.TagFor(ValueKeys.Freitext("Deutsch"));
        Assert.True(vm.AllePflichtZugeordnet);

        vm.UebernehmenCommand.Execute(null);
        Assert.NotNull(vm.Ergebnis);
        Assert.Equal(mapping.TagFor(ValueKeys.Freitext("Deutsch")),
            vm.Ergebnis!.TagFor(ValueKeys.Freitext("Bemerkungen")));
    }

    [Fact]
    public void Existing_mapping_wins_over_the_default_when_both_tags_are_present()
    {
        var formular = Bericht();
        var existing = new FieldMapping();
        existing.Tags[ValueKeys.KindName] = "Custom_Name_Tag";
        var controls = new[] { Cc("Custom_Name_Tag"), Cc(FieldMapping.Default(formular).TagFor(ValueKeys.KindName)!) };

        var vm = new FeldZuordnungViewModel(formular, controls, existing.ToJson());

        Assert.Equal("Custom_Name_Tag", vm.Zeilen.First(z => z.ValueKey == ValueKeys.KindName).AusgewaehltesTag);
    }
}
