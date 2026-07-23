using DailyMart.Domain.Rbac;

namespace DailyMart.Application.Rbac;

internal static class RoleMappingExtensions
{
    public static RoleDto ToDto(this Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        IsSystem = role.IsSystem,
        IsDefault = role.IsDefault
    };

    public static Role ToEntity(this RoleRequestDto request) => new()
    {
        Name = request.Name,
        Description = request.Description
    };

    /// <summary>Never touches IsSystem/IsDefault - those are seed-only concepts, not caller-settable.</summary>
    public static void ApplyTo(this RoleRequestDto request, Role role)
    {
        role.Name = request.Name;
        role.Description = request.Description;
    }
}
