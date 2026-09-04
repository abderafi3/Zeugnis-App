using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Fehlzeiten;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Tests;

public class FehlzeitRowViewModelTests
{
    private sealed class FakeFehlzeiten : IFehlzeitenService
    {
        public readonly List<(Guid Kind, int Gesamt, int Unentschuldigt)> Saved = [];

        public Task<IReadOnlyList<FehlzeitZeile>> GetForKlasseAsync(Guid k, string s, Halbjahr h, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FehlzeitZeile>>([]);

        public Task SaveAsync(Guid kindId, string schuljahr, Halbjahr halbjahr, int gesamt, int unentschuldigt, CancellationToken ct = default)
        {
            Saved.Add((kindId, gesamt, unentschuldigt));
            return Task.CompletedTask;
        }

        public Task<Fehlzeit?> GetAsync(Guid k, string s, Halbjahr h, CancellationToken ct = default)
            => Task.FromResult<Fehlzeit?>(null);
    }

    private static FehlzeitRowViewModel Row(FakeFehlzeiten svc, int gesamt = 0, int unentschuldigt = 0)
        => new(new FehlzeitZeile(Guid.NewGuid(), "Kind A", gesamt, unentschuldigt), "2026/27", Halbjahr.Erstes,
            svc, new SaveStatusService(new StubClock()));

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow => new(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 9, 4);
    }

    [Fact]
    public async Task A_valid_edit_is_flushed_to_the_service()
    {
        var svc = new FakeFehlzeiten();
        var row = Row(svc);

        row.Gesamt = 12;
        row.Unentschuldigt = 3;
        await row.FlushAsync();

        Assert.Single(svc.Saved);
        Assert.Equal(12, svc.Saved[0].Gesamt);
        Assert.Equal(3, svc.Saved[0].Unentschuldigt);
    }

    [Fact]
    public async Task Nothing_is_saved_when_there_is_no_pending_edit()
    {
        var svc = new FakeFehlzeiten();
        var row = Row(svc, gesamt: 5, unentschuldigt: 1);

        await row.FlushAsync();

        Assert.Empty(svc.Saved);
    }

    [Fact]
    public async Task An_invalid_edit_shows_the_hint_and_is_not_saved()
    {
        var svc = new FakeFehlzeiten();
        var row = Row(svc);

        row.Gesamt = 3;
        row.Unentschuldigt = 5; // more unexcused than total

        Assert.False(row.IstGueltig);
        Assert.NotEqual(string.Empty, row.ErrorText);

        await row.FlushAsync();
        Assert.Empty(svc.Saved);

        // fixing it makes the row valid again and saveable
        row.Unentschuldigt = 2;
        Assert.True(row.IstGueltig);
        Assert.Equal(string.Empty, row.ErrorText);

        await row.FlushAsync();
        Assert.Equal((3, 2), (svc.Saved[0].Gesamt, svc.Saved[0].Unentschuldigt));
    }

    [Fact]
    public void A_cleared_box_counts_as_zero()
    {
        var row = Row(new FakeFehlzeiten(), gesamt: 4, unentschuldigt: 1);

        row.Gesamt = null;

        Assert.False(row.IstGueltig); // unentschuldigt 1 > gesamt 0
        row.Unentschuldigt = null;
        Assert.True(row.IstGueltig);
    }
}
