using System.Collections.Concurrent;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Common;
using DailyMart.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public UnitOfWork(DbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : class, IEntity =>
        (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new Repository<T>(_context));

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
