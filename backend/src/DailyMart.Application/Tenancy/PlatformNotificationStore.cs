using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Tenancy;

namespace DailyMart.Application.Tenancy;

public class PlatformNotificationStore : IPlatformNotificationStore
{
    private readonly IUnitOfWork _unitOfWork;

    public PlatformNotificationStore(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<PlatformNotification> Repository => _unitOfWork.Repository<PlatformNotification>();

    public async Task<PlatformNotificationDto> RecordNewTenantSignupAsync(
        long tenantId, string tenantName, string adminUsername, CancellationToken cancellationToken = default)
    {
        var notification = new PlatformNotification
        {
            Type = "NewTenantSignup",
            TenantId = tenantId,
            TenantName = tenantName,
            AdminUsername = adminUsername,
            IsRead = false
        };

        await Repository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(notification);
    }

    public async Task<IReadOnlyList<PlatformNotificationDto>> GetRecentAsync(
        int take, CancellationToken cancellationToken = default)
    {
        // No SortBy given - GetPagedAsync's default (OrderByDescending(Id)) is exactly "most recently
        // created first" for an identity column, same as every other unsorted list in this codebase.
        var page = await Repository.GetPagedAsync(
            new Common.Models.PagedRequest { PageNumber = 1, PageSize = take },
            cancellationToken: cancellationToken);

        return page.Items.Select(ToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        (await Repository.FindAsync(n => !n.IsRead, cancellationToken)).Count;

    public async Task MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await Repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        Repository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static PlatformNotificationDto ToDto(PlatformNotification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        TenantId = notification.TenantId,
        TenantName = notification.TenantName,
        AdminUsername = notification.AdminUsername,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };
}
