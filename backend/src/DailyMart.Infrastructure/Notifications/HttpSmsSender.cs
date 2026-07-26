using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace DailyMart.Infrastructure.Notifications;

/// <summary>Generic REST SMS gateway client - builds a GET request from Sms.UrlTemplate (see SmsOptions'
/// doc comment), rather than a specific vendor SDK, since which gateway a shop actually uses is an ops
/// decision made per deployment, not something this codebase should hardcode.</summary>
public class HttpSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;

    public HttpSmsSender(HttpClient httpClient, IOptions<SmsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var url = _options.UrlTemplate
            .Replace("{apiKey}", Uri.EscapeDataString(_options.ApiKey))
            .Replace("{senderId}", Uri.EscapeDataString(_options.SenderId))
            .Replace("{number}", Uri.EscapeDataString(phoneNumber))
            .Replace("{message}", Uri.EscapeDataString(message));

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
