using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Auditing;

namespace DailyMart.Application.AuditLogs;

public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.Repository<AuditLog>()
            .GetPagedAsync(request, cancellationToken: cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = result.Items.Select(x => x.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }
}
