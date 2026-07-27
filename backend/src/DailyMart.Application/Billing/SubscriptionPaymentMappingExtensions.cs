using DailyMart.Domain.Billing;

namespace DailyMart.Application.Billing;

internal static class SubscriptionPaymentMappingExtensions
{
    public static SubscriptionPaymentDto ToDto(this SubscriptionPayment payment) => new()
    {
        Id = payment.Id,
        Amount = payment.Amount,
        PeriodStart = payment.PeriodStart,
        PeriodEnd = payment.PeriodEnd,
        Method = payment.Method,
        Notes = payment.Notes,
        CreatedAt = payment.CreatedAt,
        CreatedBy = payment.CreatedBy
    };
}
