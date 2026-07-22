using DailyMart.Application.Common.Interfaces;

namespace DailyMart.UnitTests.TestSupport;

public class FakeCurrentUserService : ICurrentUserService
{
    public string UserName { get; set; } = "test-user";
}
