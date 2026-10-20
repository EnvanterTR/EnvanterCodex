using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

internal sealed record ScheduledReportDocument(string[] Headers, List<object?[]> Rows);

internal static class ScheduledReportData
{
    public static async Task<ScheduledReportDocument> BuildAsync(AppDbContext db, ScheduledReport report, CancellationToken ct)
    {
        var companyId = report.CompanyId;
        var locations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, report.UserId, ct);
        var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, report.UserId, ct);
        var assets = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (locations is not null) assets = assets.Where(x => x.LocationCatalogId.HasValue && locations.Contains(x.LocationCatalogId.Value));
        var rows = new List<object?[]>();
        string T(string key) => ScheduledReportText.Get(key, report.Language);
        switch (report.ReportKind)
        {
            case "inventory":
            {
                rows.Add([T("summary"), T("assets"), await assets.CountAsync(ct), null, null]);
                var statuses = await assets.GroupBy(x => x.Status).Select(g => new { Name = g.Key, Count = g.Count() }).OrderBy(x => x.Name).ToListAsync(ct);
                foreach (var row in statuses) rows.Add([T("status"), T(row.Name), row.Count, null, null]);
                var categories = await (from asset in assets join category in db.AssetCategories.AsNoTracking()
                    on new { asset.CompanyId, asset.CategoryId } equals new { category.CompanyId, CategoryId = category.Id }
                    group asset by category.Name into g orderby g.Key select new { Name = g.Key, Count = g.Count() }).ToListAsync(ct);
                foreach (var row in categories) rows.Add([T("category"), row.Name, row.Count, null, null]);
                var costs = await assets.Where(x => x.PurchaseCost != null).GroupBy(x => x.Currency)
                    .Select(g => new { Currency = g.Key, Count = g.Count(), Amount = g.Sum(x => x.PurchaseCost!.Value) }).OrderBy(x => x.Currency).ToListAsync(ct);
                foreach (var row in costs) rows.Add([T("cost"), T("assets"), row.Count, row.Amount, row.Currency]);
                break;
            }
            case "maintenance":
            {
                var query = db.AssetMaintenances.AsNoTracking().Where(x => x.CompanyId == companyId &&
                    (x.Status == "InProgress" || x.Status == "Completed")).InAssetReadScope(db, companyId, employeeScope);
                var statuses = await query.GroupBy(x => x.Status).Select(g => new { Name = g.Key, Count = g.Count() }).OrderBy(x => x.Name).ToListAsync(ct);
                foreach (var row in statuses) rows.Add([T("status"), T(row.Name), row.Count, null, null]);
                var types = await query.GroupBy(x => x.MaintenanceType).Select(g => new { Name = g.Key, Count = g.Count() }).OrderBy(x => x.Name).ToListAsync(ct);
                foreach (var row in types) rows.Add([T("maintenance"), row.Name, row.Count, null, null]);
                var costs = await query.Where(x => x.Cost != null).GroupBy(x => x.Currency)
                    .Select(g => new { Currency = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Cost!.Value) }).OrderBy(x => x.Currency).ToListAsync(ct);
                foreach (var row in costs) rows.Add([T("cost"), T("maintenance"), row.Count, row.Amount, row.Currency]);
                break;
            }
            case "licenses":
            {
                var employees = employeeScope.Apply(db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId));
                var assignments = db.SoftwareLicenseAssignments.AsNoTracking().Where(x => x.CompanyId == companyId);
                if (employeeScope.IsRestricted) assignments = assignments.Where(x =>
                    (x.EmployeeId.HasValue || x.AssetId.HasValue) &&
                    (!x.EmployeeId.HasValue || employees.Any(e => e.Id == x.EmployeeId)) &&
                    (!x.AssetId.HasValue || assets.Any(a => a.Id == x.AssetId)));
                var licenses = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId && !x.IsArchived);
                if (employeeScope.IsRestricted) licenses = licenses.Where(x => assignments.Any(a => a.LicenseId == x.Id));
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var through = today.AddDays(30);
                rows.Add([T("summary"), T("licenses"), await licenses.CountAsync(ct), null, null]);
                rows.Add([T("status"), T("expired"), await licenses.CountAsync(x => x.ExpirationDate.HasValue && x.ExpirationDate.Value < today, ct), null, null]);
                rows.Add([T("status"), T("upcoming"), await licenses.CountAsync(x => x.ExpirationDate.HasValue && x.ExpirationDate.Value >= today && x.ExpirationDate.Value <= through, ct), null, null]);
                rows.Add([T("summary"), T("seats"), await licenses.SumAsync(x => (int?)x.Seats, ct) ?? 0, null, null]);
                rows.Add([T("summary"), T("assigned"), await assignments.CountAsync(x => x.RevokedAt == null && licenses.Any(l => l.Id == x.LicenseId), ct), null, null]);
                break;
            }
            case "stock":
            {
                var accessories = db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId);
                var consumables = db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId);
                var components = db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId);
                if (locations is not null)
                {
                    accessories = accessories.Where(x => x.LocationCatalogId.HasValue && locations.Contains(x.LocationCatalogId.Value));
                    consumables = consumables.Where(x => x.LocationCatalogId.HasValue && locations.Contains(x.LocationCatalogId.Value));
                    components = components.Where(x => x.LocationCatalogId.HasValue && locations.Contains(x.LocationCatalogId.Value));
                }
                var accessoryTotal = await accessories.SumAsync(x => (long?)x.TotalQuantity, ct) ?? 0;
                var accessoryIssued = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && accessories.Any(a => a.Id == x.AccessoryId))
                    .SumAsync(x => (long?)(x.Quantity - x.ReturnedQuantity), ct) ?? 0;
                var consumableTotal = await consumables.SumAsync(x => (long?)x.TotalQuantity, ct) ?? 0;
                var consumableIssued = await db.ConsumableMovements.Where(x => x.CompanyId == companyId && x.MovementType == "Checkout" && consumables.Any(c => c.Id == x.ConsumableId))
                    .SumAsync(x => (long?)x.Quantity, ct) ?? 0;
                var componentTotal = await components.SumAsync(x => (long?)x.TotalQuantity, ct) ?? 0;
                var componentIssued = await db.ComponentAllocations.Where(x => x.CompanyId == companyId && components.Any(c => c.Id == x.ComponentId))
                    .SumAsync(x => (long?)(x.Quantity - x.RemovedQuantity), ct) ?? 0;
                foreach (var (name, total, issued) in new[] { ("accessories", accessoryTotal, accessoryIssued), ("consumables", consumableTotal, consumableIssued), ("components", componentTotal, componentIssued) })
                {
                    rows.Add([T(name), T("total"), total, null, null]);
                    rows.Add([T(name), T("assigned"), issued, null, null]);
                    rows.Add([T(name), T("available"), total - issued, null, null]);
                }
                break;
            }
            default: throw new InvalidOperationException("Unsupported scheduled report.");
        }
        return new([T("section"), T("record"), T("count"), T("amount"), T("currency")], rows);
    }
}

internal static class ScheduledReportText
{
    private static readonly Dictionary<string, string[]> Copy = new(StringComparer.Ordinal)
    {
        ["section"] = ["Bölüm", "Section", "Раздел", "Розділ", "Оддел"],
        ["record"] = ["Kayıt", "Record", "Запись", "Запис", "Запис"],
        ["count"] = ["Adet", "Count", "Количество", "Кількість", "Број"],
        ["amount"] = ["Tutar", "Amount", "Сумма", "Сума", "Износ"],
        ["currency"] = ["Para birimi", "Currency", "Валюта", "Валюта", "Валута"],
        ["summary"] = ["Özet", "Summary", "Сводка", "Зведення", "Резиме"],
        ["status"] = ["Durum", "Status", "Статус", "Стан", "Статус"],
        ["category"] = ["Kategori", "Category", "Категория", "Категорія", "Категорија"],
        ["cost"] = ["Kayıtlı maliyet", "Recorded cost", "Учтенная стоимость", "Облікована вартість", "Евидентиран трошок"],
        ["assets"] = ["Varlıklar", "Assets", "Активы", "Активи", "Средства"],
        ["maintenance"] = ["Bakım", "Maintenance", "Техобслуживание", "Обслуговування", "Одржување"],
        ["licenses"] = ["Lisanslar", "Licenses", "Лицензии", "Ліцензії", "Лиценци"],
        ["accessories"] = ["Aksesuarlar", "Accessories", "Аксессуары", "Аксесуари", "Додатоци"],
        ["consumables"] = ["Sarf malzemeleri", "Consumables", "Расходники", "Витратні матеріали", "Потрошен материјал"],
        ["components"] = ["Bileşenler", "Components", "Компоненты", "Компоненти", "Компоненти"],
        ["total"] = ["Toplam", "Total", "Всего", "Усього", "Вкупно"],
        ["assigned"] = ["Atanmış / tüketilen", "Assigned / consumed", "Выдано / израсходовано", "Видано / витрачено", "Доделено / потрошено"],
        ["available"] = ["Kullanılabilir", "Available", "Доступно", "Доступно", "Достапно"],
        ["seats"] = ["Lisans koltukları", "License seats", "Лицензионные места", "Ліцензійні місця", "Лиценцни места"],
        ["expired"] = ["Süresi dolmuş", "Expired", "Просрочено", "Прострочено", "Истечено"],
        ["upcoming"] = ["30 günde sona erecek", "Expires within 30 days", "Истекает за 30 дней", "Спливає за 30 днів", "Истекува во 30 дена"],
        ["Available"] = ["Kullanılabilir", "Available", "Доступно", "Доступно", "Достапно"],
        ["Deployed"] = ["Zimmetli", "Assigned", "Выдано", "Видано", "Доделено"],
        ["Pending"] = ["Beklemede", "Pending", "Ожидает", "Очікує", "Во чекање"],
        ["Maintenance"] = ["Bakımda", "In maintenance", "На обслуживании", "На обслуговуванні", "На одржување"],
        ["Unusable"] = ["Kullanılamaz", "Unusable", "Недоступно", "Недоступно", "Недостапно"],
        ["Archived"] = ["Arşivli", "Archived", "Архив", "Архів", "Архивирано"],
        ["InProgress"] = ["Devam eden", "In progress", "В процессе", "У процесі", "Во тек"],
        ["Completed"] = ["Tamamlanan", "Completed", "Завершено", "Завершено", "Завршено"],
        ["subject"] = ["Zamanlanmış rapor", "Scheduled report", "Плановый отчет", "Запланований звіт", "Закажан извештај"],
        ["csvHint"] = ["Tam rapor CSV ekindedir. Maliyetler para birimine göre ayrı tutulur.", "The full report is in the CSV attachment. Costs are separated by currency.", "Полный отчет — в приложенном CSV. Стоимость разделена по валютам.", "Повний звіт — у вкладеному CSV. Вартість розділена за валютами.", "Целиот извештај е во CSV прилогот. Трошоците се одделени по валута."],
        ["footer"] = ["Bu rapor kendi rapor takviminizden gönderildi. Takvimi EnvanterTR raporlar ekranından kapatabilirsiniz.", "This report was sent from your report schedule. Disable it from EnvanterTR reports.", "Отчет отправлен по вашему расписанию. Отключить его можно в отчетах EnvanterTR.", "Звіт надіслано за вашим розкладом. Вимкнути його можна у звітах EnvanterTR.", "Извештајот е испратен според вашиот распоред. Исклучете го во извештаите на EnvanterTR."]
    };
    public static string Get(string key, string language) => Copy.TryGetValue(key, out var values)
        ? values[language switch { "en" => 1, "ru" => 2, "uk" => 3, "mk" => 4, _ => 0 }] : key;
}
