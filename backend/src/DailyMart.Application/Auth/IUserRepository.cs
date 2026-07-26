using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Auth;

namespace DailyMart.Application.Auth;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> ExistsByUsernameAsync(string username, long? excludeId, CancellationToken cancellationToken = default);
}
