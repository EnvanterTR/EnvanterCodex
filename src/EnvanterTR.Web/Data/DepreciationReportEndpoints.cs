using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class DepreciationReportEndpoints
{
    private const int MaximumReportRows = 50_000;
    private sealed record SourceRow(Guid Id, string AssetTag, string Name, string? SerialNumber, string Category,
        string? Model, string? Location, string Status, DateOnly PurchaseDate, decimal PurchaseCost, string Currency,
        int Months, decimal? Minimum, string MinimumType, string Method);
    private sealed record ReportRow(Guid Id, string AssetTag, string Name, string? SerialNumber, string Category,
        string? Model, string? Location, string Status, DateOnly PurchaseDate, decimal PurchaseCost, string Currency,
        int Months, decimal? Minimum, string MinimumType, string Method, DateOnly EndOfLife, int ElapsedMonths,
        int RemainingMonths, decimal MonthlyDepreciation, decimal BookValue, decimal AccumulatedDepreciation);

    public static void MapDepreciationReportEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/reports/depreciation", async ([FromQuery] string? q, [FromQuery] DateOnly? asOf,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            var effectiveDate = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (effectiveDate < new DateOnly(2000, 1, 1) || effectiveDate > new DateOnly(2100, 12, 31))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["asOf"] = ["Rapor tarihi 2000 ile 2100 arasında olmalıdır."] });
            var sourceQuery = BuildQuery(db, companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                sourceQuery = sourceQuery.Where(x => x.AssetTag.Contains(term) || x.Name.Contains(term) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(term)) || x.Category.Contains(term) ||
                    (x.Model != null && x.Model.Contains(term)) || (x.Location != null && x.Location.Contains(term)));
            }
            var total = await sourceQuery.CountAsync(ct);
            if (total > MaximumReportRows)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["report"] = [$"Amortisman raporu bir defada en fazla {MaximumReportRows:N0} uygun varlığı hesaplar. Aramayı daraltın."] });
            var source = await sourceQuery.ToListAsync(ct);
            var rows = source.OrderBy(x => x.AssetTag, StringComparer.Ordinal)
                .Select(x => Calculate(x, effectiveDate)).ToList();
            var totalsByCurrency = rows.GroupBy(x => x.Currency).OrderBy(x => x.Key).Select(group => new
            {
                currency = group.Key,
                assetCount = group.Count(),
                acquisitionCost = group.Sum(x => x.PurchaseCost),
                bookValue = group.Sum(x => x.BookValue),
                accumulatedDepreciation = group.Sum(x => x.AccumulatedDepreciation),
                monthlyDepreciation = group.Sum(x => x.MonthlyDepreciation)
            }).ToArray();
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page == 0 ? 1 : page, 1, pageCount);
            return Results.Ok(new { asOf = effectiveDate, total, page, pageSize, pageCount, totalsByCurrency,
                items = rows.Skip((page - 1) * pageSize).Take(pageSize) });
        }).RequireAuthorization("asset.read");

        api.MapGet("/reports/depreciation/export.csv", async ([FromQuery] string? q, [FromQuery] DateOnly? asOf,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            var effectiveDate = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (effectiveDate < new DateOnly(2000, 1, 1) || effectiveDate > new DateOnly(2100, 12, 31))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["asOf"] = ["Rapor tarihi 2000 ile 2100 arasında olmalıdır."] });
            var sourceQuery = BuildQuery(db, companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                sourceQuery = sourceQuery.Where(x => x.AssetTag.Contains(term) || x.Name.Contains(term) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(term)) || x.Category.Contains(term) ||
                    (x.Model != null && x.Model.Contains(term)) || (x.Location != null && x.Location.Contains(term)));
            }
            if (await sourceQuery.CountAsync(ct) > MaximumReportRows)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["report"] = [$"CSV dışa aktarma en fazla {MaximumReportRows:N0} uygun varlığı destekler. Aramayı daraltın."] });
            var rows = (await sourceQuery.ToListAsync(ct)).OrderBy(x => x.AssetTag, StringComparer.Ordinal)
                .Select(x => Calculate(x, effectiveDate));
            static string Cell(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "\"\"";
                var formula = value.TrimStart() is { Length: > 0 } trimmed && trimmed[0] is '=' or '+' or '-' or '@';
                return "\"" + (formula ? "'" + value : value).Replace("\"", "\"\"") + "\"";
            }
            var csv = new StringBuilder("Etiket;Varlık;Seri no;Kategori;Model;Konum;Durum;Alış tarihi;Ömür sonu;Para birimi;Alış bedeli;Aylık amortisman;Birikmiş amortisman;Defter değeri;Kalan ay;Yöntem\r\n");
            foreach (var x in rows)
                csv.AppendJoin(';', Cell(x.AssetTag), Cell(x.Name), Cell(x.SerialNumber), Cell(x.Category), Cell(x.Model), Cell(x.Location),
                    Cell(x.Status), Cell(x.PurchaseDate.ToString("yyyy-MM-dd")), Cell(x.EndOfLife.ToString("yyyy-MM-dd")), Cell(x.Currency),
                    Number(x.PurchaseCost), Number(x.MonthlyDepreciation), Number(x.AccumulatedDepreciation), Number(x.BookValue),
                    x.RemainingMonths.ToString(System.Globalization.CultureInfo.InvariantCulture), Cell(MethodLabel(x.Method))).Append("\r\n");
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-amortisman-{effectiveDate:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");
    }

    private static IQueryable<SourceRow> BuildQuery(AppDbContext db, Guid companyId) =>
        from asset in db.Assets.AsNoTracking()
        join model in db.AssetModels.AsNoTracking() on new { asset.CompanyId, Id = asset.ModelId } equals new { model.CompanyId, Id = (Guid?)model.Id }
        join category in db.AssetCategories.AsNoTracking() on new { asset.CompanyId, asset.CategoryId } equals new { category.CompanyId, CategoryId = category.Id }
        where asset.CompanyId == companyId && asset.DeletedAt == null && asset.PurchaseDate != null && asset.PurchaseCost != null &&
              model.DepreciationMonths != null
        select new SourceRow(asset.Id, asset.AssetTag, asset.Name, asset.SerialNumber, category.Name, model.Name, asset.Location,
            asset.Status, asset.PurchaseDate!.Value, asset.PurchaseCost!.Value, asset.Currency, model.DepreciationMonths!.Value,
            model.DepreciationMinimum, model.DepreciationMinimumType, model.DepreciationMethod);

    private static ReportRow Calculate(SourceRow source, DateOnly asOf)
    {
        var lifeMonths = source.Months;
        var endOfLife = source.PurchaseDate.AddMonths(lifeMonths);
        var elapsed = Math.Clamp(MonthsBetween(source.PurchaseDate, asOf), 0, lifeMonths);
        var floor = source.Minimum is null ? 0m : source.MinimumType == "percent"
            ? source.PurchaseCost * source.Minimum.Value / 100m : source.Minimum.Value;
        floor = Math.Clamp(floor, 0m, source.PurchaseCost);
        decimal book;
        if (source.Method == "linear")
            book = elapsed >= lifeMonths ? floor : source.PurchaseCost - (source.PurchaseCost - floor) * elapsed / lifeMonths;
        else
        {
            var fiscalYears = Math.Max(1, (int)Math.Ceiling(lifeMonths / 12d));
            var yearsPast = (double)(FiscalYear(asOf) - FiscalYear(source.PurchaseDate));
            if (source.Method == "half_1") yearsPast -= .5;
            else if (!IsFirstHalf(source.PurchaseDate)) yearsPast -= .5;
            if (!IsFirstHalf(asOf)) yearsPast += .5;
            yearsPast = Math.Clamp(yearsPast, 0d, fiscalYears);
            // The reference half-year methods prorate the original purchase cost and
            // intentionally do not apply the linear method's minimum floor.
            book = source.PurchaseCost - Math.Round(source.PurchaseCost * (decimal)(yearsPast / fiscalYears), 2,
                MidpointRounding.AwayFromZero);
            if (asOf < source.PurchaseDate) book = source.PurchaseCost;
        }
        var minimumBookValue = source.Method == "linear" ? floor : 0m;
        book = Math.Round(Math.Clamp(book, minimumBookValue, source.PurchaseCost), 2, MidpointRounding.AwayFromZero);
        var monthly = Math.Round((source.PurchaseCost - floor) / lifeMonths, 2, MidpointRounding.AwayFromZero);
        return new ReportRow(source.Id, source.AssetTag, source.Name, source.SerialNumber, source.Category, source.Model,
            source.Location, source.Status, source.PurchaseDate, source.PurchaseCost, source.Currency, lifeMonths, source.Minimum,
            source.MinimumType, source.Method, endOfLife, elapsed, Math.Max(0, lifeMonths - elapsed), monthly, book,
            Math.Round(source.PurchaseCost - book, 2, MidpointRounding.AwayFromZero));
    }

    internal static decimal? CalculateListValue(DateOnly? purchaseDate, decimal? purchaseCost, int? months,
        decimal? minimum, string? minimumType, string? method, DateOnly asOf)
    {
        if (purchaseDate is null || purchaseCost is null || months is null || months <= 0) return null;
        var source = new SourceRow(Guid.Empty, "", "", null, "", null, null, "", purchaseDate.Value,
            purchaseCost.Value, "TRY", months.Value, minimum, minimumType ?? "amount", method ?? "linear");
        return Calculate(source, asOf).BookValue;
    }

    internal static int? CalculateElapsedMonths(DateOnly? startDate, DateOnly asOf) =>
        startDate is DateOnly start ? MonthsBetween(start, asOf) : null;

    private static int MonthsBetween(DateOnly start, DateOnly end)
    {
        if (end < start) return 0;
        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
        return end.Day < start.Day ? months - 1 : months;
    }
    private static int FiscalYear(DateOnly date) => date.Month == 12 && date.Day == 31 ? date.Year : date.Year - 1;
    private static bool IsFirstHalf(DateOnly date) => date.Month < 6 || (date.Month == 12 && date.Day == 31);
    private static string MethodLabel(string method) => method switch { "half_1" => "Yarım yıl · ilk dönem", "half_2" => "Yarım yıl · satın alma dönemine göre", _ => "Doğrusal" };
    private static string Number(decimal value) => value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    private static bool TryCompany(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
}
