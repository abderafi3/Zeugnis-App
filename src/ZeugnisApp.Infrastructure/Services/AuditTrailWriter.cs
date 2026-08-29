using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Services;

/// <summary>Records a soft-delete in the audit trail so the Papierkorb can list and restore it.</summary>
internal static class AuditTrailWriter
{
    public static void Record(ZeugnisDbContext db, string entityType, Guid entityId, string bezeichnung, IClock clock)
    {
        db.AuditTrail.Add(new AuditTrailEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Bezeichnung = bezeichnung,
            DeletedAtUtc = clock.UtcNow,
            SnapshotJson = string.Empty, // soft-delete keeps the rows; restore just clears DeletedUtc
        });
    }
}
