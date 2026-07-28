using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Tenancy;

namespace DailyMart.Application.Tenancy;

public class SupportChatService : ISupportChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupportChatRealtimeNotifier _realtimeNotifier;

    public SupportChatService(IUnitOfWork unitOfWork, ISupportChatRealtimeNotifier realtimeNotifier)
    {
        _unitOfWork = unitOfWork;
        _realtimeNotifier = realtimeNotifier;
    }

    private IRepository<SupportMessage> Repository => _unitOfWork.Repository<SupportMessage>();

    public Task<SupportMessageDto> SendFromTenantAsync(
        long tenantId, string message, CancellationToken cancellationToken = default) =>
        SendAsync(tenantId, fromPlatformAdmin: false, message, cancellationToken);

    public Task<SupportMessageDto> SendFromPlatformAdminAsync(
        long tenantId, string message, CancellationToken cancellationToken = default) =>
        SendAsync(tenantId, fromPlatformAdmin: true, message, cancellationToken);

    private async Task<SupportMessageDto> SendAsync(
        long tenantId, bool fromPlatformAdmin, string message, CancellationToken cancellationToken)
    {
        var entity = new SupportMessage
        {
            TenantId = tenantId,
            FromPlatformAdmin = fromPlatformAdmin,
            Message = message,
            // The sender's own side is trivially "read" - only the other side starts unread.
            IsReadByTenant = !fromPlatformAdmin,
            IsReadByPlatformAdmin = fromPlatformAdmin
        };

        await Repository.AddAsync(entity, cancellationToken);
        // CreatedBy is stamped here by AuditingSaveChangesInterceptor from whichever identity - tenant
        // user or platform admin - is authenticated for this request; see SupportMessage's doc comment.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = ToDto(entity);
        await _realtimeNotifier.NotifyNewMessageAsync(tenantId, dto, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyList<SupportMessageDto>> GetConversationAsync(
        long tenantId, int take, CancellationToken cancellationToken = default)
    {
        var page = await Repository.GetPagedAsync(
            new PagedRequest { PageNumber = 1, PageSize = take },
            m => m.TenantId == tenantId,
            cancellationToken);

        // GetPagedAsync's unsorted default is most-recent-first (OrderByDescending(Id)) - reversed here
        // so the chat panel can render top-to-bottom in the order the conversation actually happened.
        return page.Items.AsEnumerable().Reverse().Select(ToDto).ToList();
    }

    public async Task<int> GetUnreadCountForTenantAsync(long tenantId, CancellationToken cancellationToken = default) =>
        (await Repository.FindAsync(
            m => m.TenantId == tenantId && m.FromPlatformAdmin && !m.IsReadByTenant, cancellationToken)).Count;

    public async Task<int> GetUnreadCountForPlatformAdminAsync(
        long tenantId, CancellationToken cancellationToken = default) =>
        (await Repository.FindAsync(
            m => m.TenantId == tenantId && !m.FromPlatformAdmin && !m.IsReadByPlatformAdmin, cancellationToken)).Count;

    public async Task<Dictionary<long, int>> GetUnreadCountsForPlatformAdminAsync(
        IEnumerable<long> tenantIds, CancellationToken cancellationToken = default)
    {
        var ids = tenantIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, int>();
        }

        var unread = await Repository.FindAsync(
            m => ids.Contains(m.TenantId) && !m.FromPlatformAdmin && !m.IsReadByPlatformAdmin, cancellationToken);

        return unread.GroupBy(m => m.TenantId).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task MarkReadByTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var unread = await Repository.FindAsync(
            m => m.TenantId == tenantId && m.FromPlatformAdmin && !m.IsReadByTenant, cancellationToken);
        if (unread.Count == 0)
        {
            return;
        }

        foreach (var message in unread)
        {
            message.IsReadByTenant = true;
            Repository.Update(message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkReadByPlatformAdminAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var unread = await Repository.FindAsync(
            m => m.TenantId == tenantId && !m.FromPlatformAdmin && !m.IsReadByPlatformAdmin, cancellationToken);
        if (unread.Count == 0)
        {
            return;
        }

        foreach (var message in unread)
        {
            message.IsReadByPlatformAdmin = true;
            Repository.Update(message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static SupportMessageDto ToDto(SupportMessage message) => new()
    {
        Id = message.Id,
        TenantId = message.TenantId,
        FromPlatformAdmin = message.FromPlatformAdmin,
        SenderName = message.CreatedBy,
        Message = message.Message,
        CreatedAt = message.CreatedAt
    };
}
