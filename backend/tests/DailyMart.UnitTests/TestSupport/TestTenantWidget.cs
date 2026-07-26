using DailyMart.Domain.Common;

namespace DailyMart.UnitTests.TestSupport;

/// <summary>
/// Throwaway TenantOwnedEntity used only to exercise the tenant-isolation query filter convention
/// in isolation - see TestWidget's doc comment for why a stand-in entity is used instead of a real
/// business one.
/// </summary>
public class TestTenantWidget : TenantOwnedEntity
{
    public string Name { get; set; } = string.Empty;
}
