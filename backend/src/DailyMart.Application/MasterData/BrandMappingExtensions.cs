using DailyMart.Domain.MasterData;

namespace DailyMart.Application.MasterData;

internal static class BrandMappingExtensions
{
    public static BrandDto ToDto(this Brand brand) => new()
    {
        Id = brand.Id,
        Name = brand.Name,
        Description = brand.Description
    };

    public static Brand ToEntity(this BrandRequestDto request) => new()
    {
        Name = request.Name,
        Description = request.Description
    };
}
