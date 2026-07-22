using DailyMart.Domain.Common;

namespace DailyMart.Domain.Auth;

/// <summary>
/// The shop's admin account. Single-shop, single-admin for now (CLAUDE.md §1) - Role exists for the
/// BRD's "future role extensibility" requirement, but only "Admin" is enforced today.
/// </summary>
public class User : AuditableEntity
{
    public string Username { get; set; } = string.Empty;

    /// <summary>Hashed via IPasswordHasher&lt;User&gt; - never store or compare plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = "Admin";

    public bool IsActive { get; set; } = true;
}
