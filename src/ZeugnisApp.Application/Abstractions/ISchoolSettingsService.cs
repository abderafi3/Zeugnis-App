using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>Reads and updates the singleton <see cref="SchoolSettings"/> row (plan §Sprint 1).</summary>
public interface ISchoolSettingsService
{
    /// <summary>Returns the settings row, creating it with defaults on first call.</summary>
    Task<SchoolSettings> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SchoolSettings settings, CancellationToken cancellationToken = default);
}
