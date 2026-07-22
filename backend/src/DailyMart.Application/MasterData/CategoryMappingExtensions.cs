using DailyMart.Domain.MasterData;

namespace DailyMart.Application.MasterData;

internal static class CategoryMappingExtensions
{
    public static CategoryDto ToDto(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description
    };

    public static Category ToEntity(this CategoryRequestDto request) => new()
    {
        Name = request.Name,
        Description = request.Description
    };
}
