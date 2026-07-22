using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Infrastructure.Persistence.Repositories;
using DailyMart.UnitTests.TestSupport;

namespace DailyMart.UnitTests.Persistence;

public class RepositoryTests
{
    [Fact]
    public async Task GetPagedAsync_without_SortBy_orders_by_Id_descending_and_paginates()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new Repository<TestWidget>(context);

        for (var i = 1; i <= 5; i++)
        {
            await repository.AddAsync(new TestWidget { Name = $"Widget {i}" });
        }
        await context.SaveChangesAsync();

        var page = await repository.GetPagedAsync(new PagedRequest { PageNumber = 1, PageSize = 2 });

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal("Widget 5", page.Items[0].Name);
        Assert.Equal("Widget 4", page.Items[1].Name);
    }

    [Fact]
    public async Task GetPagedAsync_sorts_ascending_by_a_named_property()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new Repository<TestWidget>(context);

        await repository.AddAsync(new TestWidget { Name = "Charlie" });
        await repository.AddAsync(new TestWidget { Name = "Alpha" });
        await repository.AddAsync(new TestWidget { Name = "Bravo" });
        await context.SaveChangesAsync();

        var page = await repository.GetPagedAsync(new PagedRequest { PageNumber = 1, PageSize = 10, SortBy = "Name" });

        Assert.Equal(["Alpha", "Bravo", "Charlie"], page.Items.Select(w => w.Name));
    }

    [Fact]
    public async Task GetPagedAsync_falls_back_to_Id_when_SortBy_does_not_match_a_real_property()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new Repository<TestWidget>(context);

        var first = new TestWidget { Name = "First" };
        var second = new TestWidget { Name = "Second" };
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        await context.SaveChangesAsync();

        var page = await repository.GetPagedAsync(new PagedRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "NotARealProperty",
            SortDescending = true
        });

        Assert.Equal(second.Id, page.Items[0].Id);
        Assert.Equal(first.Id, page.Items[1].Id);
    }

    [Fact]
    public async Task Remove_then_SaveChanges_is_a_soft_delete_via_the_interceptor()
    {
        await using var context = TestDbContextFactory.Create();
        IUnitOfWork unitOfWork = new DailyMart.Infrastructure.Persistence.UnitOfWork(context);
        var repository = unitOfWork.Repository<TestWidget>();

        var widget = new TestWidget { Name = "Soft deletable" };
        await repository.AddAsync(widget);
        await unitOfWork.SaveChangesAsync();

        repository.Remove(widget);
        await unitOfWork.SaveChangesAsync();

        Assert.False(await repository.ExistsAsync(w => w.Id == widget.Id));
    }
}
