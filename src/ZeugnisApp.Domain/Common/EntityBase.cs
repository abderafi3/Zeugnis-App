namespace ZeugnisApp.Domain.Common;

/// <summary>
/// Base type for all persisted aggregate entities.
/// <para>
/// Uses a client-generated <see cref="Guid"/> key so that entities can be created,
/// wired together in memory and shown in the UI before <c>SaveChanges</c> — the app
/// is autosave-heavy (plan §7.1) and never blocks the user waiting for a database id.
/// </para>
/// </summary>
public abstract class EntityBase
{
    /// <summary>Primary key. Assigned on construction, never reassigned.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp the row was first persisted.</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>UTC timestamp of the last persisted change.</summary>
    public DateTime ModifiedUtc { get; set; }
}
