using DailyMart.Domain.Common;

namespace DailyMart.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class, IEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Wraps multiple SaveChangesAsync calls in one DB transaction - for the rare operation that
    /// needs an entity's DB-generated Id (so it can be saved once to obtain it) before building a second,
    /// dependent entity that references that Id, then saving again. Without this, a failure on the second
    /// save leaves the first one committed with no corresponding second row - see
    /// InventoryService.CreateAdjustmentAsync for the concrete case this was added for.</summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation, CancellationToken cancellationToken = default);
}
