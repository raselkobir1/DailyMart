using DailyMart.Domain.Common;

namespace DailyMart.Domain.Auditing;

/// <summary>
/// Append-only record of a create/update/delete/sale event on any tracked entity.
/// Does not inherit <see cref="AuditableEntity"/> - a log entry is never itself
/// soft-deleted or updated, so it doesn't need those columns. Implements <see cref="IEntity"/>
/// directly so it can still go through the generic repository/unit-of-work.
/// </summary>
public class AuditLog : IEntity
{
    public long Id { get; set; }

    public string EntityName { get; set; } = string.Empty;

    /// <summary>Stored as text so it works regardless of the audited entity's key type.</summary>
    public string EntityId { get; set; } = string.Empty;

    public AuditAction Action { get; set; }

    /// <summary>JSON snapshot before the change. Null on Create.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot after the change. Null on Delete.</summary>
    public string? NewValues { get; set; }

    /// <summary>JSON array of column names that changed. Populated on Update only.</summary>
    public string? ChangedColumns { get; set; }

    public string PerformedBy { get; set; } = string.Empty;

    public DateTimeOffset PerformedAt { get; set; }
}
