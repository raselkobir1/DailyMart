using DailyMart.Domain.MasterData;

namespace DailyMart.Application.MasterData;

internal static class UnitMappingExtensions
{
    public static UnitDto ToDto(this Unit unit) => new()
    {
        Id = unit.Id,
        Name = unit.Name,
        Symbol = unit.Symbol
    };

    public static Unit ToEntity(this UnitRequestDto request) => new()
    {
        Name = request.Name,
        Symbol = request.Symbol
    };
}
