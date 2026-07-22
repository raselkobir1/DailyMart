using System.Linq.Expressions;
using System.Reflection;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : class, IEntity
{
    protected readonly DbSet<T> Entities;

    public Repository(DbContext context)
    {
        Entities = context.Set<T>();
    }

    public IQueryable<T> Query() => Entities.AsQueryable();

    public Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Entities.ToListAsync(cancellationToken);

    public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        Entities.Where(predicate).ToListAsync(cancellationToken);

    public async Task<PagedResult<T>> GetPagedAsync(
        PagedRequest request,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = Entities.AsQueryable();

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = string.IsNullOrWhiteSpace(request.SortBy)
            ? query.OrderByDescending(e => e.Id)
            : ApplySort(query, request.SortBy, request.SortDescending);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        Entities.AnyAsync(predicate, cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Entities.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Entities.Update(entity);

    public void Remove(T entity) => Entities.Remove(entity);

    /// <summary>Builds `OrderBy(propertyName)`/`OrderByDescending(propertyName)` via reflection so callers
    /// can sort by any column name coming from a query string, without a dynamic-LINQ package dependency.
    /// Falls back to ordering by Id if the property name doesn't exist on T.</summary>
    private static IQueryable<T> ApplySort(IQueryable<T> query, string propertyName, bool descending)
    {
        var property = typeof(T).GetProperty(
            propertyName,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property is null)
        {
            return descending ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id);
        }

        var parameter = Expression.Parameter(typeof(T), "e");
        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.PropertyType);

        return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda })!;
    }
}
