using Microsoft.AspNetCore.Identity;

namespace EnvanterTR.Web.Data;

public sealed class EnvanterUser : IdentityUser<Guid>
{
    public EnvanterUser() => Id = Guid.NewGuid();
    public required string DisplayName { get; set; }
    // Nullable for accounts created before this field existed; do not backfill an invented registration date.
    public DateTimeOffset? AccountCreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public bool PlatformAdmin { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ImpersonatorUserId { get; set; }
    public Guid? ImpersonatorSessionId { get; set; }
    public string? ImpersonatorSecurityStampHash { get; set; }
    public string? ImpersonationReturnTokenHash { get; set; }
    public bool? ImpersonatorRememberMe { get; set; }
    public string? ImpersonationReason { get; set; }
    public DateTimeOffset? ImpersonationEndedAt { get; set; }
}

public sealed class CompanyMembership
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public required string RoleKey { get; set; }
    public string LocationAccessMode { get; set; } = "all";
    public string CompanyUnitAccessMode { get; set; } = "all";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CompanyMembershipLocationAccess
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public Guid LocationId { get; set; }
}

public sealed class CompanyMembershipCompanyUnitAccess
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyUnitId { get; set; }
}

public sealed class CompanyCustomRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string RoleKey { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string PermissionsJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }
}
