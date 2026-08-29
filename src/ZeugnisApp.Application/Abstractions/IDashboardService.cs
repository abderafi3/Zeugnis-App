namespace ZeugnisApp.Application.Abstractions;

/// <summary>Supplies the live counts shown on the five start-screen tiles (plan §7.2).</summary>
public interface IDashboardService
{
    Task<DashboardCounts> GetCountsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Live counts for the start screen.</summary>
public sealed record DashboardCounts(
    int Klassen,
    int Kinder,
    int Formulare,
    int Floskeln,
    int OffeneZeugnisse);
