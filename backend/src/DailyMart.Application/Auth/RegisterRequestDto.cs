namespace DailyMart.Application.Auth;

/// <summary>Self-service "sign up your shop" request - creates a brand new Tenant plus its first
/// Admin User in one call, via ITenantProvisioningService. Unlike CreateUserRequestDto (admin-driven,
/// adds a user to an EXISTING tenant), this is the only way a brand new tenant ever gets created.</summary>
public class RegisterRequestDto
{
    public string CompanyName { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;
}
