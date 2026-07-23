using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Auth;

/// <summary>Admin user-management (list/create/update/delete other accounts) - distinct from IAuthService,
/// which is about the caller's own session (login/refresh/logout/change-password). There's still only one
/// way in (no self-registration, CLAUDE.md §1); this is what lets that first admin create more accounts.</summary>
public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Throws BusinessRuleException if Username is already taken or Role doesn't match any
    /// existing Role's name.</summary>
    Task<UserDto> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Throws BusinessRuleException if Role doesn't match any existing Role's name.</summary>
    Task<UserDto> UpdateAsync(long id, UpdateUserRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>currentUserId comes from the caller's own JWT claims (same pattern as
    /// IAuthService.ChangePasswordAsync) - throws BusinessRuleException if it matches id, so an admin can
    /// never delete the account they're currently signed in as.</summary>
    Task DeleteAsync(long id, long currentUserId, CancellationToken cancellationToken = default);
}
