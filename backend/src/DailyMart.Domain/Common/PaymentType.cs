namespace DailyMart.Domain.Common;

/// <summary>Shared by Purchase (Module 7) and POS Sales (Module 9) - the BRD calls out identical
/// Cash/Credit/Partial payment types for both, so this isn't duplicated per module.</summary>
public enum PaymentType
{
    Cash,
    Credit,
    Partial
}
