using Microsoft.AspNetCore.Identity;

namespace EnvanterTR.Web.Data;

public sealed class EnvanterUser : IdentityUser<Guid>
{
    public EnvanterUser() => Id = Guid.NewGuid();
    public required string DisplayName { get; set; }
}

public sealed class CompanyMembership
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string RoleKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
