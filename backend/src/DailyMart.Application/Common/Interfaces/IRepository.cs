using System.Linq.Expressions;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Common;

namespace DailyMart.Application.Common.Interfaces;

/// <summary>
/// Generic CRUD + query surface shared by every module's repositories. Module-specific repositories
/// (e.g. ISupplierRepository) extend this with custom queries rather than duplicating this contract.
/// </summary>
public interface IRepository<T> where T : class, IEntity
{
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<PagedResult<T>> GetPagedAsync(
        PagedRequest request,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    /// <summary>Marks the entity for removal; the audit interceptor converts this into a soft delete.</summary>
    void Remove(T entity);

    /// <summary>Escape hatch for module-specific repositories to compose further LINQ queries.</summary>
    IQueryable<T> Query();
}
