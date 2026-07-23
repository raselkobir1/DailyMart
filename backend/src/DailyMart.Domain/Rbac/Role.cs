using DailyMart.Domain.Common;

namespace DailyMart.Domain.Rbac;

/// <summary>
/// A named permission set assignable to a User (User.Role is a plain string, not a foreign key here -
/// matches the JWT's ClaimTypes.Role claim, which only ever needs the name, not the row). System roles
/// (IsSystem) can't be deleted or renamed - "Admin" is seeded this way so there's always at least one role
/// nobody can lock everyone else out of. IsDefault marks the role future self-registration would assign,
/// even though this app has no self-registration today (CLAUDE.md §1) - kept for parity with the RBAC
/// model this was ported from, not used yet.
/// </summary>
public class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public bool IsDefault { get; set; }
}
