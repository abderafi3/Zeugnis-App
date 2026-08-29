namespace ZeugnisApp.Application.Abstractions;

/// <summary>Recover or purge soft-deleted classes, pupils and report cards (plan §Sprint 11).</summary>
public interface IPapierkorbService
{
    Task<IReadOnlyList<PapierkorbEintrag>> GetEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Un-deletes the aggregate (and its soft-deleted children) and removes the trash entry.</summary>
    Task RestoreAsync(Guid auditEntryId, CancellationToken cancellationToken = default);

    /// <summary>Hard-deletes everything whose trash entry is older than the retention window.</summary>
    Task<int> PurgeExpiredAsync(int retentionDays, CancellationToken cancellationToken = default);
}

/// <summary>One row in the Papierkorb list.</summary>
public sealed record PapierkorbEintrag(Guid AuditEntryId, string EntityType, string Bezeichnung, DateTime DeletedAtUtc);

/// <summary>Well-known <see cref="PapierkorbEintrag.EntityType"/> values.</summary>
public static class PapierkorbTypen
{
    public const string Klasse = "Klasse";
    public const string Kind = "Kind";
    public const string Zeugnis = "Zeugnis";
    public const string Floskel = "Floskel";
    public const string Formular = "Formular";
}
