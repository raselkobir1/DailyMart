using DailyMart.Domain.Auth;

namespace DailyMart.Application.Auth;

internal static class UserMappingExtensions
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        Role = user.Role,
        IsActive = user.IsActive
    };
}
