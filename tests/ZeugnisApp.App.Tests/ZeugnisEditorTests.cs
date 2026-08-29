using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Zeugnisse;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Tests;

public class ZeugnisEditorTests
{
    private sealed class SavingZeugnisService : IZeugnisService
    {
        public Zeugnis? Saved { get; private set; }
        public Task SaveAsync(Zeugnis zeugnis, CancellationToken ct = default) { Saved = zeugnis; return Task.CompletedTask; }

        public Task<ZeugnisErgebnis> GetOrCreateAsync(Guid k, string s, Halbjahr h, Guid? f = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Zeugnis?> GetAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Zeugnis?> GetWithContextAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetFertiggestelltAsync(Guid id, bool v, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ZeugnisUebersichtZeile>> GetUebersichtForKlasseAsync(Guid k, string s, Halbjahr h, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubPicker(string? result) : IFloskelPickerService
    {
        public Task<string?> PickAsync(string sampleName = "Alex", Geschlecht? sampleGeschlecht = Geschlecht.Weiblich)
            => Task.FromResult(result);
    }

    private sealed class NullNavigator : IZeugnisseNavigator
    {
        public void ShowUebersicht() { }
        public Task EditZeugnisAsync(Guid kindId, string schuljahr, Halbjahr halbjahr) => Task.CompletedTask;
    }

    private static Zeugnis BuildZeugnis(Formular formular, Kind kind) => new()
    {
        KindId = kind.Id, FormularId = formular.Id, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes,
        Kind = kind, Formular = formular,
    };

    private static Formular NotenFormular() => new()
    {
        Anzeigename = "Klasse 3 – 1. Halbjahr", Jahrgang = 3, HatNotenblock = true, HatBegruendeteEmpfehlung = false,
        Faecher = ["Deutsch", "Mathematik"],
        Freitextfelder = [new() { Name = "Arbeits- und Sozialverhalten", Reihenfolge = 0, MaxZeichen = 20 }],
    };

    [Fact]
    public async Task Editor_shows_only_the_sections_the_formular_switches_enable()
    {
        var formular = NotenFormular();
        var kind = new Kind { Vorname = "Anna", Nachname = "A", Foerderstatus = Foerderstatus.Regel };
        var vm = new ZeugnisEditorViewModel(BuildZeugnis(formular, kind), new SavingZeugnisService(), new StubPicker(null),
            new SaveStatusService(new TestClock()), new NullNavigator());

        await vm.InitializeAsync();

        Assert.True(vm.ShowNoten);
        Assert.False(vm.ShowEmpfehlung);
        Assert.False(vm.ShowVersetzung);
        Assert.Equal(2, vm.Noten.Count);
        Assert.Single(vm.Freitexte);
    }

    [Fact]
    public async Task Editor_auto_inserts_the_zieldifferent_remark_into_the_bemerkungen()
    {
        var kind = new Kind { Vorname = "Tom", Nachname = "B", Foerderstatus = Foerderstatus.ZieldifferentLernen };
        var service = new SavingZeugnisService();
        var vm = new ZeugnisEditorViewModel(BuildZeugnis(NotenFormular(), kind), service, new StubPicker(null),
            new SaveStatusService(new TestClock()), new NullNavigator());

        await vm.InitializeAsync();

        Assert.True(vm.ZieldifferentHinweisEingefuegt);
        Assert.Contains("Lernen", vm.Bemerkungen!, StringComparison.Ordinal);
        Assert.Same(vm.Bemerkungen, service.Saved!.Bemerkungen); // persisted on open
    }

    [Fact]
    public async Task Inserting_a_floskel_appends_the_resolved_text_to_the_field()
    {
        var vm = new ZeugnisEditorViewModel(BuildZeugnis(NotenFormular(),
                new Kind { Vorname = "Anna", Nachname = "A" }),
            new SavingZeugnisService(), new StubPicker("Anna arbeitet konzentriert."),
            new SaveStatusService(new TestClock()), new NullNavigator());
        await vm.InitializeAsync();

        var feld = vm.Freitexte[0];
        feld.Text = "Startsatz.";
        await feld.FloskelEinfuegenCommand.ExecuteAsync(null);

        Assert.Equal("Startsatz. Anna arbeitet konzentriert.", feld.Text);
        Assert.True(feld.IstUeberlauf); // 20-char budget exceeded -> orange flag
    }

    private sealed class TestClock : ZeugnisApp.Application.Abstractions.IClock
    {
        public DateTime UtcNow => new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 8, 29);
    }
}
