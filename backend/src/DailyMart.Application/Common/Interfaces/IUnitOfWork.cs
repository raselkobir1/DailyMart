using DailyMart.Domain.Common;

namespace DailyMart.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class, IEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
