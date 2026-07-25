using System.Linq.Expressions;
using DailyMart.Application.AuditLogs;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Auditing;
using Moq;

namespace DailyMart.UnitTests.AuditLogs;

public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _repository = new();
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        _sut = new AuditLogService(_repository.Object);
    }

    private static AuditLog MakeLog(
        long id = 1,
        string entityName = "Product",
        string entityId = "42",
        AuditAction action = AuditAction.Updated,
        string performedBy = "alice",
        DateTimeOffset? performedAt = null) => new()
    {
        Id = id,
        EntityName = entityName,
        EntityId = entityId,
        Action = action,
        OldValues = "{\"Price\":10}",
        NewValues = "{\"Price\":12}",
        ChangedColumns = "[\"Price\"]",
        PerformedBy = performedBy,
        PerformedAt = performedAt ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    };

    private Expression<Func<AuditLog, bool>>? _capturedPredicate;

    private void ArrangePredicateCapture()
    {
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<AuditLog, bool>>?, CancellationToken>((_, predicate, _) => _capturedPredicate = predicate)
            .ReturnsAsync(new PagedResult<AuditLog> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });
    }

    [Fact]
    public async Task GetPagedAsync_maps_domain_entities_to_dtos_and_preserves_paging_metadata()
    {
        var log = MakeLog();
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog> { Items = [log], TotalCount = 1, PageNumber = 1, PageSize = 20 });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal(log.Id, dto.Id);
        Assert.Equal("Product", dto.EntityName);
        Assert.Equal("Updated", dto.Action);
        Assert.Equal("alice", dto.PerformedBy);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_defaults_to_PerformedAt_descending_when_no_sort_is_requested()
    {
        PagedRequest? capturedRequest = null;
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<AuditLog, bool>>?, CancellationToken>((r, _, _) => capturedRequest = r)
            .ReturnsAsync(new PagedResult<AuditLog> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });

        await _sut.GetPagedAsync(new PagedRequest());

        Assert.Equal(nameof(AuditLog.PerformedAt), capturedRequest!.SortBy);
        Assert.True(capturedRequest.SortDescending);
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_entityName_action_and_date_range()
    {
        ArrangePredicateCapture();
        await _sut.GetPagedAsync(
            new PagedRequest(),
            entityName: "Product",
            action: AuditAction.Updated,
            fromDate: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            toDate: new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));

        var isMatch = _capturedPredicate!.Compile();

        Assert.True(isMatch(MakeLog(entityName: "Product", action: AuditAction.Updated, performedAt: new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero))));
        Assert.False(isMatch(MakeLog(entityName: "Sale", action: AuditAction.Updated))); // wrong entity
        Assert.False(isMatch(MakeLog(entityName: "Product", action: AuditAction.Deleted))); // wrong action
        Assert.False(isMatch(MakeLog(entityName: "Product", action: AuditAction.Updated, performedAt: new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)))); // outside range
    }

    [Fact]
    public async Task GetPagedAsync_search_term_matches_PerformedBy_or_EntityId()
    {
        ArrangePredicateCapture();
        await _sut.GetPagedAsync(new PagedRequest { SearchTerm = "alice" });

        var isMatch = _capturedPredicate!.Compile();

        Assert.True(isMatch(MakeLog(performedBy: "alice")));
        Assert.True(isMatch(MakeLog(performedBy: "bob", entityId: "alice-corp"))); // matches EntityId instead
        Assert.False(isMatch(MakeLog(performedBy: "bob", entityId: "99")));
    }

    [Fact]
    public async Task GetPagedAsync_with_no_filters_matches_everything()
    {
        ArrangePredicateCapture();
        await _sut.GetPagedAsync(new PagedRequest());

        var isMatch = _capturedPredicate!.Compile();

        Assert.True(isMatch(MakeLog(entityName: "AnythingAtAll", action: AuditAction.Deleted)));
    }

    [Fact]
    public async Task GetEntityNamesAsync_delegates_to_the_repository()
    {
        _repository.Setup(r => r.GetDistinctEntityNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Customer", "Product", "Sale"]);

        var result = await _sut.GetEntityNamesAsync();

        Assert.Equal(["Customer", "Product", "Sale"], result);
    }
}
