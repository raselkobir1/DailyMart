using DailyMart.Domain.Auditing;

namespace DailyMart.Application.AuditLogs;

internal static class AuditLogMappingExtensions
{
    public static AuditLogDto ToDto(this AuditLog log) => new()
    {
        Id = log.Id,
        EntityName = log.EntityName,
        EntityId = log.EntityId,
        Action = log.Action.ToString(),
        OldValues = log.OldValues,
        NewValues = log.NewValues,
        ChangedColumns = log.ChangedColumns,
        PerformedBy = log.PerformedBy,
        PerformedAt = log.PerformedAt
    };
}
