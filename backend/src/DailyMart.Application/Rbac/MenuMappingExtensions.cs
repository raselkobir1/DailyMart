using DailyMart.Domain.Rbac;

namespace DailyMart.Application.Rbac;

internal static class MenuMappingExtensions
{
    public static MenuDto ToDto(this Menu menu) => new()
    {
        Id = menu.Id,
        Key = menu.Key,
        Label = menu.Label,
        Route = menu.Route,
        Icon = menu.Icon,
        SortOrder = menu.SortOrder,
        ParentId = menu.ParentId
    };

    public static Menu ToEntity(this CreateMenuRequestDto request) => new()
    {
        Key = request.Key,
        Label = request.Label,
        Route = request.Route,
        Icon = request.Icon,
        SortOrder = request.SortOrder,
        ParentId = request.ParentId
    };

    /// <summary>Never touches Key - write-once, see Menu's doc comment.</summary>
    public static void ApplyTo(this MenuRequestDto request, Menu menu)
    {
        menu.Label = request.Label;
        menu.Route = request.Route;
        menu.Icon = request.Icon;
        menu.SortOrder = request.SortOrder;
        menu.ParentId = request.ParentId;
    }
}
