using DailyMart.Application.Auth;
using DailyMart.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(DbContext context) : base(context)
    {
    }

    /// <summary>Deliberately bypasses the automatic tenant filter: this is the login lookup itself, so
    /// there is no tenant context yet to filter by - finding which tenant the username belongs to is
    /// exactly what this query is for. Username is global (not per-tenant) precisely so this lookup
    /// stays unambiguous - see UserConfiguration's doc comment.</summary>
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        Entities.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, cancellationToken);

    /// <summary>Also bypasses the tenant filter, for the same reason - Username's uniqueness is global,
    /// so the pre-insert check has to be global too, matching the database constraint it's guarding
    /// against (a tenant-scoped check here would let two different tenants both pass the app-level
    /// check for the same username, then fail at SaveChanges with a generic 409 instead of this
    /// service-level BusinessRuleException).</summary>
    public Task<bool> ExistsByUsernameAsync(string username, long? excludeId, CancellationToken cancellationToken = default) =>
        Entities.IgnoreQueryFilters().AnyAsync(
            u => !u.IsDeleted && u.Username.ToLower() == username.ToLower() && (excludeId == null || u.Id != excludeId),
            cancellationToken);
}
