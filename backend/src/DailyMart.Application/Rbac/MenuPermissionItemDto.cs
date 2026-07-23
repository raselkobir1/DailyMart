namespace DailyMart.Application.Rbac;

public class MenuPermissionItemDto
{
    public long MenuId { get; init; }

    public bool CanView { get; init; }

    public bool CanCreate { get; init; }

    public bool CanEdit { get; init; }

    public bool CanDelete { get; init; }
}
