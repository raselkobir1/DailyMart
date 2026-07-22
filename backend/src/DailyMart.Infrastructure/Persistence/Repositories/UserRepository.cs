using DailyMart.Application.Auth;
using DailyMart.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(DbContext context) : base(context)
    {
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
}
