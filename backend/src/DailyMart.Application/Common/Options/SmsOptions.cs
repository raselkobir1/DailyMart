namespace DailyMart.Application.Common.Options;

/// <summary>Ops-managed SMS gateway config - deliberately generic (a URL template) rather than one vendor's
/// SDK, since simple REST-based SMS resellers (the common case for a local gateway) all follow the same
/// shape: a GET request with the recipient/message/credentials as query parameters. UrlTemplate's
/// "{apiKey}", "{senderId}", "{number}", "{message}" placeholders are substituted (URL-encoded) by
/// HttpSmsSender before the request is sent.</summary>
public class SmsOptions
{
    public const string SectionName = "Sms";

    public bool Enabled { get; set; }

    public string UrlTemplate { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string SenderId { get; set; } = string.Empty;
}
