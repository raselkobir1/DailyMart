namespace DailyMart.Domain.Common;

/// <summary>
/// Base type for every entity in the system. Carries the shared audit columns
/// (CreatedAt/CreatedBy/UpdatedAt/UpdatedBy/IsDeleted) described in CLAUDE.md §4-5.
/// </summary>
public abstract class AuditableEntity : IEntity
{
    public long Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}
