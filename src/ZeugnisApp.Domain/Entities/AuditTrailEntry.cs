using ZeugnisApp.Domain.Common;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// Backs the undo / Papierkorb feature (plan §4 / §Sprint 11). One row per soft-deleted aggregate,
/// holding a JSON snapshot sufficient to restore it.
/// </summary>
public class AuditTrailEntry : EntityBase
{
    /// <summary>CLR type name of the deleted aggregate (e.g. "Klasse", "Kind", "Zeugnis").</summary>
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    /// <summary>Full JSON snapshot of the aggregate at deletion time.</summary>
    public string SnapshotJson { get; set; } = string.Empty;

    public DateTime DeletedAtUtc { get; set; }

    /// <summary>Short human-readable label for the Papierkorb list ("Klasse E1", "Max Mustermann").</summary>
    public string Bezeichnung { get; set; } = string.Empty;
}
