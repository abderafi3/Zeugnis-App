using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.Infrastructure;

/// <summary>Production <see cref="IClock"/> backed by the OS clock.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
