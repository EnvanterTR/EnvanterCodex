using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EnvanterTR.Web.Data;

/// <summary>Validates persisted mappings after provider/Identity model finalization.</summary>
public static class FinalizedTenantModelScope
{
    private static readonly HashSet<Type> SharedTypes =
    [
        typeof(EnvanterUser), typeof(IdentityRole<Guid>), typeof(IdentityRoleClaim<Guid>),
        typeof(IdentityUserClaim<Guid>), typeof(IdentityUserLogin<Guid>),
        typeof(IdentityUserRole<Guid>), typeof(IdentityUserToken<Guid>), typeof(IdentityUserPasskey<Guid>),
        typeof(PlatformAdministrator), typeof(PlatformAuditRetentionPolicy),
        typeof(PlatformFollowUpEmailDigest), typeof(PublicSiteContent),
        typeof(SubscriptionCoupon), typeof(SubscriptionPlan)
    ];

    // Pre-provisioning and platform-only records may have no company yet.
    private static readonly HashSet<Type> ConditionalTypes =
    [typeof(CompanyCheckoutOrder), typeof(PlatformDemoRequest), typeof(PlatformAuditEntry), typeof(UserSession)];

    public static void Validate(IModel model)
    {
        // Resolve the provider's actual mappings, not OnModelCreating's transient types.
        _ = model.GetRelationalModel();
        var checkedRoots = new HashSet<IEntityType>();
        foreach (var entity in model.GetEntityTypes())
        {
            if (!entity.GetTableMappings().Any() && !entity.GetViewMappings().Any()) continue;
            var root = entity;
            while (root.FindOwnership() is { } ownership) root = ownership.PrincipalEntityType;
            root = root.GetRootType();
            if (!checkedRoots.Add(root) || SharedTypes.Contains(root.ClrType)) continue;

            var name = root.ClrType == typeof(Company) ? nameof(Company.Id) : "CompanyId";
            var property = root.FindProperty(name);
            if (property is null || (property.ClrType != typeof(Guid) && property.ClrType != typeof(Guid?)))
                throw new InvalidOperationException($"Persisted entity '{root.Name}' requires a Guid {name} or an explicit shared scope.");
            if (property.IsNullable && !ConditionalTypes.Contains(root.ClrType))
                throw new InvalidOperationException($"Persisted entity '{root.Name}' requires a non-null company identifier.");
            if (root.FindDeclaredQueryFilter("TenantScope") is null)
                throw new InvalidOperationException($"Persisted entity '{root.Name}' requires the TenantScope query filter.");
        }
    }
}
