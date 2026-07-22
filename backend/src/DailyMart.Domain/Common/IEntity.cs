namespace DailyMart.Domain.Common;

/// <summary>
/// Minimal marker for anything the generic repository can manage. Split out from
/// <see cref="AuditableEntity"/> because not every persisted type carries audit columns -
/// <see cref="Auditing.AuditLog"/> is append-only and implements this directly instead.
/// </summary>
public interface IEntity
{
    long Id { get; set; }
}
