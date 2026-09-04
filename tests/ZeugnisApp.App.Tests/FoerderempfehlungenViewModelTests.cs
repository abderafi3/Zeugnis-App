using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Zeugnisse;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Tests;

public class FoerderempfehlungenViewModelTests
{
    private sealed class FakeService : IFoerderempfehlungService
    {
        public readonly List<Foerderempfehlung> Store = [];

        public Task<IReadOnlyList<Foerderempfehlung>> GetForKindAsync(Guid kindId, string schuljahr, Halbjahr halbjahr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Foerderempfehlung>>(
                Store.Where(e => e.KindId == kindId && e.Schuljahr == schuljahr && e.Halbjahr == halbjahr)
                     .OrderBy(e => e.Fach).ToList());

        public Task<Foerderempfehlung?> GetAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(e => e.Id == id));

        public Task<Foerderempfehlung> GetOrCreateAsync(Guid kindId, string schuljahr, Halbjahr halbjahr, string fach, CancellationToken ct = default)
        {
            var f = fach.Trim();
            var existing = Store.FirstOrDefault(e =>
                e.KindId == kindId && e.Schuljahr == schuljahr && e.Halbjahr == halbjahr && e.Fach == f);
            if (existing is null)
            {
                existing = new Foerderempfehlung { KindId = kindId, Schuljahr = schuljahr, Halbjahr = halbjahr, Fach = f };
                Store.Add(existing);
            }

            return Task.FromResult(existing);
        }

        public Task SaveAsync(Foerderempfehlung empfehlung, CancellationToken ct = default)
        {
            if (Store.All(e => e.Id != empfehlung.Id))
            {
                Store.Add(empfehlung);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            Store.RemoveAll(e => e.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class Clock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 8, 29);
    }

    private sealed class YesDialogs : IDialogService
    {
        public Task<bool> ConfirmAsync(string title, string body, string? okText = null, string? cancelText = null)
            => Task.FromResult(true);
    }

    private sealed class NullNavigator : IZeugnisseNavigator
    {
        public void ShowUebersicht() { }
        public Task EditZeugnisAsync(Guid k, string s, Halbjahr h) => Task.CompletedTask;
        public Task ShowFoerderempfehlungenAsync(Guid k, string n, string s, Halbjahr h) => Task.CompletedTask;
    }

    private static FoerderempfehlungenViewModel NewVm(FakeService svc, Guid kindId)
        => new(svc, new SaveStatusService(new Clock()), new YesDialogs(), new NullNavigator(),
            kindId, "Lena Muster", "2026/27", Halbjahr.Erstes);

    [Fact]
    public async Task Adding_a_subject_creates_a_card_and_removes_it_from_the_picklist()
    {
        var svc = new FakeService();
        var kindId = Guid.NewGuid();
        var vm = NewVm(svc, kindId);
        await vm.InitializeAsync();

        Assert.True(vm.IstLeer);
        Assert.Contains("Deutsch", vm.FachOptionen);

        vm.NeuesFach = "Deutsch";
        await vm.HinzufuegenCommand.ExecuteAsync(null);

        Assert.False(vm.IstLeer);
        Assert.Single(vm.Empfehlungen);
        Assert.Equal("Deutsch", vm.Empfehlungen[0].Fach);
        Assert.DoesNotContain("Deutsch", vm.FachOptionen);   // no longer offered
        Assert.Null(vm.NeuesFach);
        Assert.Single(svc.Store);
    }

    [Fact]
    public async Task Editing_a_card_and_flushing_persists_the_three_sections()
    {
        var svc = new FakeService();
        var kindId = Guid.NewGuid();
        var vm = NewVm(svc, kindId);
        await vm.InitializeAsync();

        vm.NeuesFach = "Mathematik";
        await vm.HinzufuegenCommand.ExecuteAsync(null);

        var card = vm.Empfehlungen[0];
        card.Beobachtungen = "Unsicher im ZR bis 100.";
        card.SchulischeMassnahmen = "Fördergruppe.";
        card.EmpfehlungenFuerZuhause = "Täglich Kopfrechnen.";
        card.Fertiggestellt = true;

        await vm.FlushAsync();

        var saved = svc.Store.Single();
        Assert.Equal("Unsicher im ZR bis 100.", saved.Beobachtungen);
        Assert.Equal("Fördergruppe.", saved.SchulischeMassnahmen);
        Assert.Equal("Täglich Kopfrechnen.", saved.EmpfehlungenFuerZuhause);
        Assert.True(saved.Fertiggestellt);
    }

    [Fact]
    public async Task Deleting_a_card_removes_it_and_re_offers_the_subject()
    {
        var svc = new FakeService();
        var kindId = Guid.NewGuid();
        var vm = NewVm(svc, kindId);
        await vm.InitializeAsync();

        vm.NeuesFach = "Englisch";
        await vm.HinzufuegenCommand.ExecuteAsync(null);
        var card = vm.Empfehlungen[0];

        await card.LoeschenCommand.ExecuteAsync(null);

        Assert.Empty(vm.Empfehlungen);
        Assert.True(vm.IstLeer);
        Assert.Contains("Englisch", vm.FachOptionen);
        Assert.Empty(svc.Store);
    }

    [Fact]
    public async Task Existing_recommendations_load_on_initialize()
    {
        var svc = new FakeService();
        var kindId = Guid.NewGuid();
        svc.Store.Add(new Foerderempfehlung { KindId = kindId, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes, Fach = "Deutsch" });
        svc.Store.Add(new Foerderempfehlung { KindId = kindId, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes, Fach = "Sport" });

        var vm = NewVm(svc, kindId);
        await vm.InitializeAsync();

        Assert.Equal(new[] { "Deutsch", "Sport" }, vm.Empfehlungen.Select(r => r.Fach));
    }
}
