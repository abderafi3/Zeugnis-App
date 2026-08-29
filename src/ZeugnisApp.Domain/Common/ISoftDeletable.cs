namespace ZeugnisApp.Domain.Common;

/// <summary>
/// Marks an entity that participates in the soft-delete / Papierkorb feature (plan §Sprint 11).
/// The infrastructure layer applies a global query filter on <see cref="DeletedUtc"/> so that
/// "deleted" rows disappear from normal queries but remain recoverable for the retention window.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>UTC timestamp the row was soft-deleted, or <c>null</c> while it is live.</summary>
    DateTime? DeletedUtc { get; set; }
}
