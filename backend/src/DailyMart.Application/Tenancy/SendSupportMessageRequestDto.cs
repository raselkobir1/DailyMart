namespace DailyMart.Application.Tenancy;

/// <summary>Shared by both directions - SupportChatController (tenant side) and PlatformTenantsController's
/// {id}/support-chat routes (platform side) - the shape of "send a message" is identical either way, only
/// which service method it calls differs.</summary>
public class SendSupportMessageRequestDto
{
    public string Message { get; init; } = string.Empty;
}
