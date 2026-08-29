using ZeugnisApp.App.ViewModels.Floskeln;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Tests;

public class FloskelPickerTests
{
    private sealed class FakeFloskelnService(params Floskel[] floskeln) : IFloskelnService
    {
        public Task<IReadOnlyList<Floskel>> GetAsync(FloskelFilter filter, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Floskel>>(floskeln);

        public Task<Floskel> SaveAsync(Floskel floskel, CancellationToken ct = default) => Task.FromResult(floskel);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetBereicheAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(["Sozialverhalten"]);
    }

    [Fact]
    public async Task Preview_resolves_placeholders_for_the_sample_child()
    {
        var service = new FakeFloskelnService(
            new Floskel { Text = "[Name] arbeitet ruhig. [er/sie] hilft anderen.", Bereich = "Sozialverhalten", Stufe = 1 });
        var vm = new FloskelPickerViewModel(service);

        await vm.InitializeAsync();
        vm.SampleName = "Max";
        vm.SampleGeschlecht = Geschlecht.Maennlich;

        Assert.Equal("Max arbeitet ruhig. Er hilft anderen.", vm.PreviewText);
    }

    [Fact]
    public async Task Einfuegen_sets_the_result_and_requests_close()
    {
        var service = new FakeFloskelnService(
            new Floskel { Text = "[Name] ist zuverlässig.", Bereich = "Sozialverhalten", Stufe = 2 });
        var vm = new FloskelPickerViewModel(service);
        await vm.InitializeAsync();
        vm.SampleName = "Mia";
        vm.SampleGeschlecht = Geschlecht.Weiblich;

        bool? closedWith = null;
        vm.RequestClose += ok => closedWith = ok;

        vm.EinfuegenCommand.Execute(null);

        Assert.True(closedWith);
        Assert.Equal("Mia ist zuverlässig.", vm.Result);
    }
}
