using DailyMart.Domain.Common;

namespace DailyMart.Domain.MasterData;

public class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
