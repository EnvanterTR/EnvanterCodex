using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<EnvanterUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyApiToken> CompanyApiTokens => Set<CompanyApiToken>();
    public DbSet<SupplierCatalogEntry> SupplierCatalogEntries => Set<SupplierCatalogEntry>();
    public DbSet<CompanyDepartment> CompanyDepartments => Set<CompanyDepartment>();
    public DbSet<CompanyLocation> CompanyLocations => Set<CompanyLocation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetBrand> AssetBrands => Set<AssetBrand>();
    public DbSet<AssetModel> AssetModels => Set<AssetModel>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetReservation> AssetReservations => Set<AssetReservation>();
    public DbSet<AssetCustomFieldDefinition> AssetCustomFieldDefinitions => Set<AssetCustomFieldDefinition>();
    public DbSet<SavedAssetSearch> SavedAssetSearches => Set<SavedAssetSearch>();
    public DbSet<SavedAssetImportMapping> SavedAssetImportMappings => Set<SavedAssetImportMapping>();
    public DbSet<CustomAssetReport> CustomAssetReports => Set<CustomAssetReport>();
    public DbSet<DashboardPreference> DashboardPreferences => Set<DashboardPreference>();
    public DbSet<OperationalNotificationPreference> OperationalNotificationPreferences => Set<OperationalNotificationPreference>();
    public DbSet<OperationalNotificationEmailDigest> OperationalNotificationEmailDigests => Set<OperationalNotificationEmailDigest>();
    public DbSet<OperationalNotificationState> OperationalNotificationStates => Set<OperationalNotificationState>();
    public DbSet<AssetEvent> AssetEvents => Set<AssetEvent>();
    public DbSet<AssetAttachment> AssetAttachments => Set<AssetAttachment>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<AssetMaintenance> AssetMaintenances => Set<AssetMaintenance>();
    public DbSet<AssetMaintenanceAttachment> AssetMaintenanceAttachments => Set<AssetMaintenanceAttachment>();
    public DbSet<Accessory> Accessories => Set<Accessory>();
    public DbSet<AccessoryCategory> AccessoryCategories => Set<AccessoryCategory>();
    public DbSet<AccessoryModel> AccessoryModels => Set<AccessoryModel>();
    public DbSet<AccessoryAssignment> AccessoryAssignments => Set<AccessoryAssignment>();
    public DbSet<AccessoryMovement> AccessoryMovements => Set<AccessoryMovement>();
    public DbSet<AccessoryKit> AccessoryKits => Set<AccessoryKit>();
    public DbSet<AccessoryKitLine> AccessoryKitLines => Set<AccessoryKitLine>();
    public DbSet<AccessoryPurchaseOrder> AccessoryPurchaseOrders => Set<AccessoryPurchaseOrder>();
    public DbSet<AccessoryPurchaseOrderLine> AccessoryPurchaseOrderLines => Set<AccessoryPurchaseOrderLine>();
    public DbSet<StockPurchaseOrder> StockPurchaseOrders => Set<StockPurchaseOrder>();
    public DbSet<StockPurchaseOrderLine> StockPurchaseOrderLines => Set<StockPurchaseOrderLine>();
    public DbSet<AssetPurchaseOrder> AssetPurchaseOrders => Set<AssetPurchaseOrder>();
    public DbSet<AssetPurchaseOrderLine> AssetPurchaseOrderLines => Set<AssetPurchaseOrderLine>();
    public DbSet<AssetPurchaseOrderAttachment> AssetPurchaseOrderAttachments => Set<AssetPurchaseOrderAttachment>();
    public DbSet<StockPurchaseOrderAttachment> StockPurchaseOrderAttachments => Set<StockPurchaseOrderAttachment>();
    public DbSet<AccessoryPurchaseOrderAttachment> AccessoryPurchaseOrderAttachments => Set<AccessoryPurchaseOrderAttachment>();
    public DbSet<Consumable> Consumables => Set<Consumable>();
    public DbSet<ConsumableMovement> ConsumableMovements => Set<ConsumableMovement>();
    public DbSet<InventoryRequest> InventoryRequests => Set<InventoryRequest>();
    public DbSet<SoftwareLicense> SoftwareLicenses => Set<SoftwareLicense>();
    public DbSet<SoftwareLicenseAttachment> SoftwareLicenseAttachments => Set<SoftwareLicenseAttachment>();
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
        modelBuilder.Entity<CompanyApiToken>().HasKey(x => x.Id);
        modelBuilder.Entity<CompanyApiToken>().Property(x => x.Name).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<CompanyApiToken>().Property(x => x.TokenPrefix).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<CompanyApiToken>().Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        modelBuilder.Entity<CompanyApiToken>().Property(x => x.ScopesJson).HasMaxLength(300).IsRequired();
        modelBuilder.Entity<CompanyApiToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<CompanyApiToken>().HasIndex(x => new { x.CompanyId, x.UserId, x.CreatedAt });
        modelBuilder.Entity<CompanyApiToken>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompanyApiToken>().HasOne<CompanyMembership>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.UserId }).HasPrincipalKey(x => new { x.CompanyId, x.UserId })
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompanyApiToken>().ToTable("CompanyApiTokens", table =>
            table.HasCheckConstraint("CK_CompanyApiTokens_Expiry", "\"ExpiresAt\" IS NULL OR \"ExpiresAt\" > \"CreatedAt\""));
        modelBuilder.Entity<Company>().Property(x => x.Slug).HasMaxLength(80);
        modelBuilder.Entity<Company>().Property(x => x.ContactEmail).HasMaxLength(256);
        modelBuilder.Entity<Company>().Property(x => x.BrandLogoPath).HasMaxLength(500);
        modelBuilder.Entity<Company>().Property(x => x.AssetStatusLabelsJson).HasMaxLength(3000);
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
        modelBuilder.Entity<AssetBrand>().Property(x => x.Website).HasMaxLength(300);
        modelBuilder.Entity<AssetBrand>().Property(x => x.SupportUrl).HasMaxLength(300);
        modelBuilder.Entity<AssetBrand>().Property(x => x.WarrantyUrl).HasMaxLength(300);
        modelBuilder.Entity<AssetBrand>().Property(x => x.SupportPhone).HasMaxLength(60);
        modelBuilder.Entity<AssetBrand>().Property(x => x.SupportEmail).HasMaxLength(254);
        modelBuilder.Entity<AssetBrand>().Property(x => x.LogoFileName).HasMaxLength(40);
        modelBuilder.Entity<AssetModel>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<AssetModel>().Property(x => x.PhotoFileName).HasMaxLength(40);
        modelBuilder.Entity<AssetModel>().Property(x => x.DepreciationMinimum).HasPrecision(18, 2);
        modelBuilder.Entity<AssetModel>().Property(x => x.DepreciationMinimumType).HasMaxLength(12).IsRequired();
        modelBuilder.Entity<AssetModel>().Property(x => x.DepreciationMethod).HasMaxLength(16).IsRequired();
        modelBuilder.Entity<AssetModel>().ToTable("AssetModels", table =>
        {
            table.HasCheckConstraint("CK_AssetModels_DepreciationMonths", "\"DepreciationMonths\" IS NULL OR \"DepreciationMonths\" BETWEEN 1 AND 3600");
            table.HasCheckConstraint("CK_AssetModels_DepreciationMinimum", "\"DepreciationMinimum\" IS NULL OR \"DepreciationMinimum\" >= 0");
            table.HasCheckConstraint("CK_AssetModels_DepreciationMinimumType", "\"DepreciationMinimumType\" IN ('amount', 'percent')");
            table.HasCheckConstraint("CK_AssetModels_DepreciationMethod", "\"DepreciationMethod\" IN ('linear', 'half_1', 'half_2')");
        });
        modelBuilder.Entity<AssetCustomFieldDefinition>().Property(x => x.Key).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<AssetCustomFieldDefinition>().Property(x => x.Name).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<AssetCustomFieldDefinition>().Property(x => x.FieldType).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<AssetCustomFieldDefinition>().Property(x => x.OptionsJson).HasMaxLength(8000);
        modelBuilder.Entity<AssetCustomFieldDefinition>().HasIndex(x => new { x.CompanyId, x.Key }).IsUnique();
        modelBuilder.Entity<AssetCustomFieldDefinition>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetCustomFieldDefinition>().ToTable("AssetCustomFieldDefinitions", table =>
            table.HasCheckConstraint("CK_AssetCustomFieldDefinitions_Type", "\"FieldType\" IN ('text', 'number', 'date', 'checkbox', 'select')"));
        modelBuilder.Entity<Asset>().Property(x => x.CustomFieldsJson).HasMaxLength(16000);
        modelBuilder.Entity<EnvanterUser>().Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<CompanyMembership>().HasKey(x => new { x.CompanyId, x.UserId });
        modelBuilder.Entity<CompanyMembership>().Property(x => x.RoleKey).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<CompanyMembership>().HasIndex(x => new { x.UserId, x.CompanyId }).IsUnique();
        modelBuilder.Entity<CompanyMembership>().HasIndex(x => new { x.CompanyId, x.EmployeeId })
            .IsUnique().HasFilter("\"EmployeeId\" IS NOT NULL");
        modelBuilder.Entity<CompanyMembership>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompanyMembership>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompanyMembership>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        modelBuilder.Entity<SavedAssetSearch>().Property(x => x.Name).HasMaxLength(60).IsRequired();
        modelBuilder.Entity<SavedAssetSearch>().Property(x => x.NormalizedName).HasMaxLength(60).IsRequired();
        modelBuilder.Entity<SavedAssetSearch>().Property(x => x.FiltersJson).HasMaxLength(4096).IsRequired();
        modelBuilder.Entity<SavedAssetSearch>().HasIndex(x => new { x.CompanyId, x.UserId, x.NormalizedName }).IsUnique();
        modelBuilder.Entity<SavedAssetSearch>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SavedAssetSearch>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SavedAssetImportMapping>().Property(x => x.Name).HasMaxLength(60).IsRequired();
        modelBuilder.Entity<SavedAssetImportMapping>().Property(x => x.NormalizedName).HasMaxLength(60).IsRequired();
        modelBuilder.Entity<SavedAssetImportMapping>().Property(x => x.Family).HasMaxLength(32).HasDefaultValue("assets").IsRequired();
        modelBuilder.Entity<SavedAssetImportMapping>().Property(x => x.MappingJson).HasMaxLength(8192).IsRequired();
        modelBuilder.Entity<SavedAssetImportMapping>().HasIndex(x => new { x.CompanyId, x.UserId, x.Family, x.NormalizedName }).IsUnique();
        modelBuilder.Entity<SavedAssetImportMapping>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SavedAssetImportMapping>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomAssetReport>().Property(x => x.Name).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<CustomAssetReport>().Property(x => x.NormalizedName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<CustomAssetReport>().Property(x => x.DefinitionJson).HasMaxLength(8192).IsRequired();
        modelBuilder.Entity<CustomAssetReport>().HasIndex(x => new { x.CompanyId, x.UserId, x.NormalizedName }).IsUnique();
        modelBuilder.Entity<CustomAssetReport>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CustomAssetReport>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DashboardPreference>().HasKey(x => new { x.CompanyId, x.UserId });
        modelBuilder.Entity<DashboardPreference>().Property(x => x.VisibleWidgetsJson).HasMaxLength(512).IsRequired();
        modelBuilder.Entity<DashboardPreference>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DashboardPreference>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OperationalNotificationPreference>().HasKey(x => new { x.CompanyId, x.UserId });
        modelBuilder.Entity<OperationalNotificationPreference>().Property(x => x.EnabledKindsJson).HasMaxLength(256).IsRequired();
        modelBuilder.Entity<OperationalNotificationPreference>().Property(x => x.EmailEnabledKindsJson).HasMaxLength(256).IsRequired();
        modelBuilder.Entity<OperationalNotificationPreference>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OperationalNotificationPreference>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OperationalNotificationEmailDigest>().HasKey(x => new { x.CompanyId, x.UserId, x.DeliveryDate });
        modelBuilder.Entity<OperationalNotificationEmailDigest>().Property(x => x.Status).HasMaxLength(16).IsRequired();
        modelBuilder.Entity<OperationalNotificationEmailDigest>().Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        modelBuilder.Entity<OperationalNotificationEmailDigest>().Property(x => x.LastError).HasMaxLength(80);
        modelBuilder.Entity<OperationalNotificationEmailDigest>().ToTable(table =>
            table.HasCheckConstraint("CK_OperationalNotificationEmailDigests_Status", "\"Status\" IN ('Sending', 'Sent', 'Failed')"));
        modelBuilder.Entity<OperationalNotificationEmailDigest>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OperationalNotificationEmailDigest>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OperationalNotificationState>().HasKey(x => new { x.CompanyId, x.UserId, x.Kind, x.RecordId });
        modelBuilder.Entity<OperationalNotificationState>().Property(x => x.Kind).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<OperationalNotificationState>().HasIndex(x => new { x.CompanyId, x.UserId, x.DismissedAt });
        modelBuilder.Entity<OperationalNotificationState>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OperationalNotificationState>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Employee>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetCategory>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetBrand>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetModel>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetModel>().HasOne<AssetCategory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.CategoryId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Asset>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetReservation>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetAttachment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<EmployeeDocument>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<Accessory>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryModel>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryAssignment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryKit>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryKitLine>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<Consumable>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<SoftwareLicense>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<SoftwareLicenseAttachment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<InventoryComponent>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<ComponentAllocation>().HasAlternateKey(x => new { x.CompanyId, x.Id });

        modelBuilder.Entity<Employee>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SupplierCatalogEntry>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<Accessory>().HasOne<SupplierCatalogEntry>().WithMany().HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AccessoryModel>().HasOne<AssetBrand>().WithMany().HasForeignKey(x => new { x.CompanyId, x.BrandId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Accessory>().HasOne<AssetBrand>().WithMany().HasForeignKey(x => new { x.CompanyId, x.BrandId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Accessory>().HasOne<AccessoryModel>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ModelId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
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
        modelBuilder.Entity<SupplierCatalogEntry>().Property(x => x.LogoFileName).HasMaxLength(40);
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
        modelBuilder.Entity<CompanyDepartment>().HasOne<CompanyDepartment>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ParentDepartmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<CompanyDepartment>().HasOne<CompanyLocation>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LocationCatalogId }).HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<CompanyDepartment>().HasIndex(x => new { x.CompanyId, x.ParentDepartmentId });
        modelBuilder.Entity<CompanyDepartment>().HasIndex(x => new { x.CompanyId, x.LocationCatalogId });
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
        modelBuilder.Entity<AccessoryModel>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
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
            table.HasCheckConstraint("CK_Consumables_WarrantyAfterPurchase", "\"WarrantyExpiresAt\" IS NULL OR \"PurchaseDate\" IS NULL OR \"WarrantyExpiresAt\" >= \"PurchaseDate\"");
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
        modelBuilder.Entity<SoftwareLicenseAttachment>().Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<SoftwareLicenseAttachment>().Property(x => x.StorageFileName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<SoftwareLicenseAttachment>().Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<SoftwareLicenseAttachment>().Property(x => x.EulaVersion).HasMaxLength(80);
        modelBuilder.Entity<SoftwareLicenseAttachment>().Property(x => x.Sha256).HasMaxLength(64);
        modelBuilder.Entity<SoftwareLicenseAttachment>().HasAlternateKey(x => new { x.CompanyId, x.LicenseId, x.Id });
        modelBuilder.Entity<SoftwareLicenseAttachment>().HasIndex(x => new { x.CompanyId, x.LicenseId, x.CreatedAt });
        modelBuilder.Entity<SoftwareLicenseAttachment>().HasIndex(x => new { x.CompanyId, x.LicenseId })
            .IsUnique().HasFilter("\"IsCurrentEula\" = TRUE");
        modelBuilder.Entity<SoftwareLicenseAttachment>().ToTable("SoftwareLicenseAttachments", table =>
        {
            table.HasCheckConstraint("CK_SoftwareLicenseAttachments_Length", "\"Length\" BETWEEN 1 AND 10485760");
            table.HasCheckConstraint("CK_SoftwareLicenseAttachments_Eula", "(\"IsEulaDocument\" = TRUE AND \"EulaVersion\" IS NOT NULL AND char_length(\"EulaVersion\") BETWEEN 1 AND 80 AND \"Sha256\" IS NOT NULL) OR (\"IsEulaDocument\" = FALSE AND \"IsCurrentEula\" = FALSE AND \"EulaVersion\" IS NULL)");
            table.HasCheckConstraint("CK_SoftwareLicenseAttachments_CurrentEula", "\"IsCurrentEula\" = FALSE OR \"IsEulaDocument\" = TRUE");
        });
        modelBuilder.Entity<SoftwareLicenseAttachment>().HasOne<SoftwareLicense>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LicenseId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SoftwareLicenseAttachment>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);
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
        modelBuilder.Entity<SoftwareLicenseAssignment>().Property(x => x.EulaVersion).HasMaxLength(80);
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
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.EulaAcceptedByUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SoftwareLicenseAssignment>().HasOne<SoftwareLicenseAttachment>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LicenseId, x.EulaAttachmentId })
            .HasPrincipalKey(x => new { x.CompanyId, x.LicenseId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
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
            table.HasCheckConstraint("CK_InventoryComponents_WarrantyAfterPurchase", "\"WarrantyExpiresAt\" IS NULL OR \"PurchaseDate\" IS NULL OR \"WarrantyExpiresAt\" >= \"PurchaseDate\"");
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
        modelBuilder.Entity<AssetModel>().HasIndex(x => new { x.CompanyId, x.CategoryId });

        modelBuilder.Entity<Accessory>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<AccessoryCategory>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<AccessoryCategory>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryCategory>().HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        modelBuilder.Entity<AccessoryCategory>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Accessory>().Property(x => x.Category).HasMaxLength(120);
        modelBuilder.Entity<Accessory>().Property(x => x.ModelNumber).HasMaxLength(120);
        modelBuilder.Entity<Accessory>().Property(x => x.ItemNumber).HasMaxLength(120);
        modelBuilder.Entity<Accessory>().Property(x => x.Supplier).HasMaxLength(200);
        modelBuilder.Entity<Accessory>().Property(x => x.Location).HasMaxLength(160);
        modelBuilder.Entity<Accessory>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<Accessory>().Property(x => x.PhotoFileName).HasMaxLength(80);
        modelBuilder.Entity<Accessory>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<Accessory>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<Accessory>().HasIndex(x => new { x.CompanyId, x.Name });
        modelBuilder.Entity<AccessoryModel>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<AccessoryModel>().Property(x => x.PhotoFileName).HasMaxLength(40);
        modelBuilder.Entity<AccessoryModel>().HasIndex(x => new { x.CompanyId, x.BrandId, x.Name }).IsUnique();
        modelBuilder.Entity<Accessory>().HasIndex(x => new { x.CompanyId, x.ModelId });
        modelBuilder.Entity<Accessory>().HasIndex(x => new { x.CompanyId, x.BrandId });
        modelBuilder.Entity<Accessory>().HasIndex(x => new { x.CompanyId, x.CategoryId, x.Location });
        modelBuilder.Entity<Accessory>().HasOne<AccessoryCategory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.CategoryId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Accessory>().ToTable("Accessories", table =>
        {
            table.HasCheckConstraint("CK_Accessories_Quantities", "\"MinimumQuantity\" >= 0 AND \"TotalQuantity\" >= 0");
            table.HasCheckConstraint("CK_Accessories_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
            table.HasCheckConstraint("CK_Accessories_Currency", "char_length(\"Currency\") = 3");
            table.HasCheckConstraint("CK_Accessories_WarrantyAfterPurchase", "\"WarrantyExpiresAt\" IS NULL OR \"PurchaseDate\" IS NULL OR \"WarrantyExpiresAt\" >= \"PurchaseDate\"");
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
        modelBuilder.Entity<AccessoryPurchaseOrder>().Property(x => x.TaxRatePercent).HasPrecision(5, 2);
        modelBuilder.Entity<AccessoryPurchaseOrder>().Property(x => x.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<AccessoryPurchaseOrder>().Property(x => x.FreightAmount).HasPrecision(18, 2);
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
            table.HasCheckConstraint("CK_AccessoryPurchaseOrders_FinancialAdjustments", "\"TaxRatePercent\" BETWEEN 0 AND 100 AND \"DiscountAmount\" >= 0 AND \"FreightAmount\" >= 0");
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

        modelBuilder.Entity<StockPurchaseOrder>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<StockPurchaseOrder>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockPurchaseOrder>().Property(x => x.OrderNumber).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<StockPurchaseOrder>().Property(x => x.Supplier).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<StockPurchaseOrder>().Property(x => x.Status).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<StockPurchaseOrder>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<StockPurchaseOrder>().Property(x => x.TaxRatePercent).HasPrecision(5, 2);
        modelBuilder.Entity<StockPurchaseOrder>().Property(x => x.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<StockPurchaseOrder>().Property(x => x.FreightAmount).HasPrecision(18, 2);
        modelBuilder.Entity<StockPurchaseOrder>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<StockPurchaseOrder>().HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
        modelBuilder.Entity<StockPurchaseOrder>().HasIndex(x => new { x.CompanyId, x.Status, x.OrderDate });
        modelBuilder.Entity<StockPurchaseOrder>().HasOne<EnvanterUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<StockPurchaseOrder>().HasOne<SupplierCatalogEntry>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<StockPurchaseOrder>().ToTable("StockPurchaseOrders", table =>
        {
            table.HasCheckConstraint("CK_StockPurchaseOrders_Status", "\"Status\" IN ('Ordered', 'PartiallyReceived', 'Received', 'Cancelled')");
            table.HasCheckConstraint("CK_StockPurchaseOrders_Dates", "\"ExpectedOn\" IS NULL OR \"ExpectedOn\" >= \"OrderDate\"");
            table.HasCheckConstraint("CK_StockPurchaseOrders_Currency", "char_length(\"Currency\") = 3");
            table.HasCheckConstraint("CK_StockPurchaseOrders_FinancialAdjustments", "\"TaxRatePercent\" BETWEEN 0 AND 100 AND \"DiscountAmount\" >= 0 AND \"FreightAmount\" >= 0");
        });
        modelBuilder.Entity<StockPurchaseOrderLine>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<StockPurchaseOrderLine>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockPurchaseOrderLine>().Property(x => x.Notes).HasMaxLength(500);
        modelBuilder.Entity<StockPurchaseOrderLine>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<StockPurchaseOrderLine>().HasIndex(x => new { x.CompanyId, x.PurchaseOrderId });
        modelBuilder.Entity<StockPurchaseOrderLine>().HasIndex(x => new { x.CompanyId, x.ConsumableId });
        modelBuilder.Entity<StockPurchaseOrderLine>().HasIndex(x => new { x.CompanyId, x.ComponentId });
        modelBuilder.Entity<StockPurchaseOrderLine>().ToTable("StockPurchaseOrderLines", table =>
        {
            table.HasCheckConstraint("CK_StockPurchaseOrderLines_ExactlyOneStockItem", "(\"ConsumableId\" IS NOT NULL AND \"ComponentId\" IS NULL) OR (\"ConsumableId\" IS NULL AND \"ComponentId\" IS NOT NULL)");
            table.HasCheckConstraint("CK_StockPurchaseOrderLines_Quantity", "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");
            table.HasCheckConstraint("CK_StockPurchaseOrderLines_Cost", "\"UnitCost\" >= 0");
        });
        modelBuilder.Entity<StockPurchaseOrderLine>().HasOne<StockPurchaseOrder>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockPurchaseOrderLine>().HasOne<Consumable>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ConsumableId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<StockPurchaseOrderLine>().HasOne<InventoryComponent>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ComponentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        modelBuilder.Entity<AssetPurchaseOrder>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetPurchaseOrder>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetPurchaseOrder>().Property(x => x.OrderNumber).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AssetPurchaseOrder>().Property(x => x.Supplier).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<AssetPurchaseOrder>().Property(x => x.Status).HasMaxLength(24).IsRequired();
        modelBuilder.Entity<AssetPurchaseOrder>().Property(x => x.Currency).HasMaxLength(3).IsRequired();
        modelBuilder.Entity<AssetPurchaseOrder>().Property(x => x.Notes).HasMaxLength(2000);
        modelBuilder.Entity<AssetPurchaseOrder>().Property(x => x.TaxRatePercent).HasPrecision(5, 2);
        modelBuilder.Entity<AssetPurchaseOrder>().Property(x => x.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<AssetPurchaseOrder>().Property(x => x.FreightAmount).HasPrecision(18, 2);
        modelBuilder.Entity<AssetPurchaseOrder>().HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
        modelBuilder.Entity<AssetPurchaseOrder>().HasIndex(x => new { x.CompanyId, x.Status, x.OrderDate });
        modelBuilder.Entity<AssetPurchaseOrder>().HasOne<EnvanterUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AssetPurchaseOrder>().HasOne<SupplierCatalogEntry>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AssetPurchaseOrder>().ToTable("AssetPurchaseOrders", table =>
        {
            table.HasCheckConstraint("CK_AssetPurchaseOrders_Status", "\"Status\" IN ('Ordered', 'PartiallyReceived', 'Received', 'Cancelled')");
            table.HasCheckConstraint("CK_AssetPurchaseOrders_Dates", "\"ExpectedOn\" IS NULL OR \"ExpectedOn\" >= \"OrderDate\"");
            table.HasCheckConstraint("CK_AssetPurchaseOrders_Currency", "char_length(\"Currency\") = 3");
            table.HasCheckConstraint("CK_AssetPurchaseOrders_FinancialAdjustments", "\"TaxRatePercent\" BETWEEN 0 AND 100 AND \"DiscountAmount\" >= 0 AND \"FreightAmount\" >= 0");
        });
        modelBuilder.Entity<AssetPurchaseOrderLine>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetPurchaseOrderLine>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetPurchaseOrderLine>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<AssetPurchaseOrderLine>().Property(x => x.Notes).HasMaxLength(500);
        modelBuilder.Entity<AssetPurchaseOrderLine>().Property(x => x.UnitCost).HasPrecision(18, 2);
        modelBuilder.Entity<AssetPurchaseOrderLine>().HasIndex(x => new { x.CompanyId, x.PurchaseOrderId });
        modelBuilder.Entity<AssetPurchaseOrderLine>().ToTable("AssetPurchaseOrderLines", table =>
        {
            table.HasCheckConstraint("CK_AssetPurchaseOrderLines_Quantity", "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");
            table.HasCheckConstraint("CK_AssetPurchaseOrderLines_Cost", "\"UnitCost\" >= 0");
        });
        modelBuilder.Entity<AssetPurchaseOrderLine>().HasOne<AssetPurchaseOrder>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetPurchaseOrderLine>().HasOne<AssetCategory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.CategoryId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AssetPurchaseOrderLine>().HasOne<AssetBrand>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.BrandId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<AssetPurchaseOrderLine>().HasOne<AssetModel>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ModelId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        modelBuilder.Entity<AssetPurchaseOrderAttachment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetPurchaseOrderAttachment>().Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<AssetPurchaseOrderAttachment>().Property(x => x.StorageFileName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AssetPurchaseOrderAttachment>().Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AssetPurchaseOrderAttachment>().HasIndex(x => new { x.CompanyId, x.PurchaseOrderId, x.CreatedAt });
        modelBuilder.Entity<AssetPurchaseOrderAttachment>().ToTable("AssetPurchaseOrderAttachments", table =>
            table.HasCheckConstraint("CK_AssetPurchaseOrderAttachments_Length", "\"Length\" BETWEEN 1 AND 10485760"));
        modelBuilder.Entity<AssetPurchaseOrderAttachment>().HasOne<AssetPurchaseOrder>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetPurchaseOrderAttachment>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockPurchaseOrderAttachment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<StockPurchaseOrderAttachment>().Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<StockPurchaseOrderAttachment>().Property(x => x.StorageFileName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<StockPurchaseOrderAttachment>().Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<StockPurchaseOrderAttachment>().HasIndex(x => new { x.CompanyId, x.PurchaseOrderId, x.CreatedAt });
        modelBuilder.Entity<StockPurchaseOrderAttachment>().ToTable("StockPurchaseOrderAttachments", table =>
            table.HasCheckConstraint("CK_StockPurchaseOrderAttachments_Length", "\"Length\" BETWEEN 1 AND 10485760"));
        modelBuilder.Entity<StockPurchaseOrderAttachment>().HasOne<StockPurchaseOrder>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockPurchaseOrderAttachment>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AccessoryPurchaseOrderAttachment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AccessoryPurchaseOrderAttachment>().Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<AccessoryPurchaseOrderAttachment>().Property(x => x.StorageFileName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AccessoryPurchaseOrderAttachment>().Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AccessoryPurchaseOrderAttachment>().HasIndex(x => new { x.CompanyId, x.PurchaseOrderId, x.CreatedAt });
        modelBuilder.Entity<AccessoryPurchaseOrderAttachment>().ToTable("AccessoryPurchaseOrderAttachments", table =>
            table.HasCheckConstraint("CK_AccessoryPurchaseOrderAttachments_Length", "\"Length\" BETWEEN 1 AND 10485760"));
        modelBuilder.Entity<AccessoryPurchaseOrderAttachment>().HasOne<AccessoryPurchaseOrder>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccessoryPurchaseOrderAttachment>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Asset>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.AssetTag }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.DeletedAt, x.UpdatedAt });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.SerialNumber });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.Status, x.UpdatedAt });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.AssignedEmployeeId });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.ExpectedReturnOn });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.Location });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.LocationCatalogId });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.ParentAssetId });
        modelBuilder.Entity<Asset>().HasIndex(x => new { x.CompanyId, x.PurchaseOrderId });
        modelBuilder.Entity<Asset>().HasOne<AssetPurchaseOrder>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<Asset>().HasOne<CompanyLocation>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LocationCatalogId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        modelBuilder.Entity<AssetReservation>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetReservation>().HasOne<Asset>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssetId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AssetReservation>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.EmployeeId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AssetReservation>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AssetReservation>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AssetReservation>().Property(x => x.Status).HasMaxLength(16).IsRequired();
        modelBuilder.Entity<AssetReservation>().Property(x => x.RequestNote).HasMaxLength(1000);
        modelBuilder.Entity<AssetReservation>().Property(x => x.ReviewNote).HasMaxLength(1000);
        modelBuilder.Entity<AssetReservation>().HasIndex(x => new { x.CompanyId, x.Status, x.StartsOn });
        modelBuilder.Entity<AssetReservation>().HasIndex(x => new { x.CompanyId, x.AssetId, x.StartsOn, x.EndsOn });
        modelBuilder.Entity<AssetReservation>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_AssetReservations_Dates", "\"EndsOn\" >= \"StartsOn\"");
            table.HasCheckConstraint("CK_AssetReservations_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected', 'Cancelled', 'Completed')");
        });
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
            table.HasCheckConstraint("CK_Assets_ExpectedReturn", "\"ExpectedReturnOn\" IS NULL OR (\"Status\" = 'Deployed' AND \"AssignedEmployeeId\" IS NOT NULL)");
            table.HasCheckConstraint("CK_Assets_NonnegativeCost", "\"PurchaseCost\" IS NULL OR \"PurchaseCost\" >= 0");
            table.HasCheckConstraint("CK_Assets_Currency", "char_length(\"Currency\") = 3");
            table.HasCheckConstraint("CK_Assets_NotOwnParent", "\"ParentAssetId\" IS NULL OR \"ParentAssetId\" <> \"Id\"");
        });
        modelBuilder.Entity<Asset>().HasOne<AssetCategory>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.CategoryId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Asset>().HasOne<Asset>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ParentAssetId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        modelBuilder.Entity<AssetEvent>().HasIndex(x => new { x.CompanyId, x.AssetId, x.OccurredAt });
        modelBuilder.Entity<AssetEvent>().HasAlternateKey(x => new { x.CompanyId, x.AssetId, x.Id });
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
        modelBuilder.Entity<AssetAttachment>().HasOne<AssetEvent>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.AssetId, x.AssetEventId })
            .HasPrincipalKey(x => new { x.CompanyId, x.AssetId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
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
        modelBuilder.Entity<AssetMaintenance>().HasIndex(x => new { x.CompanyId, x.ResponsibleEmployeeId, x.CreatedAt });
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
        modelBuilder.Entity<AssetMaintenance>().HasAlternateKey(x => new { x.CompanyId, x.Id });
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
        modelBuilder.Entity<AssetMaintenance>().HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ResponsibleEmployeeId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        modelBuilder.Entity<AssetMaintenanceAttachment>().HasAlternateKey(x => new { x.CompanyId, x.Id });
        modelBuilder.Entity<AssetMaintenanceAttachment>().Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<AssetMaintenanceAttachment>().Property(x => x.StorageFileName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AssetMaintenanceAttachment>().Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AssetMaintenanceAttachment>().HasIndex(x => new { x.CompanyId, x.MaintenanceId, x.CreatedAt });
        modelBuilder.Entity<AssetMaintenanceAttachment>().ToTable("AssetMaintenanceAttachments", table =>
            table.HasCheckConstraint("CK_AssetMaintenanceAttachments_Length", "\"Length\" BETWEEN 1 AND 10485760"));
        modelBuilder.Entity<AssetMaintenanceAttachment>().HasOne<Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetMaintenanceAttachment>().HasOne<AssetMaintenance>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.MaintenanceId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssetMaintenanceAttachment>().HasOne<EnvanterUser>().WithMany()
            .HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
