using DailyMart.Application.AuditLogs;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Auditing;
using Moq;

namespace DailyMart.UnitTests.AuditLogs;

public class AuditLogServiceTests
{
    [Fact]
    public async Task GetPagedAsync_maps_domain_entities_to_dtos_and_preserves_paging_metadata()
    {
        var log = new AuditLog
        {
            Id = 1,
            EntityName = "Product",
            EntityId = "42",
            Action = AuditAction.Updated,
            OldValues = "{\"Price\":10}",
            NewValues = "{\"Price\":12}",
            ChangedColumns = "[\"Price\"]",
            PerformedBy = "alice",
            PerformedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var repositoryMock = new Mock<IRepository<AuditLog>>();
        repositoryMock
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>
            {
                Items = [log],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Repository<AuditLog>()).Returns(repositoryMock.Object);

        var service = new AuditLogService(unitOfWorkMock.Object);

        var result = await service.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal(log.Id, dto.Id);
        Assert.Equal("Product", dto.EntityName);
        Assert.Equal("Updated", dto.Action);
        Assert.Equal("alice", dto.PerformedBy);
        Assert.Equal(1, result.TotalCount);
    }
}
