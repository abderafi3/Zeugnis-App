using System.Windows;
using System.Windows.Controls;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Zeugnisse;
using ZeugnisApp.App.Views.Zeugnisse;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Tests;

/// <summary>
/// Loads the real <see cref="FoerderempfehlungenView"/> XAML with an initialized view model and
/// forces a layout pass — catches invalid SymbolRegular names and binding/template errors.
/// </summary>
[Collection("WpfRender")]
public sealed class FoerderempfehlungenViewRenderTests(WpfDispatcherFixture wpf)
{
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

    private sealed class OneRowService : IFoerderempfehlungService
    {
        private readonly List<Foerderempfehlung> _store =
            [new() { KindId = Guid.NewGuid(), Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes, Fach = "Deutsch", Beobachtungen = "Liest noch stockend." }];

        public Task<IReadOnlyList<Foerderempfehlung>> GetForKindAsync(Guid k, string s, Halbjahr h, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Foerderempfehlung>>(_store);
        public Task<Foerderempfehlung?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_store.FirstOrDefault(e => e.Id == id));
        public Task<Foerderempfehlung> GetOrCreateAsync(Guid k, string s, Halbjahr h, string fach, CancellationToken ct = default) => Task.FromResult(_store[0]);
        public Task SaveAsync(Foerderempfehlung e, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    [Fact]
    public void View_renders_with_a_real_view_model()
    {
        Exception? failure = null;

        wpf.Invoke(() =>
        {
            try
            {
                var vm = new FoerderempfehlungenViewModel(
                    new OneRowService(), new SaveStatusService(new Clock()), new YesDialogs(), new NullNavigator(),
                    Guid.NewGuid(), "Lena Muster", "2026/27", Halbjahr.Erstes);
                vm.InitializeAsync().GetAwaiter().GetResult();

                var view = new FoerderempfehlungenView { DataContext = vm };
                var host = new ContentControl { Content = view, Width = 1000, Height = 1200 };
                host.Measure(new Size(1000, 1200));
                host.Arrange(new Rect(0, 0, 1000, 1200));
                host.UpdateLayout();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        Assert.True(failure is null, failure?.ToString());
    }
}
