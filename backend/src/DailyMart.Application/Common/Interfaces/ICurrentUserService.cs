namespace DailyMart.Application.Common.Interfaces;

/// <summary>
/// Resolves who is performing the current operation, for CreatedBy/UpdatedBy/PerformedBy stamping.
/// Implemented in Infrastructure against HttpContext; Module 1 (Authentication) supplies the real
/// JWT-authenticated user. Until then it falls back to a "system" identity.
/// </summary>
public interface ICurrentUserService
{
    string UserName { get; }
}
