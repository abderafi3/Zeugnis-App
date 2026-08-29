using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>Simple absence totals per child per term (plan §Sprint 7).</summary>
public interface IFehlzeitenService
{
    /// <summary>One row per child in the class for the given term (zeros when nothing entered yet).</summary>
    Task<IReadOnlyList<FehlzeitZeile>> GetForKlasseAsync(
        Guid klasseId, string schuljahr, Halbjahr halbjahr, CancellationToken cancellationToken = default);

    /// <summary>Upserts one child's totals. Throws when <c>unentschuldigt &gt; gesamt</c> or a value is negative.</summary>
    Task SaveAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, int stundenGesamt, int stundenUnentschuldigt,
        CancellationToken cancellationToken = default);

    Task<Fehlzeit?> GetAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, CancellationToken cancellationToken = default);
}

/// <summary>A row in the Fehlzeiten-Tabelle (plan §7.2).</summary>
public sealed record FehlzeitZeile(Guid KindId, string AnzeigeName, int StundenGesamt, int StundenUnentschuldigt);
