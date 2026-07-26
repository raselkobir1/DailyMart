using DailyMart.Application.Common.Interfaces;

namespace DailyMart.UnitTests.TestSupport;

public class FakeCurrentTenantService : ICurrentTenantService
{
    public long? TenantId { get; set; }
}
