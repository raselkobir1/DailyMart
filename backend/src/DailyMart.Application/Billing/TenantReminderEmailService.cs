using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using DailyMart.Application.Tenancy;
using Microsoft.Extensions.Options;

namespace DailyMart.Application.Billing;

public class TenantReminderEmailService : ITenantReminderEmailService
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITenantContactLookupService _tenantContactLookupService;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;

    public TenantReminderEmailService(
        ISubscriptionService subscriptionService,
        ITenantContactLookupService tenantContactLookupService,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions)
    {
        _subscriptionService = subscriptionService;
        _tenantContactLookupService = tenantContactLookupService;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
    }

    public async Task<TenantReminderEmailResultDto> SendReminderAsync(
        long tenantId, CancellationToken cancellationToken = default)
    {
        if (!_emailOptions.Enabled)
        {
            throw new BusinessRuleException("Email sending is not configured for the platform.");
        }

        var subscription = await _subscriptionService.GetByTenantIdAsync(tenantId, cancellationToken);

        string reminderType;
        string subject;
        string body;
        if (subscription.IsOverdue)
        {
            reminderType = "Overdue";
            (subject, body) = BuildOverdueEmail(subscription);
        }
        else if (subscription.IsFree)
        {
            reminderType = "Free";
            (subject, body) = BuildFreeEmail(subscription);
        }
        else
        {
            throw new BusinessRuleException(
                $"{subscription.TenantName} is not overdue and not on the Free plan - there's nothing to remind them about.");
        }

        var shopEmail = await _tenantContactLookupService.GetShopEmailAsync(tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(shopEmail))
        {
            throw new BusinessRuleException($"{subscription.TenantName} has no contact email on file yet.");
        }

        await _emailSender.SendAsync(shopEmail, subscription.TenantName, subject, body, cancellationToken);

        return new TenantReminderEmailResultDto { SentTo = shopEmail, ReminderType = reminderType };
    }

    private static (string Subject, string Body) BuildOverdueEmail(TenantSubscriptionDto subscription)
    {
        const string subject = "Action needed: your DailyMart subscription payment is overdue";

        var sinceText = subscription.CurrentPeriodEnd is { } end ? $" since {end:d}" : string.Empty;
        var body = $"""
            <h2>Hi {subscription.TenantName},</h2>
            <p>Your <strong>{subscription.PlanName}</strong> plan payment is overdue{sinceText}.</p>
            <p>Please arrange payment to keep your DailyMart account in good standing.</p>
            <p>If you've already paid, please disregard this message - it may take a short time to reflect.</p>
            """;

        return (subject, body);
    }

    private static (string Subject, string Body) BuildFreeEmail(TenantSubscriptionDto subscription)
    {
        const string subject = "Get more from DailyMart - consider upgrading from Free";

        var body = $"""
            <h2>Hi {subscription.TenantName},</h2>
            <p>You're currently on the <strong>Free</strong> plan.</p>
            <p>If DailyMart has been working well for your business, consider upgrading to a paid plan to
            support continued development and get priority support.</p>
            <p>Reply to this email or reach out to us if you'd like to discuss upgrading.</p>
            """;

        return (subject, body);
    }
}
