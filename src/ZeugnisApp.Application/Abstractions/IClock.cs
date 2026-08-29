namespace ZeugnisApp.Application.Abstractions;

/// <summary>Testable wall-clock. Injected everywhere instead of <see cref="DateTime.UtcNow"/>.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
