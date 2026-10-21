namespace EnvanterTR.Web.Data;

public static class AnnualCloudCatalog
{
    public const string Version = "2026-10-21";
    public const int TrialDays = 15;
    public sealed record Tier(string Code, string Name, int Users, decimal MonthlyEquivalent, decimal AnnualPrice);
    public static IReadOnlyList<Tier> Tiers { get; } = Array.AsReadOnly(new[]
    {
        new Tier("cloud-individual-1", "Bireysel · 1 kullanıcı", 1, 200m, 2400m),
        new Tier("cloud-business-4", "İşletme · 4 kullanıcı", 4, 350m, 4200m),
        new Tier("cloud-business-10", "İşletme · 10 kullanıcı", 10, 500m, 6000m),
        new Tier("cloud-corporate-50", "Kurumsal · 50 kullanıcı", 50, 750m, 9000m),
        new Tier("cloud-corporate-100", "Kurumsal · 100 kullanıcı", 100, 1000m, 12000m),
        new Tier("cloud-corporate-250", "Kurumsal · 250 kullanıcı", 250, 1200m, 14400m),
        new Tier("cloud-corporate-1000", "Kurumsal · 1.000 kullanıcı", 1000, 2000m, 24000m),
        new Tier("cloud-corporate-2000", "Kurumsal · 2.000 kullanıcı", 2000, 3000m, 36000m)
    });

    public static SavePlanRequest Draft(Tier tier, int order, string reason) => new(
        tier.Code, tier.Name, "Yıllık tek ödeme; aylık tutar yalnız karşılaştırma içindir. Fiyat KDV hariçtir.",
        "cloud", tier.Users, null, 0m, tier.AnnualPrice, 0m, "TRY", false, 0,
        "{\"reports\":true,\"api\":true,\"maintenance\":true}", false, true, order, reason);
}

public sealed record CreateAnnualCatalogDraftsRequest(string? ChangeReason);
