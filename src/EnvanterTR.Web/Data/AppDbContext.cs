using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<EnvanterUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<SupplierCatalogEntry> SupplierCatalogEntries => Set<SupplierCatalogEntry>();
    public DbSet<CompanyDepartment> CompanyDepartments => Set<CompanyDepartment>();
    public DbSet<CompanyLocation> CompanyLocations => Set<CompanyLocation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetBrand> AssetBrands => Set<AssetBrand>();
    public DbSet<AssetModel> AssetModels => Set<AssetModel>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetEvent> AssetEvents => Set<AssetEvent>();
    public DbSet<AssetAttachment> AssetAttachments => Set<AssetAttachment>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<AssetMaintenance> AssetMaintenances => Set<AssetMaintenance>();
    public DbSet<Accessory> Accessories => Set<Accessory>();
    public DbSet<AccessoryAssignment> AccessoryAssignments => Set<AccessoryAssignment>();
    public DbSet<AccessoryMovement> AccessoryMovements => Set<AccessoryMovement>();
    public DbSet<AccessoryKit> AccessoryKits => Set<AccessoryKit>();
    public DbSet<AccessoryKitLine> AccessoryKitLines => Set<AccessoryKitLine>();
    public DbSet<AccessoryPurchaseOrder> AccessoryPurchaseOrders => Set<AccessoryPurchaseOrder>();
    public DbSet<AccessoryPurchaseOrderLine> AccessoryPurchaseOrderLines => Set<AccessoryPurchaseOrderLine>();
    public DbSet<Consumable> Consumables => Set<Consumable>();
    public DbSet<ConsumableMovement> ConsumableMovements => Set<ConsumableMovement>();
    public DbSet<InventoryRequest> InventoryRequests => Set<InventoryRequest>();
    public DbSet<SoftwareLicense> SoftwareLicenses => Set<SoftwareLicense>();
    public DbSet<SoftwareLicenseAssignment> SoftwareLicenseAssignments => Set<SoftwareLicenseAssignment>();
    public DbSet<SoftwareLicenseEvent> SoftwareLicenseEvents => Set<SoftwareLicenseEvent>();
    public DbSet<InventoryComponent> InventoryComponents => Set<InventoryComponent>();
    public DbSet<ComponentAllocation> ComponentAllocations => Set<ComponentAllocation>();
    public DbSet<ComponentMovement> ComponentMovements => Set<ComponentMovement>();
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
        modelBuilder.Entity<Company>().Property(x => x.LabelPrintSettingsJson).HasMaxLength(4096);
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
        modelBuilder.Entity<Employee>().Property(x => x.JobTitle).HasMaxLength(160);
        modelBuilder.Entity<Employee>().Property(x => x.Phone).HasMaxLength(40);
        modelBuilder.Entity<Employee>().Property(x => x.Location).HasMaxLength(160);
        modelBuilder.Entity<Employee>().HasIndex(x => new { x.CompanyId, x.LocationCatalogId });
        modelBuilder.Entity<Employee>().HasOne<CompanyLocation>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LocationCatalogId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Employee>().Property(x => x.PrivateNotes).HasMaxLength(4000);
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
        modelBuilder.Entity<AssetAttachment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<EmployeeDocument>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<Accessory>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryAssignment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryKit>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryKitLine>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<Consumable>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<SoftwareLicense>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<InventoryComponent>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<ComponentAllocation>().HasAlternateKey(x => new { x.CompanyId, x.Id });

        modelBuilder.Entity<Employee>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SupplierCatalogEntry>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<Accessory>().HasOne<SupplierCatalogEntry>().WithMany().HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Consumable>().HasOne<SupplierCatalogEntry>().WithMany().HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryComponent>().HasOne<SupplierCatalogEntry>().WithMany().HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SoftwareLicense>().HasOne<SupplierCatalogEntry>().WithMany().HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AssetMaintenance>().HasOne<SupplierCatalogEntry>().WithMany().HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierCatalogEntry>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SupplierCatalogEntry>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<SupplierCatalogEntry>().Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<SupplierCatalogEntry>().Property(x => x.ContactName).HasMaxLength(160);
        modelBuilder.Entity<SupplierCatalogEntry>().Property(x => x.Email).HasMaxLength(254);
        modelBuilder.Entity<SupplierCatalogEntry>().Property(x => x.Phone).HasMaxLength(50);
        modelBuilder.Entity<SupplierCatalogEntry>().Property(x => x.Website).HasMaxLength(500);
        modelBuilder.Entity<SupplierCatalogEntry>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<SupplierCatalogEntry>().HasIndex(x => new { x.CompanyId, x.NormalizedName }).IsUnique();
        modelBuilder.Entity<SupplierCatalogEntry>().HasIndex(x => new { x.CompanyId, x.IsActive, x.Name });
        modelBuilder.Entity<SupplierCatalogEntry>().ToTable("SupplierCatalogEntries");
        modelBuilder.Entity<SupplierCatalogEntry>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<CompanyDepartment>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompanyDepartment>().Property(x => x.Name).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<CompanyDepartment>().Property(x => x.NormalizedName).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<CompanyDepartment>().HasIndex(x => new { x.CompanyId, x.NormalizedName }).IsUnique();
        modelBuilder.Entity<CompanyDepartment>().ToTable("CompanyDepartments");
        modelBuilder.Entity<CompanyDepartment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<CompanyLocation>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompanyLocation>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<CompanyLocation>().Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<CompanyLocation>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<CompanyLocation>().HasOne<CompanyLocation>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ParentLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CompanyLocation>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ManagerEmployeeId }).HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<CompanyLocation>().HasIndex(x => x.CompanyId).IsUnique().HasFilter("\"IsDefault\" = TRUE");
        modelBuilder.Entity<CompanyLocation>().HasIndex(x => new { x.CompanyId, x.NormalizedName })
            .IsUnique().HasFilter("\"ParentLocationId\" IS NULL");
        modelBuilder.Entity<CompanyLocation>().HasIndex(x => new { x.CompanyId, x.ParentLocationId, x.NormalizedName })
            .IsUnique().HasFilter("\"ParentLocationId\" IS NOT NULL");
        modelBuilder.Entity<CompanyLocation>().ToTable("CompanyLocations");
        modelBuilder.Entity<AssetCategory>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetBrand>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetModel>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Asset>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Accessory>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryAssignment>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryMovement>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryKit>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryKitLine>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Consumable>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ConsumableMovement>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Consumable>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Consumable>().Property(x => x.Category).HasMaxLength(120);
        modelBuilder.Entity<Consumable>().Property(x => x.ModelNumber).HasMaxLength(120);
        modelBuilder.Entity<Consumable>().Property(x => x.ItemNumber).HasMaxLength(120);
        modelBuilder.Entity<Consumable>().Property(x => x.Supplier).HasMaxLength(200);
        modelBuilder.Entity<Consumable>().Property(x => x.Location).HasMaxLength(160);
        modelBuilder.Entity<Consumable>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<Consumable>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<Consumable>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<Consumable>().HasIndex(x => new { x.CompanyId, x.Name });
        modelBuilder.Entity<Consumable>().HasIndex(x => new { x.CompanyId, x.Category, x.Location });
        modelBuilder.Entity<Consumable>().ToTable("Consumables", table =>
        {
            table.HasCheckConstraint("CK_Consumables_Quantities", "\"MinimumQuantity\" >= 0 AND \"TotalQuantity\" >= 0");
            table.HasCheckConstraint("CK_Consumables_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
            table.HasCheckConstraint("CK_Consumables_Currency", "char_length(\"Currency\") = 3");
        });
        modelBuilder.Entity<ConsumableMovement>().Property(x => x.MovementType).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<ConsumableMovement>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<ConsumableMovement>().HasIndex(x => new { x.CompanyId, x.ConsumableId, x.OccurredAt });
        modelBuilder.Entity<ConsumableMovement>().ToTable("ConsumableMovements", table =>
        {
            table.HasCheckConstraint("CK_ConsumableMovements_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_ConsumableMovements_Type", "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout')");
        });
        modelBuilder.Entity<ConsumableMovement>().HasOne<Consumable>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ConsumableId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ConsumableMovement>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<ConsumableMovement>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryRequest>().Property(x => x.ItemType).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<InventoryRequest>().Property(x => x.Status).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<InventoryRequest>().Property(x => x.RequestNote).HasMaxLength(1000);
        modelBuilder.Entity<InventoryRequest>().Property(x => x.ReviewNote).HasMaxLength(1000);
        modelBuilder.Entity<InventoryRequest>().HasIndex(x => new { x.CompanyId, x.Status, x.CreatedAt });
        modelBuilder.Entity<InventoryRequest>().HasIndex(x => new { x.CompanyId, x.EmployeeId, x.CreatedAt });
        modelBuilder.Entity<InventoryRequest>().ToTable("InventoryRequests", table =>
        {
            table.HasCheckConstraint("CK_InventoryRequests_ItemType", "\"ItemType\" IN ('Accessory', 'Consumable', 'Asset')");
            table.HasCheckConstraint("CK_InventoryRequests_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
            table.HasCheckConstraint("CK_InventoryRequests_Quantity", "\"Quantity\" BETWEEN 1 AND 100000");
            table.HasCheckConstraint("CK_InventoryRequests_ReviewedAt", "(\"Status\" = 'Pending' AND \"ReviewedAt\" IS NULL) OR (\"Status\" <> 'Pending' AND \"ReviewedAt\" IS NOT NULL)");
        });
        modelBuilder.Entity<InventoryRequest>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InventoryRequest>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryRequest>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<InventoryRequest>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SoftwareLicense>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<SoftwareLicense>().Property(x => x.Publisher).HasMaxLength(160);
        modelBuilder.Entity<SoftwareLicense>().Property(x => x.ProductVersion).HasMaxLength(120);
        modelBuilder.Entity<SoftwareLicense>().Property(x => x.LicenseType).HasMaxLength(32).IsRequired();
        modelBuilder.Entity<SoftwareLicense>().Property(x => x.LicenseReference).HasMaxLength(160);
        modelBuilder.Entity<SoftwareLicense>().Property(x => x.Supplier).HasMaxLength(200);
        modelBuilder.Entity<SoftwareLicense>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<SoftwareLicense>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<SoftwareLicense>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<SoftwareLicense>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SoftwareLicense>().HasIndex(x => new { x.CompanyId, x.Name });
        modelBuilder.Entity<SoftwareLicense>().HasIndex(x => new { x.CompanyId, x.ExpirationDate });
        modelBuilder.Entity<SoftwareLicense>().ToTable("SoftwareLicenses", table =>
        {
            table.HasCheckConstraint("CK_SoftwareLicenses_Seats", "\"Seats\" >= 0");
            table.HasCheckConstraint("CK_SoftwareLicenses_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
            table.HasCheckConstraint("CK_SoftwareLicenses_Currency", "char_length(\"Currency\") = 3");
            table.HasCheckConstraint("CK_SoftwareLicenses_Type", "\"LicenseType\" IN ('Subscription', 'Perpetual', 'Device', 'Site', 'Trial', 'Other')");
        });
        modelBuilder.Entity<SoftwareLicenseAssignment>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasIndex(x => new { x.CompanyId, x.LicenseId, x.RevokedAt });
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasIndex(x => new { x.CompanyId, x.LicenseId, x.EmployeeId })
            .IsUnique().HasFilter("\"RevokedAt\" IS NULL AND \"EmployeeId\" IS NOT NULL");
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasIndex(x => new { x.CompanyId, x.LicenseId, x.AssetId })
            .IsUnique().HasFilter("\"RevokedAt\" IS NULL AND \"AssetId\" IS NOT NULL");
        modelBuilder.Entity<SoftwareLicenseAssignment>().ToTable("SoftwareLicenseAssignments", table =>
        {
            table.HasCheckConstraint("CK_SoftwareLicenseAssignments_Revocation", "\"RevokedAt\" IS NULL OR \"RevokedAt\" >= \"AssignedAt\"");
            table.HasCheckConstraint("CK_SoftwareLicenseAssignments_Target", "(\"EmployeeId\" IS NOT NULL AND \"AssetId\" IS NULL) OR (\"EmployeeId\" IS NULL AND \"AssetId\" IS NOT NULL)");
        });
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasOne<SoftwareLicense>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LicenseId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasOne<Asset>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssetId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SoftwareLicenseEvent>().Property(x => x.EventType).HasMaxLength(32).IsRequired();
        modelBuilder.Entity<SoftwareLicenseEvent>().Property(x => x.Details).HasMaxLength(2000);
        modelBuilder.Entity<SoftwareLicenseEvent>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SoftwareLicenseEvent>().HasIndex(x => new { x.CompanyId, x.LicenseId, x.OccurredAt });
        modelBuilder.Entity<SoftwareLicenseEvent>().HasOne<SoftwareLicense>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LicenseId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SoftwareLicenseEvent>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<SoftwareLicenseEvent>().HasOne<Asset>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssetId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<SoftwareLicenseEvent>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryComponent>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InventoryComponent>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<InventoryComponent>().Property(x => x.Category).HasMaxLength(120);
        modelBuilder.Entity<InventoryComponent>().Property(x => x.ModelNumber).HasMaxLength(120);
        modelBuilder.Entity<InventoryComponent>().Property(x => x.ItemNumber).HasMaxLength(120);
        modelBuilder.Entity<InventoryComponent>().Property(x => x.SerialNumber).HasMaxLength(160);
        modelBuilder.Entity<InventoryComponent>().Property(x => x.Supplier).HasMaxLength(200);
        modelBuilder.Entity<InventoryComponent>().Property(x => x.Location).HasMaxLength(160);
        modelBuilder.Entity<InventoryComponent>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<InventoryComponent>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<InventoryComponent>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<InventoryComponent>().HasIndex(x => new { x.CompanyId, x.Name });
        modelBuilder.Entity<InventoryComponent>().HasIndex(x => new { x.CompanyId, x.SerialNumber });
        modelBuilder.Entity<InventoryComponent>().ToTable("InventoryComponents", table =>
        {
            table.HasCheckConstraint("CK_InventoryComponents_Quantities", "\"MinimumQuantity\" >= 0 AND \"TotalQuantity\" >= 0");
            table.HasCheckConstraint("CK_InventoryComponents_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
            table.HasCheckConstraint("CK_InventoryComponents_Currency", "char_length(\"Currency\") = 3");
        });
        modelBuilder.Entity<ComponentAllocation>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ComponentAllocation>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<ComponentAllocation>().HasIndex(x => new { x.CompanyId, x.ComponentId, x.AssetId, x.LastRemovedAt });
        modelBuilder.Entity<ComponentAllocation>().ToTable("ComponentAllocations", table =>
        {
            table.HasCheckConstraint("CK_ComponentAllocations_Quantities", "\"Quantity\" > 0 AND \"RemovedQuantity\" BETWEEN 0 AND \"Quantity\"");
            table.HasCheckConstraint("CK_ComponentAllocations_RemovedAt", "\"LastRemovedAt\" IS NULL OR \"LastRemovedAt\" >= \"AllocatedAt\"");
        });
        modelBuilder.Entity<ComponentAllocation>().HasOne<InventoryComponent>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ComponentId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ComponentAllocation>().HasOne<Asset>().WithMany().HasForeignKey(x => new { x.CompanyId, x.AssetId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ComponentAllocation>().HasOne<EnvanterUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ComponentMovement>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ComponentMovement>().Property(x => x.MovementType).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<ComponentMovement>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<ComponentMovement>().HasIndex(x => new { x.CompanyId, x.ComponentId, x.OccurredAt });
        modelBuilder.Entity<ComponentMovement>().ToTable("ComponentMovements", table =>
        {
            table.HasCheckConstraint("CK_ComponentMovements_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_ComponentMovements_Type", "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'Checkin')");
        });
        modelBuilder.Entity<ComponentMovement>().HasOne<InventoryComponent>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ComponentId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ComponentMovement>().HasOne<ComponentAllocation>().WithMany().HasForeignKey(x => new { x.CompanyId, x.AllocationId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<ComponentMovement>().HasOne<Asset>().WithMany().HasForeignKey(x => new { x.CompanyId, x.AssetId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<ComponentMovement>().HasOne<EnvanterUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<AccessoryKit>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<AccessoryKit>().Property(x => x.Description).HasMaxLength(1000);
        modelBuilder.Entity<AccessoryKit>().HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        modelBuilder.Entity<AccessoryKit>().HasIndex(x => new { x.CompanyId, x.IsActive, x.Name });
        modelBuilder.Entity<AccessoryKitLine>().HasIndex(x => new { x.CompanyId, x.AccessoryKitId });
        modelBuilder.Entity<AccessoryKitLine>().HasIndex(x => new { x.CompanyId, x.AccessoryId });
        modelBuilder.Entity<AccessoryKitLine>().HasIndex(x => new { x.CompanyId, x.AccessoryKitId, x.AccessoryId }).IsUnique();
        modelBuilder.Entity<AccessoryKitLine>().ToTable("AccessoryKitLines", table =>
            table.HasCheckConstraint("CK_AccessoryKitLines_Quantity", "\"Quantity\" > 0"));
        modelBuilder.Entity<AccessoryKitLine>().HasOne<AccessoryKit>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AccessoryKitId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryKitLine>().HasOne<Accessory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AccessoryId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AccessoryPurchaseOrder>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryPurchaseOrder>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryPurchaseOrder>().Property(x => x.OrderNumber).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AccessoryPurchaseOrder>().Property(x => x.Supplier).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<AccessoryPurchaseOrder>().Property(x => x.Status).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<AccessoryPurchaseOrder>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<AccessoryPurchaseOrder>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<AccessoryPurchaseOrder>().HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
        modelBuilder.Entity<AccessoryPurchaseOrder>().HasIndex(x => new { x.CompanyId, x.Status, x.OrderDate });
        modelBuilder.Entity<AccessoryPurchaseOrder>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AccessoryPurchaseOrder>().HasOne<SupplierCatalogEntry>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<AccessoryPurchaseOrder>().ToTable("AccessoryPurchaseOrders", table =>
        {
            table.HasCheckConstraint("CK_AccessoryPurchaseOrders_Status", "\"Status\" IN ('Ordered', 'PartiallyReceived', 'Received', 'Cancelled')");
            table.HasCheckConstraint("CK_AccessoryPurchaseOrders_Dates", "\"ExpectedOn\" IS NULL OR \"ExpectedOn\" >= \"OrderDate\"");
            table.HasCheckConstraint("CK_AccessoryPurchaseOrders_Currency", "char_length(\"Currency\") = 3");
        });
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().Property(x => x.Notes).HasMaxLength(500);
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().HasIndex(x => new { x.CompanyId, x.PurchaseOrderId });
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().HasIndex(x => new { x.CompanyId, x.AccessoryId });
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().ToTable("AccessoryPurchaseOrderLines", table =>
        {
            table.HasCheckConstraint("CK_AccessoryPurchaseOrderLines_Quantity", "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");
            table.HasCheckConstraint("CK_AccessoryPurchaseOrderLines_Cost", "\"UnitCost\" >= 0");
        });
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().HasOne<AccessoryPurchaseOrder>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryPurchaseOrderLine>().HasOne<Accessory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AccessoryId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.AssetTag }).IsUnique();
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.SerialNumber });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.Status, x.UpdatedAt });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.AssignedEmployeeId });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.Location });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.LocationCatalogId });
        modelBuilder.Entity<Asset>().HasOne<CompanyLocation>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LocationCatalogId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Accessory>().HasIndex(x => new { x.CompanyId, x.LocationCatalogId });
        modelBuilder.Entity<Accessory>().HasOne<CompanyLocation>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LocationCatalogId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Consumable>().HasIndex(x => new { x.CompanyId, x.LocationCatalogId });
        modelBuilder.Entity<Consumable>().HasOne<CompanyLocation>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LocationCatalogId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<InventoryComponent>().HasIndex(x => new { x.CompanyId, x.LocationCatalogId });
        modelBuilder.Entity<InventoryComponent>().HasOne<CompanyLocation>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LocationCatalogId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Asset>().Property(x => x.AssetTag).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<Asset>().Property(x => x.Name).HasMaxLength(240).IsRequired();
        modelBuilder.Entity<Asset>().Property(x => x.Status).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<Asset>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<Asset>().Property(x => x.PurchaseCost).HasPrecision(18, 2);
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.WarrantyExpiresAt });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.NextAuditOn });
        modelBuilder.Entity<Asset>().ToTable("Assets", table =>
        {
            table.HasCheckConstraint("CK_Assets_Status", "\"Status\" IN ('Available', 'Deployed', 'Pending', 'Maintenance', 'Unusable', 'Archived')");
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

        modelBuilder.Entity<AssetAttachment>().Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<AssetAttachment>().Property(x => x.StorageFileName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AssetAttachment>().Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AssetAttachment>().HasIndex(x => new { x.CompanyId, x.AssetId, x.CreatedAt });
        modelBuilder.Entity<AssetAttachment>().ToTable("AssetAttachments", table =>
            table.HasCheckConstraint("CK_AssetAttachments_Length", "\"Length\" BETWEEN 1 AND 10485760"));
        modelBuilder.Entity<AssetAttachment>().HasOne<Asset>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssetId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetAttachment>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EmployeeDocument>().Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<EmployeeDocument>().Property(x => x.StorageFileName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<EmployeeDocument>().Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<EmployeeDocument>().HasIndex(x => new { x.CompanyId, x.EmployeeId, x.CreatedAt });
        modelBuilder.Entity<EmployeeDocument>().ToTable("EmployeeDocuments", table =>
            table.HasCheckConstraint("CK_EmployeeDocuments_Length", "\"Length\" BETWEEN 1 AND 10485760"));
        modelBuilder.Entity<EmployeeDocument>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeDocument>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AssetMaintenance>().HasIndex(x => new { x.CompanyId, x.AssetId, x.CreatedAt });
        modelBuilder.Entity<AssetMaintenance>().HasIndex(x => new { x.CompanyId, x.AssetId, x.Status });
        modelBuilder.Entity<AssetMaintenance>().HasIndex(x => new { x.CompanyId, x.Status, x.PlannedOn });
        modelBuilder.Entity<AssetMaintenance>().HasIndex(x => new { x.CompanyId, x.AssetId })
            .IsUnique().HasFilter("\"Status\" IN ('InProgress', 'Scheduled')");
        modelBuilder.Entity<AssetMaintenance>().Property(x => x.MaintenanceType).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<AssetMaintenance>().Property(x => x.Supplier).HasMaxLength(200);
        modelBuilder.Entity<AssetMaintenance>().Property(x => x.Description).HasMaxLength(1000);
        modelBuilder.Entity<AssetMaintenance>().Property(x => x.Status).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<AssetMaintenance>().Property(x => x.Cost).HasPrecision(18, 2);
        modelBuilder.Entity<AssetMaintenance>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<AssetMaintenance>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<AssetMaintenance>().Property(x => x.CompletionNotes).HasMaxLength(2000);
        modelBuilder.Entity<AssetMaintenance>().ToTable("AssetMaintenances", table =>
        {
            table.HasCheckConstraint("CK_AssetMaintenances_Status", "\"Status\" IN ('Scheduled', 'InProgress', 'Completed', 'Cancelled')");
            table.HasCheckConstraint("CK_AssetMaintenances_Period", "\"CompletedOn\" IS NULL OR \"CompletedOn\" >= \"StartedOn\"");
            table.HasCheckConstraint("CK_AssetMaintenances_Planned", "\"Status\" NOT IN ('Scheduled', 'Cancelled') OR \"PlannedOn\" IS NOT NULL");
            table.HasCheckConstraint("CK_AssetMaintenances_Cost", "\"Cost\" IS NULL OR \"Cost\" >= 0");
            table.HasCheckConstraint("CK_AssetMaintenances_Currency", "char_length(\"Currency\") = 3");
            table.HasCheckConstraint("CK_AssetMaintenances_Completion", "(\"Status\" IN ('Scheduled', 'Cancelled', 'InProgress') AND \"CompletedOn\" IS NULL) OR (\"Status\" = 'Completed' AND \"CompletedOn\" IS NOT NULL)");
        });
        modelBuilder.Entity<AssetMaintenance>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetMaintenance>().HasOne<Asset>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssetId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetMaintenance>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
