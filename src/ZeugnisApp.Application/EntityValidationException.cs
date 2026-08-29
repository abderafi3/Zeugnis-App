namespace ZeugnisApp.Application;

/// <summary>
/// Thrown by services when an entity fails validation. Carries field-keyed messages so a
/// view model can show them inline (plan §7.1 — guided, not gatekept: the UI validates before
/// calling, this is the defensive backstop).
/// </summary>
public sealed class EntityValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Die Eingaben sind unvollständig oder ungültig.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
