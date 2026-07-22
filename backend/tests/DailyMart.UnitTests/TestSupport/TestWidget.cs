using DailyMart.Domain.Common;

namespace DailyMart.UnitTests.TestSupport;

/// <summary>
/// Throwaway AuditableEntity used only to exercise Module 0's generic mechanisms (interceptor,
/// soft-delete filter, repository). Module 0 ships no real business entity of its own to test against -
/// Product/Customer/etc. don't exist until later modules - so this stands in for "some future entity".
/// </summary>
public class TestWidget : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
}
