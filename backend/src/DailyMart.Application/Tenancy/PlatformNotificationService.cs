using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DailyMart.Application.Tenancy;

public class PlatformNotificationService : IPlatformNotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly IPlatformRealtimeNotifier _platformRealtimeNotifier;
    private readonly IPlatformNotificationStore _platformNotificationStore;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<PlatformNotificationService> _logger;

    public PlatformNotificationService(
        IEmailSender emailSender,
        IPlatformRealtimeNotifier platformRealtimeNotifier,
        IPlatformNotificationStore platformNotificationStore,
        IOptions<EmailOptions> emailOptions,
        ILogger<PlatformNotificationService> logger)
    {
        _emailSender = emailSender;
        _platformRealtimeNotifier = platformRealtimeNotifier;
        _platformNotificationStore = platformNotificationStore;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    /// <summary>Three independent channels - a missing/failing email never skips the live push or the
    /// persisted record, and vice versa. The persisted record is recorded first (and is the one channel
    /// that basically can't meaningfully fail short of the database itself being down) so its real Id is
    /// available to include in the live push - see IPlatformRealtimeNotifier's doc comment on why that
    /// matters. All three are individually best-effort, so this method as a whole never throws - a new
    /// tenant's registration must never fail because of anything in here.</summary>
    public async Task NotifyNewTenantSignupAsync(
        long tenantId, string tenantName, string adminUsername, CancellationToken cancellationToken = default)
    {
        var notification = await RecordAsync(tenantId, tenantName, adminUsername, cancellationToken);

        await NotifyByEmailAsync(tenantId, tenantName, adminUsername, cancellationToken);

        await _platformRealtimeNotifier.NotifyNewTenantSignupAsync(
            notification?.Id ?? 0, tenantId, tenantName, adminUsername,
            notification?.CreatedAt ?? DateTimeOffset.UtcNow, cancellationToken);
    }

    private async Task<PlatformNotificationDto?> RecordAsync(
        long tenantId, string tenantName, string adminUsername, CancellationToken cancellationToken)
    {
        try
        {
            return await _platformNotificationStore.RecordNewTenantSignupAsync(
                tenantId, tenantName, adminUsername, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not persist the platform-owner signup notification for tenant {TenantId} ({TenantName}) - " +
                "registration still succeeded; the live push (if anyone's connected) still goes out.",
                tenantId, tenantName);
            return null;
        }
    }

    private async Task NotifyByEmailAsync(
        long tenantId, string tenantName, string adminUsername, CancellationToken cancellationToken)
    {
        if (!_emailOptions.Enabled || string.IsNullOrWhiteSpace(_emailOptions.PlatformNotificationAddress))
        {
            _logger.LogInformation(
                "Skipped the platform-owner signup notification email for tenant {TenantId} ({TenantName}) - " +
                "email notifications aren't configured.", tenantId, tenantName);
            return;
        }

        try
        {
            var subject = $"New DailyMart signup: {tenantName}";
            var body = $"""
                <h2>A new company signed up</h2>
                <p><strong>Company:</strong> {tenantName} (tenant #{tenantId})</p>
                <p><strong>Admin username:</strong> {adminUsername}</p>
                <p>They start on the Free plan - review them in the platform panel if needed.</p>
                """;

            await _emailSender.SendAsync(
                _emailOptions.PlatformNotificationAddress!, "Platform Owner", subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            // Best-effort only - a new tenant's registration must never fail because the platform owner
            // couldn't be notified about it, so any send failure here is logged and swallowed rather than
            // propagated to the caller (contrast with SaleInvoiceDeliveryService/TenantReminderEmailService,
            // both explicit user-triggered sends where a failure should surface to whoever clicked send).
            _logger.LogWarning(ex,
                "Could not send the platform-owner signup notification email for tenant {TenantId} ({TenantName}) - " +
                "registration still succeeded.", tenantId, tenantName);
        }
    }
}
