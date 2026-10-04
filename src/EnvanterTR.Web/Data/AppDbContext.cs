using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<EnvanterUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetBrand> AssetBrands => Set<AssetBrand>();
    public DbSet<AssetModel> AssetModels => Set<AssetModel>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetEvent> AssetEvents => Set<AssetEvent>();
    public DbSet<Accessory> Accessories => Set<Accessory>();
    public DbSet<AccessoryAssignment> AccessoryAssignments => Set<AccessoryAssignment>();
    public DbSet<AccessoryMovement> AccessoryMovements => Set<AccessoryMovement>();
    public DbSet<CompanyMembership> CompanyMemberships => Set<CompanyMembership>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<PlatformAdministrator> PlatformAdministrators => Set<PlatformAdministrator>();
    public DbSet<LocalLicenseGrant> LocalLicenseGrants => Set<LocalLicenseGrant>();
    public DbSet<PublicSiteContent> PublicSiteContents => Set<PublicSiteContent>();
    public DbSet<PlatformAuditEntry> PlatformAuditEntries => Set<PlatformAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Company>().HasKey(x => x.Id);
        modelBuilder.Entity<Company>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Company>().HasIndex(x => x.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL");
        modelBuilder.Entity<Company>().Property(x => x.Slug).HasMaxLength(80);
        modelBuilder.Entity<Company>().Property(x => x.ContactEmail).HasMaxLength(256);
        modelBuilder.Entity<Company>().Property(x => x.BrandLogoPath).HasMaxLength(500);
        modelBuilder.Entity<Company>().Property(x => x.BrandPrimaryColor).HasMaxLength(7).IsRequired();
        modelBuilder.Entity<SubscriptionPlan>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.Code).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.Description).HasMaxLength(2000);
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.DeploymentKind).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.FeatureFlagsJson).HasMaxLength(8000);
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.MonthlyPrice).HasPrecision(18, 2);
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.AnnualPrice).HasPrecision(18, 2);
        modelBuilder.Entity<SubscriptionPlan>().Property(x => x.OneTimePrice).HasPrecision(18, 2);
        modelBuilder.Entity<SubscriptionPlan>().ToTable("SubscriptionPlans", table =>
        {
            table.HasCheckConstraint("CK_SubscriptionPlans_Prices", "\"MonthlyPrice\" >= 0 AND \"AnnualPrice\" >= 0 AND \"OneTimePrice\" >= 0");
            table.HasCheckConstraint("CK_SubscriptionPlans_Currency", "char_length(\"Currency\") = 3");
            table.HasCheckConstraint("CK_SubscriptionPlans_Seats", "\"MaxLoginUsers\" IS NULL OR \"MaxLoginUsers\" > 0");
            table.HasCheckConstraint("CK_SubscriptionPlans_Assets", "\"MaxAssets\" IS NULL OR \"MaxAssets\" > 0");
            table.HasCheckConstraint("CK_SubscriptionPlans_OfflineGrace", "\"OfflineGraceDays\" BETWEEN 0 AND 365");
        });
        modelBuilder.Entity<TenantSubscription>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TenantSubscription>().HasOne<SubscriptionPlan>().WithMany()
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TenantSubscription>().HasIndex(x => new { x.CompanyId, x.EndsAt });
        modelBuilder.Entity<TenantSubscription>().HasIndex(x => x.CompanyId)
            .IsUnique().HasFilter("\"Status\" IN ('Trial', 'Active')");
        modelBuilder.Entity<TenantSubscription>().Property(x => x.Status).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<TenantSubscription>().Property(x => x.PaymentReference).HasMaxLength(180);
        modelBuilder.Entity<TenantSubscription>().ToTable("TenantSubscriptions", table =>
        {
            table.HasCheckConstraint("CK_TenantSubscriptions_Status", "\"Status\" IN ('Trial', 'Active', 'Expired', 'Cancelled', 'Suspended')");
            table.HasCheckConstraint("CK_TenantSubscriptions_Period", "\"EndsAt\" > \"StartsAt\"");
        });
        modelBuilder.Entity<TenantSubscription>().HasIndex(x => new { x.CompanyId, x.Status, x.EndsAt });
        modelBuilder.Entity<PlatformAdministrator>().HasKey(x => x.UserId);
        modelBuilder.Entity<PlatformAdministrator>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LocalLicenseGrant>().HasIndex(x => x.LicenseKeyHash).IsUnique();
        modelBuilder.Entity<LocalLicenseGrant>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LocalLicenseGrant>().HasOne<SubscriptionPlan>().WithMany()
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LocalLicenseGrant>().Property(x => x.LicenseKeyHash).HasMaxLength(128).IsRequired();
        modelBuilder.Entity<LocalLicenseGrant>().Property(x => x.SignedPayload).HasMaxLength(8000).IsRequired();
        modelBuilder.Entity<PublicSiteContent>().HasIndex(x => x.ContentKey).IsUnique();
        modelBuilder.Entity<PublicSiteContent>().Property(x => x.ContentKey).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<PublicSiteContent>().Property(x => x.Title).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<PublicSiteContent>().Property(x => x.Body).HasMaxLength(12000);
        modelBuilder.Entity<PublicSiteContent>().Property(x => x.MediaPath).HasMaxLength(500);
        modelBuilder.Entity<PlatformAuditEntry>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PlatformAuditEntry>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<PlatformAuditEntry>().HasIndex(x => x.OccurredAt);
        modelBuilder.Entity<PlatformAuditEntry>().Property(x => x.Action).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<PlatformAuditEntry>().Property(x => x.TargetType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<PlatformAuditEntry>().Property(x => x.Details).HasMaxLength(2000);
        modelBuilder.Entity<Employee>().Property(x => x.EmployeeNumber).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<Employee>().Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Employee>().Property(x => x.Email).HasMaxLength(256);
        modelBuilder.Entity<Employee>().Property(x => x.Department).HasMaxLength(160);
        modelBuilder.Entity<AssetCategory>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<AssetBrand>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<AssetModel>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<EnvanterUser>().Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<CompanyMembership>().HasKey(x => new { x.CompanyId, x.UserId });
        modelBuilder.Entity<CompanyMembership>().Property(x => x.RoleKey).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<CompanyMembership>().HasIndex(x => new { x.UserId, x.CompanyId }).IsUnique();
        modelBuilder.Entity<CompanyMembership>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompanyMembership>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Employee>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetCategory>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetBrand>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetModel>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<Asset>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<Accessory>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryAssignment>().HasAlternateKey(x => new { x.CompanyId, x.Id });

        modelBuilder.Entity<Employee>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetCategory>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetBrand>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetModel>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Asset>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Accessory>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryAssignment>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryMovement>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetEvent>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Employee>().HasIndex(x => new { x.CompanyId, x.EmployeeNumber }).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => new { x.CompanyId, x.Email });
        modelBuilder.Entity<Employee>().HasIndex(x => new { x.CompanyId, x.ManagerEmployeeId });
        modelBuilder.Entity<Employee>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ManagerEmployeeId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Employee>().ToTable("Employees", table =>
            table.HasCheckConstraint("CK_Employees_NoSelfManager", "\"ManagerEmployeeId\" IS NULL OR \"ManagerEmployeeId\" <> \"Id\""));
        modelBuilder.Entity<AssetCategory>().HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        modelBuilder.Entity<AssetBrand>().HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        modelBuilder.Entity<AssetModel>().HasIndex(x => new { x.CompanyId, x.BrandId, x.Name }).IsUnique();

        modelBuilder.Entity<Accessory>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Accessory>().Property(x => x.Category).HasMaxLength(120);
        modelBuilder.Entity<Accessory>().Property(x => x.ModelNumber).HasMaxLength(120);
        modelBuilder.Entity<Accessory>().Property(x => x.ItemNumber).HasMaxLength(120);
        modelBuilder.Entity<Accessory>().Property(x => x.Supplier).HasMaxLength(200);
        modelBuilder.Entity<Accessory>().Property(x => x.Location).HasMaxLength(160);
        modelBuilder.Entity<Accessory>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<Accessory>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<Accessory>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<Accessory>().HasIndex(x => new { x.CompanyId, x.Name });
        modelBuilder.Entity<Accessory>().HasIndex(x => new { x.CompanyId, x.Category, x.Location });
        modelBuilder.Entity<Accessory>().ToTable("Accessories", table =>
        {
            table.HasCheckConstraint("CK_Accessories_Quantities", "\"MinimumQuantity\" >= 0 AND \"TotalQuantity\" >= 0");
            table.HasCheckConstraint("CK_Accessories_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
            table.HasCheckConstraint("CK_Accessories_Currency", "char_length(\"Currency\") = 3");
        });
        modelBuilder.Entity<AccessoryAssignment>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<AccessoryAssignment>().Property(x => x.TargetType).HasMaxLength(16).HasDefaultValue("Employee").IsRequired();
        modelBuilder.Entity<AccessoryAssignment>().Property(x => x.Location).HasMaxLength(160);
        modelBuilder.Entity<AccessoryAssignment>().HasIndex(x => new { x.CompanyId, x.AccessoryId, x.EmployeeId });
        modelBuilder.Entity<AccessoryAssignment>().ToTable("AccessoryAssignments", table =>
        {
            table.HasCheckConstraint("CK_AccessoryAssignments_Quantities", "\"Quantity\" > 0 AND \"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\"");
            table.HasCheckConstraint("CK_AccessoryAssignments_Target", "(\"TargetType\" = 'Employee' AND \"EmployeeId\" IS NOT NULL AND \"AssetId\" IS NULL AND \"Location\" IS NULL) OR (\"TargetType\" = 'Asset' AND \"AssetId\" IS NOT NULL AND \"EmployeeId\" IS NULL AND \"Location\" IS NULL) OR (\"TargetType\" = 'Location' AND \"Location\" IS NOT NULL AND \"EmployeeId\" IS NULL AND \"AssetId\" IS NULL)");
        });
        modelBuilder.Entity<AccessoryAssignment>().HasOne<Accessory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AccessoryId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryAssignment>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<AccessoryAssignment>().HasOne<Asset>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssetId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<AccessoryMovement>().Property(x => x.MovementType).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<AccessoryMovement>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<AccessoryMovement>().HasIndex(x => new { x.CompanyId, x.AccessoryId, x.OccurredAt });
        modelBuilder.Entity<AccessoryMovement>().ToTable("AccessoryMovements", table =>
        {
            table.HasCheckConstraint("CK_AccessoryMovements_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_AccessoryMovements_Type", "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'Checkin')");
        });
        modelBuilder.Entity<AccessoryMovement>().HasOne<Accessory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AccessoryId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryMovement>().HasOne<AccessoryAssignment>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssignmentId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AccessoryMovement>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<AccessoryMovement>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.AssetTag }).IsUnique();
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.SerialNumber });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.Status, x.UpdatedAt });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.AssignedEmployeeId });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.Location });
        modelBuilder.Entity<Asset>().Property(x => x.AssetTag).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<Asset>().Property(x => x.Name).HasMaxLength(240).IsRequired();
        modelBuilder.Entity<Asset>().Property(x => x.Status).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<Asset>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<Asset>().Property(x => x.PurchaseCost).HasPrecision(18, 2);
        modelBuilder.Entity<Asset>().ToTable("Assets", table =>
        {
            table.HasCheckConstraint("CK_Assets_Status", "\"Status\" IN ('Available', 'Deployed', 'Maintenance', 'Archived')");
            table.HasCheckConstraint("CK_Assets_AssignmentStatus", "(\"Status\" = 'Deployed' AND \"AssignedEmployeeId\" IS NOT NULL) OR (\"Status\" <> 'Deployed' AND \"AssignedEmployeeId\" IS NULL)");
            table.HasCheckConstraint("CK_Assets_NonnegativeCost", "\"PurchaseCost\" IS NULL OR \"PurchaseCost\" >= 0");
            table.HasCheckConstraint("CK_Assets_Currency", "char_length(\"Currency\") = 3");
        });
        modelBuilder.Entity<Asset>().HasOne<AssetCategory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.CategoryId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetEvent>().HasIndex(x => new { x.CompanyId, x.AssetId, x.OccurredAt });
        modelBuilder.Entity<AssetEvent>().Property(x => x.EventType).HasMaxLength(60).IsRequired();
        modelBuilder.Entity<AssetEvent>().Property(x => x.Details).HasMaxLength(2000);
        modelBuilder.Entity<AssetEvent>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AuditEntry>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AuditEntry>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AuditEntry>().HasIndex(x => new { x.CompanyId, x.OccurredAt });
        modelBuilder.Entity<AuditEntry>().Property(x => x.TargetType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AuditEntry>().Property(x => x.Action).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AuditEntry>().Property(x => x.Details).HasMaxLength(2000);
        modelBuilder.Entity<AssetEvent>().HasOne<Asset>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssetId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
    }
}
