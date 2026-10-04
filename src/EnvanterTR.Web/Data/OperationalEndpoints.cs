using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace EnvanterTR.Web.Data;

public static class OperationalEndpoints
{
    private const long CompanyLogoRequestLimitBytes = 2_162_688; // 2 MiB logo plus bounded multipart overhead.

    public static IEndpointRouteBuilder MapOperationalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api").RequireAuthorization();
        api.AddEndpointFilter(async (invocation, next) =>
        {
            var user = invocation.HttpContext.User;
            if (!TryScope(user, out var companyId, out var actorId) ||
                string.IsNullOrWhiteSpace(user.FindFirstValue("company_role"))) return Results.Forbid();
            var db = invocation.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var role = user.FindFirstValue("company_role");
            if (!await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId && x.IsActive &&
                    db.CompanyMemberships.Any(m => m.CompanyId == companyId && m.UserId == actorId && m.RoleKey == role),
                    invocation.HttpContext.RequestAborted)) return Results.Forbid();
            if (HttpMethods.IsGet(invocation.HttpContext.Request.Method) || HttpMethods.IsHead(invocation.HttpContext.Request.Method))
                return await next(invocation);
            var antiforgery = invocation.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try { await antiforgery.ValidateRequestAsync(invocation.HttpContext); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
            return await next(invocation);
        });

        api.MapGet("/company/profile", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var company = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => new { x.Name, x.ContactEmail, x.BrandPrimaryColor, HasLogo = x.BrandLogoPath != null })
                .SingleOrDefaultAsync(cancellationToken);
            return company is null ? Results.NotFound() : Results.Ok(company);
        }).RequireAuthorization("settings.manage");

        api.MapGet("/company/branding", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var branding = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => new { x.Name, x.BrandPrimaryColor, HasLogo = x.BrandLogoPath != null })
                .SingleOrDefaultAsync(cancellationToken);
            return branding is null ? Results.NotFound() : Results.Ok(branding);
        }).RequireAuthorization("company.member");

        api.MapGet("/company/label-settings", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var raw = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => x.LabelPrintSettingsJson).SingleOrDefaultAsync(cancellationToken);
            if (raw is null) return Results.Ok(new { isConfigured = false, settings = DefaultLabelPrintSettings });
            try { return Results.Ok(new { isConfigured = true, settings = JsonSerializer.Deserialize<LabelPrintSettingsRequest>(raw) ?? DefaultLabelPrintSettings }); }
            catch (JsonException) { return Results.Ok(new { isConfigured = false, settings = DefaultLabelPrintSettings }); }
        }).RequireAuthorization("company.member");

        api.MapPut("/company/label-settings", async ([FromBody] LabelPrintSettingsRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Barcode1d is not ("code128" or "code39" or "ean5" or "ean13" or "upca" or "upce" or "none") ||
                request.Barcode2d is not ("qrcode" or "pdf417" or "datamatrix" or "none") ||
                request.QrTarget is not ("tag" or "asset-url") ||
                request.WidthMm is < 25 or > 150 || request.HeightMm is < 15 or > 100 ||
                request.OffsetXmm is < -30 or > 30 || request.OffsetYmm is < -30 or > 30 ||
                request.PaddingMm is < 0 or > 15 || request.CodeSizeMm is < 10 or > 45)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["settings"] = ["Barkod türü ve yazdırma ölçülerini izin verilen aralıklarla kontrol edin."] });
            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            company.LabelPrintSettingsJson = JsonSerializer.Serialize(request);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Company", TargetId = companyId, Action = "LabelSettingsUpdated",
                Details = "Şirketin barkod ve etiket yazdırma tercihleri güncellendi." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { isConfigured = true, settings = request });
        }).RequireAuthorization("settings.manage");

        api.MapPut("/company/profile", async ([FromBody] UpdateCompanyProfileRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var name = request.Name?.Trim();
            var email = request.ContactEmail?.Trim();
            var color = request.BrandPrimaryColor?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200 ||
                (email is { Length: > 256 } || (!string.IsNullOrEmpty(email) && !MailAddress.TryCreate(email, out _))) ||
                color is null || !Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant))
                return Results.ValidationProblem(new Dictionary<string, string[]> {
                    ["profile"] = ["Şirket adı (en fazla 200), geçerli e-posta ve #RRGGBB biçiminde renk gereklidir."]
                });
            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            company.Name = name;
            company.ContactEmail = string.IsNullOrWhiteSpace(email) ? null : email;
            company.BrandPrimaryColor = color.ToUpperInvariant();
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Company", TargetId = companyId, Action = "ProfileUpdated",
                Details = "Şirket adı, iletişim e-postası veya tema rengi güncellendi." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { company.Name, company.ContactEmail, company.BrandPrimaryColor, HasLogo = company.BrandLogoPath is not null });
        }).RequireAuthorization("settings.manage");

        api.MapPost("/company/branding/logo", async ([FromForm] IFormFile? logo, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (logo is null || logo.Length is < 1 or > 2_097_152)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["logo"] = ["Logo 1 bayt ile 2 MB arasında olmalıdır."] });
            await using var buffer = new MemoryStream();
            await logo.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var (contentType, extension) = DetectLogo(bytes);
            if (contentType is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["logo"] = ["Yalnızca doğrulanmış PNG, JPEG veya WebP görselleri yüklenebilir."] });

            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            var directory = Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media", companyId.ToString("N"));
            Directory.CreateDirectory(directory);
            var fileName = $"{Guid.NewGuid():N}.{extension}";
            var destination = Path.Combine(directory, fileName);
            await System.IO.File.WriteAllBytesAsync(destination, bytes, cancellationToken);
            var previous = company.BrandLogoPath;
            company.BrandLogoPath = fileName;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Company", TargetId = companyId, Action = "BrandLogoUpdated", Details = "Şirket logosu değiştirildi." });
            await db.SaveChangesAsync(cancellationToken);
            if (previous is not null && Regex.IsMatch(previous, "^[a-fA-F0-9]{32}\\.(png|jpg|webp)$", RegexOptions.CultureInvariant))
            {
                var oldPath = Path.Combine(directory, previous);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
            return Results.Ok(new { hasLogo = true, contentType });
        }).RequireAuthorization("settings.manage")
            .WithMetadata(new RequestSizeLimitAttribute(CompanyLogoRequestLimitBytes));

        api.MapGet("/company/branding/logo", async (ClaimsPrincipal user, AppDbContext db,
            IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var fileName = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => x.BrandLogoPath).SingleOrDefaultAsync(cancellationToken);
            if (fileName is null || !Regex.IsMatch(fileName, "^[a-fA-F0-9]{32}\\.(png|jpg|webp)$", RegexOptions.CultureInvariant))
                return Results.NotFound();
            var path = Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media", companyId.ToString("N"), fileName);
            if (!System.IO.File.Exists(path)) return Results.NotFound();
            var contentType = Path.GetExtension(fileName) switch { ".png" => "image/png", ".jpg" => "image/jpeg", _ => "image/webp" };
            return Results.File(await System.IO.File.ReadAllBytesAsync(path, cancellationToken), contentType,
                enableRangeProcessing: false);
        }).RequireAuthorization("company.member");

        api.MapGet("/employees", async ([FromQuery] string? q, [FromQuery] string? department,
            [FromQuery] Guid? managerId, [FromQuery] bool? active,
            [FromQuery] int page, [FromQuery] int pageSize, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var query = db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (active is bool isActive) query = query.Where(x => x.IsActive == isActive);
            if (managerId is Guid selectedManager) query = query.Where(x => x.ManagerEmployeeId == selectedManager);
            if (!string.IsNullOrWhiteSpace(department))
            {
                var selectedDepartment = department.Trim();
                if (selectedDepartment.Length > 160) return Results.ValidationProblem(new Dictionary<string, string[]> { ["department"] = ["Departman filtresi 160 karakteri aşamaz."] });
                var departmentPattern = selectedDepartment.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                query = query.Where(x => x.Department != null && EF.Functions.ILike(x.Department.Trim(), departmentPattern, "\\"));
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.EmployeeNumber.Contains(term) || x.DisplayName.Contains(term) ||
                    (x.Email != null && x.Email.Contains(term)) || (x.Department != null && x.Department.Contains(term)) ||
                    db.Employees.Any(manager => manager.CompanyId == companyId && manager.Id == x.ManagerEmployeeId && manager.DisplayName.Contains(term)));
            }
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderBy(x => x.DisplayName).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.EmployeeNumber, x.DisplayName, x.Email, x.Department, x.IsActive,
                    x.ManagerEmployeeId,
                    ManagerName = db.Employees.Where(manager => manager.CompanyId == companyId && manager.Id == x.ManagerEmployeeId)
                        .Select(manager => manager.DisplayName).FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { items, total, page, pageSize });
        }).RequireAuthorization("people.read");

        api.MapPost("/employees", async ([FromBody] CreateEmployeeRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var number = request.EmployeeNumber?.Trim().ToUpperInvariant();
            var displayName = request.DisplayName?.Trim();
            var email = Clean(request.Email);
            var department = Clean(request.Department);
            if (string.IsNullOrWhiteSpace(number) || number.Length > 80 ||
                string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200 ||
                email is { Length: > 256 } || department is { Length: > 160 })
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["employee"] = ["Personel numarası ve görünen ad zorunludur; numara 80, ad 200 karakteri aşamaz."]
                });
            if (await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.EmployeeNumber == number, cancellationToken))
                return Results.Conflict(new { message = "Bu personel numarası şirket içinde kullanılıyor." });
            if (request.ManagerEmployeeId is Guid managerId &&
                !await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.Id == managerId && x.IsActive, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["managerEmployeeId"] = ["Yönetici aynı şirkette bulunan etkin bir çalışan olmalıdır."] });
            var employee = new Employee
            {
                CompanyId = companyId, EmployeeNumber = number, DisplayName = displayName,
                Email = email, Department = department, ManagerEmployeeId = request.ManagerEmployeeId
            };
            db.Employees.Add(employee);
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee",
                TargetId = employee.Id, Action = "Created", Details = $"Personel no: {employee.EmployeeNumber}"
            });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Çalışan kaydı çakıştı." }); }
            return Results.Created($"/api/employees/{employee.Id}", new
            {
                employee.Id, employee.EmployeeNumber, employee.DisplayName, employee.Email, employee.Department,
                employee.ManagerEmployeeId, employee.IsActive
            });
        }).RequireAuthorization("people.manage");

        api.MapGet("/employees/{id:guid}", async (Guid id, [FromQuery] int page, [FromQuery] int pageSize,
            ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new
                {
                    x.Id, x.EmployeeNumber, x.DisplayName, x.Email, x.Department, x.IsActive, x.CreatedAt,
                    x.ManagerEmployeeId,
                    ManagerName = db.Employees.Where(manager => manager.CompanyId == companyId && manager.Id == x.ManagerEmployeeId)
                        .Select(manager => manager.DisplayName).FirstOrDefault()
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (employee is null) return Results.NotFound();
            var assignedQuery = db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.AssignedEmployeeId == id);
            var assignedAssetCount = await assignedQuery.CountAsync(cancellationToken);
            var assignedAssets = await assignedQuery.OrderBy(x => x.AssetTag)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.AssetTag, x.Name, x.Status, x.Location })
                .ToListAsync(cancellationToken);
            var reportsQuery = db.Employees.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.ManagerEmployeeId == id);
            var directReportCount = await reportsQuery.CountAsync(cancellationToken);
            var directReports = await reportsQuery.OrderBy(x => x.DisplayName).Take(100)
                .Select(x => new
                {
                    x.Id, x.EmployeeNumber, x.DisplayName, x.Department, x.IsActive,
                    AssignedAssetCount = db.Assets.Count(asset => asset.CompanyId == companyId && asset.AssignedEmployeeId == x.Id)
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { employee, assignedAssets, assignedAssetCount, directReports, directReportCount, page, pageSize });
        }).RequireAuthorization("people.read");

        api.MapPatch("/employees/{id:guid}/manager", async (Guid id, [FromBody] SetEmployeeManagerRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var companyLock = await db.Companies.FromSqlInterpolated(
                    $"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (companyLock is null) return Results.Forbid();
            var employee = await db.Employees.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (employee is null) return Results.NotFound();
            if (request.ManagerEmployeeId == id)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["managerEmployeeId"] = ["Bir çalışan kendisini yönetici olarak seçemez."] });

            string? managerName = null;
            if (request.ManagerEmployeeId is Guid managerId)
            {
                var manager = await db.Employees.AsNoTracking()
                    .Where(x => x.CompanyId == companyId && x.Id == managerId && x.IsActive)
                    .Select(x => new { x.Id, x.DisplayName }).SingleOrDefaultAsync(cancellationToken);
                if (manager is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["managerEmployeeId"] = ["Yönetici aynı şirkette bulunan etkin bir çalışan olmalıdır."] });
                managerName = manager.DisplayName;

                var hierarchy = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId)
                    .Select(x => new { x.Id, x.ManagerEmployeeId }).ToDictionaryAsync(x => x.Id, x => x.ManagerEmployeeId, cancellationToken);
                var visited = new HashSet<Guid>();
                Guid? ancestor = managerId;
                while (ancestor is Guid ancestorId)
                {
                    if (ancestorId == id)
                        return Results.ValidationProblem(new Dictionary<string, string[]> { ["managerEmployeeId"] = ["Bu seçim çalışan-yönetici döngüsü oluşturur."] });
                    if (!visited.Add(ancestorId))
                        return Results.Conflict(new { message = "Şirket çalışan hiyerarşisinde mevcut bir döngü bulundu; yönetici bağlantısı değiştirilmedi." });
                    ancestor = hierarchy.GetValueOrDefault(ancestorId);
                }
            }

            if (employee.ManagerEmployeeId == request.ManagerEmployeeId)
                return Results.Ok(new { employee.Id, employee.ManagerEmployeeId, ManagerName = managerName });
            employee.ManagerEmployeeId = request.ManagerEmployeeId;
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee", TargetId = id,
                Action = "ManagerChanged",
                Details = managerName is null ? "Çalışanın yöneticisi kaldırıldı." : $"Çalışanın yöneticisi {managerName} olarak değiştirildi."
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { employee.Id, employee.ManagerEmployeeId, ManagerName = managerName });
        }).RequireAuthorization("people.manage");

        api.MapPatch("/employees/{id:guid}/active", async (Guid id, [FromBody] SetEmployeeActiveRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var employee = await db.Employees.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (employee is null) return Results.NotFound();
            if (employee.IsActive == request.IsActive) return Results.Ok(new { employee.Id, employee.IsActive });
            employee.IsActive = request.IsActive;
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee", TargetId = id,
                Action = request.IsActive ? "Reactivated" : "Deactivated",
                Details = request.IsActive ? "Çalışan kaydı yeniden etkinleştirildi." : "Çalışan kaydı pasife alındı; geçmiş ve zimmet bağlantıları korundu."
            });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { employee.Id, employee.IsActive });
        }).RequireAuthorization("people.manage");

        // Department names are currently captured on employee records. This directory is a
        // live, company-scoped summary; it does not pretend those free-text values are a
        // separately managed department catalog.
        api.MapGet("/departments", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var departments = await db.Employees.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Department != null && x.Department.Trim() != "")
                .GroupBy(x => x.Department!.Trim().ToLower())
                .OrderBy(group => group.Key)
                .Select(group => new
                {
                    name = group.Min(employee => employee.Department!.Trim()),
                    employeeCount = group.Count(),
                    activeEmployeeCount = group.Count(employee => employee.IsActive),
                    inactiveEmployeeCount = group.Count(employee => !employee.IsActive)
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { items = departments, total = departments.Count });
        }).RequireAuthorization("people.read");

        api.MapPost("/assets/{id:guid}/assign", async (Guid id, [FromBody] AssignAssetRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Note is { Length: > 2000 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 2000 karakter olabilir."] });
            if (request.EmployeeId == Guid.Empty)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Etkin çalışan bu şirkette bulunamadı."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var employee = await db.Employees.FromSqlInterpolated($"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.EmployeeId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !employee.IsActive)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Etkin çalışan bu şirkette bulunamadı."] });
            var asset = await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            if (asset.AssignedEmployeeId is not null || asset.Status != AssetStatuses.Available)
                return Results.Conflict(new { message = "Yalnızca boşta ve kullanılabilir varlık zimmetlenebilir." });
            asset.AssignedEmployeeId = request.EmployeeId;
            asset.Status = AssetStatuses.Deployed;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            db.AssetEvents.Add(new AssetEvent
            {
                CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                EmployeeId = request.EmployeeId, EventType = "Assigned", Details = Clean(request.Note)
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { asset.Id, asset.AssetTag, asset.Status, asset.AssignedEmployeeId, asset.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/return", async (Guid id, [FromBody] ReturnAssetRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Note is { Length: > 2000 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 2000 karakter olabilir."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var asset = await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            if (asset.AssignedEmployeeId is not Guid employeeId)
                return Results.Conflict(new { message = "Bu varlık şu anda bir çalışana zimmetli değil." });
            asset.AssignedEmployeeId = null;
            asset.Status = AssetStatuses.Available;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            db.AssetEvents.Add(new AssetEvent
            {
                CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                EmployeeId = employeeId, EventType = "Returned", Details = Clean(request.Note)
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { asset.Id, asset.AssetTag, asset.Status, asset.AssignedEmployeeId, asset.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapPatch("/assets/{id:guid}/status", async (Guid id, [FromBody] ChangeAssetStatusRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var next = request.Status?.Trim();
            if (request.Note is { Length: > 1900 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 1900 karakter olabilir."] });
            if (next is not (AssetStatuses.Available or AssetStatuses.Maintenance or AssetStatuses.Archived))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Durum Available, Maintenance veya Archived olabilir; zimmet ayrı işlemle yapılır."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var asset = await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            if (asset.AssignedEmployeeId is not null)
                return Results.Conflict(new { message = "Önce varlığı çalışandan iade alın." });
            if (asset.Status == AssetStatuses.Archived && next != AssetStatuses.Archived)
                return Results.Conflict(new { message = "Arşivlenen varlığı tekrar kullanıma almak için ayrı geri alma akışı gereklidir." });
            if (next != AssetStatuses.Maintenance && await db.AssetMaintenances.AnyAsync(x =>
                    x.CompanyId == companyId && x.AssetId == id && x.Status == "InProgress", cancellationToken))
                return Results.Conflict(new { message = "Açık bakım kaydı tamamlanmadan varlığın durumu değiştirilemez." });
            if (asset.Status == next) return Results.Ok(new { asset.Id, asset.Status });
            var previous = asset.Status;
            asset.Status = next;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            db.AssetEvents.Add(new AssetEvent
            {
                CompanyId = companyId, AssetId = id, ActorUserId = actorId,
                EventType = "StatusChanged", Details = $"{previous} → {next}. {Clean(request.Note)}"
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { asset.Id, asset.Status, asset.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/bulk-status", async ([FromBody] BulkChangeAssetStatusRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var ids = request.AssetIds ?? [];
            var next = request.Status?.Trim();
            if (ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["1 ile 100 arasında, tekrarsız varlık seçin."] });
            if (request.Note is { Length: > 1900 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 1900 karakter olabilir."] });
            if (next is not (AssetStatuses.Available or AssetStatuses.Maintenance or AssetStatuses.Archived))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Durum Kullanılabilir, Bakımda veya Arşiv olabilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var assets = await db.Assets.FromSqlInterpolated(
                    $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);
            if (assets.Count != ids.Length)
                return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya bu şirkete ait değil. Hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => asset.AssignedEmployeeId is not null))
                return Results.Conflict(new { message = "Seçimde çalışana zimmetli varlık var. Önce iade işlemi yapın; hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => asset.Status == AssetStatuses.Archived && next != AssetStatuses.Archived))
                return Results.Conflict(new { message = "Arşivlenmiş varlıklar toplu işlemle yeniden etkinleştirilemez; hiçbir kayıt değiştirilmedi." });
            if (next != AssetStatuses.Maintenance && await db.AssetMaintenances.AnyAsync(x =>
                    x.CompanyId == companyId && ids.Contains(x.AssetId) && x.Status == "InProgress", cancellationToken))
                return Results.Conflict(new { message = "Seçimde tamamlanmamış bakım kaydı bulunan varlık var; hiçbir kayıt değiştirilmedi." });

            var now = DateTimeOffset.UtcNow;
            var changed = assets.Where(asset => asset.Status != next).ToList();
            foreach (var asset in changed)
            {
                var previous = asset.Status;
                asset.Status = next!;
                asset.UpdatedAt = now;
                var details = $"{previous} → {next}. {Clean(request.Note)}".Trim();
                db.AssetEvents.Add(new AssetEvent
                {
                    CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                    EventType = "StatusChanged", Details = details
                });
                db.AuditEntries.Add(new AuditEntry
                {
                    CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset",
                    TargetId = asset.Id, Action = "BulkStatusChanged", Details = details
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { updated = changed.Count, unchanged = assets.Count - changed.Count, status = next });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/export-selected.csv", async ([FromBody] SelectedAssetExportRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var ids = request.AssetIds ?? [];
            if (ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["1 ile 100 arasında, tekrarsız varlık seçin."] });
            var rows = await db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && ids.Contains(x.Id))
                .OrderBy(x => x.AssetTag)
                .Select(x => new SelectedAssetExportRow(
                    x.AssetTag, x.Name, x.Status,
                    db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault(),
                    db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault(),
                    db.AssetModels.Where(m => m.CompanyId == companyId && m.Id == x.ModelId).Select(m => m.Name).FirstOrDefault(),
                    x.SerialNumber, x.Location,
                    db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                    db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    x.PurchaseDate, x.PurchaseCost, x.Currency))
                .ToListAsync(cancellationToken);
            if (rows.Count != ids.Length) return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya bu şirkete ait değil." });
            var csv = new StringBuilder();
            csv.AppendLine("Etiket;Ad;Durum;Kategori;Marka;Model;Seri No;Konum;Personel No;Çalışan;Alış Tarihi;Alış Bedeli;Para Birimi");
            foreach (var row in rows)
                csv.AppendLine(string.Join(';', new[] { Cell(row.AssetTag), Cell(row.Name), Cell(row.Status),
                    Cell(row.Category), Cell(row.Brand), Cell(row.Model), Cell(row.SerialNumber),
                    Cell(row.Location), Cell(row.EmployeeNumber), Cell(row.EmployeeName),
                    Cell(row.PurchaseDate?.ToString("yyyy-MM-dd")),
                    Cell(row.PurchaseCost?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                    Cell(row.Currency) }));
            return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
                "text/csv; charset=utf-8", $"envantertr-secili-varliklar-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/activity", async ([FromQuery] int page, [FromQuery] int pageSize,
            [FromQuery] Guid? assetId, [FromQuery] Guid? employeeId,
            [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
            [FromQuery] string? eventType, [FromQuery] string? q,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            if (from is not null && to is not null && from.Value > to.Value)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var query = db.AssetEvents.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (assetId is Guid selectedAsset) query = query.Where(x => x.AssetId == selectedAsset);
            if (employeeId is Guid selectedEmployee) query = query.Where(x => x.EmployeeId == selectedEmployee);
            if (from is DateTimeOffset start) query = query.Where(x => x.OccurredAt >= start);
            if (to is DateTimeOffset end) query = query.Where(x => x.OccurredAt <= end);
            if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(x => x.EventType == eventType.Trim());
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => (x.Details != null && x.Details.Contains(term)) ||
                    db.Assets.Any(a => a.CompanyId == companyId && a.Id == x.AssetId &&
                        (a.AssetTag.Contains(term) || a.Name.Contains(term))) ||
                    db.Employees.Any(e => e.CompanyId == companyId && e.Id == x.EmployeeId &&
                        (e.DisplayName.Contains(term) || e.EmployeeNumber.Contains(term))));
            }
            var total = await query.CountAsync(cancellationToken);
            var entries = await query.OrderByDescending(x => x.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.AssetId, AssetTag = db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.AssetId).Select(a => a.AssetTag).FirstOrDefault(),
                    x.EventType, x.Details, x.EmployeeId,
                    EmployeeName = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    x.ActorUserId, ActorName = db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    x.OccurredAt
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { entries, total, page, pageSize });
        }).RequireAuthorization("asset.read");

        api.MapGet("/audit", async ([FromQuery] int page, [FromQuery] int pageSize,
            [FromQuery] string? q, [FromQuery] string? targetType,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var query = db.AuditEntries.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(targetType)) query = query.Where(x => x.TargetType == targetType.Trim());
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => x.Action.Contains(term) || (x.Details != null && x.Details.Contains(term)) ||
                    db.Users.Any(u => u.Id == x.ActorUserId && u.DisplayName.Contains(term)));
            }
            var total = await query.CountAsync(cancellationToken);
            var entries = await query.OrderByDescending(x => x.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.TargetType, x.TargetId, x.Action, x.Details, x.OccurredAt,
                    ActorName = db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault()
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { entries, total, page, pageSize });
        }).RequireAuthorization("settings.manage");

        api.MapGet("/members", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var members = await db.CompanyMemberships.AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.RoleKey == "owner" ? 0 : x.RoleKey == "admin" ? 1 : 2)
                .ThenBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.UserId, x.RoleKey, x.CreatedAt,
                    IsCurrentUser = x.UserId == actorId,
                    Name = db.Users.Where(u => u.Id == x.UserId).Select(u => u.DisplayName).FirstOrDefault(),
                    Email = db.Users.Where(u => u.Id == x.UserId).Select(u => u.Email).FirstOrDefault(),
                    IsInvitationPending = db.Users.Where(u => u.Id == x.UserId).Select(u => !u.EmailConfirmed).FirstOrDefault()
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { members, total = members.Count, roles = new[] { "admin", "asset_manager", "asset_operator", "auditor", "viewer", "hr_operator" } });
        }).RequireAuthorization("settings.manage");

        api.MapPost("/members/invite", async ([FromBody] InviteCompanyMemberRequest request,
            ClaimsPrincipal user, AppDbContext db, UserManager<EnvanterUser> userManager,
            IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var name = request.FullName?.Trim();
            var email = request.Email?.Trim();
            var roleKey = request.RoleKey?.Trim();
            var allowedRoles = new[] { "admin", "asset_manager", "asset_operator", "auditor", "viewer", "hr_operator" };
            if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || string.IsNullOrWhiteSpace(email) ||
                email.Length > 256 || !MailAddress.TryCreate(email, out _) || roleKey is null ||
                !allowedRoles.Contains(roleKey, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["invite"] = ["Ad, geçerli e-posta ve desteklenen rol gereklidir."] });
            var actorRole = user.FindFirstValue("company_role");
            if (roleKey == "admin" && actorRole != "owner") return Results.Forbid();

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                var existingMembership = await db.CompanyMemberships.SingleOrDefaultAsync(
                    x => x.CompanyId == companyId && x.UserId == existingUser.Id, cancellationToken);
                if (existingMembership is null)
                    return Results.Conflict(new { message = "Bu e-posta için davet açılamadı. E-posta adresini doğrulayın veya şirket yöneticinizle iletişime geçin." });
                if (existingUser.EmailConfirmed)
                    return Results.Conflict(new { message = "Bu kullanıcı daveti zaten kabul etmiş. Rolünü kullanıcı listesinden değiştirebilirsiniz." });
                if (existingMembership.RoleKey != roleKey)
                    return Results.Conflict(new { message = "Bekleyen davetin rolünü önce kullanıcı listesinden güncelleyin." });
                var resendToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                try
                {
                    await AccountEndpoints.SendAccountInvitationEmailAsync(email, name, resendToken, configuration, cancellationToken);
                    return Results.Ok(new { invitationSent = true, resent = true, message = "Bekleyen davet yeniden gönderildi." });
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    return Results.Ok(new { invitationSent = false, resent = true, message = "Davet kaydı duruyor ancak e-posta gönderilemedi. E-posta ayarlarını kontrol edip yeniden deneyin." });
                }
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var lockedCompany = await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedCompany is null || !lockedCompany.IsActive) return Results.Forbid();
            var now = DateTimeOffset.UtcNow;
            var subscription = await (from item in db.TenantSubscriptions.AsNoTracking()
                join plan in db.SubscriptionPlans.AsNoTracking() on item.PlanId equals plan.Id
                where item.CompanyId == companyId && (item.Status == "Trial" || item.Status == "Active") &&
                    item.StartsAt <= now && item.EndsAt > now
                select new { item.Id, plan.MaxLoginUsers }).SingleOrDefaultAsync(cancellationToken);
            if (subscription is null)
                return Results.Conflict(new { message = "Şirketin etkin deneme veya aboneliği bulunamadı. Kullanıcı davetinden önce lisansı etkinleştirin." });
            var memberCount = await db.CompanyMemberships.CountAsync(x => x.CompanyId == companyId, cancellationToken);
            if (subscription.MaxLoginUsers is int seatLimit && memberCount >= seatLimit)
                return Results.Conflict(new { message = $"Bu plan {seatLimit} giriş hesabıyla sınırlı. Kullanıcı eklemek için planı yükseltin." });

            var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!";
            var invitedUser = new EnvanterUser
            {
                UserName = email, Email = email, EmailConfirmed = false, DisplayName = name
            };
            var createResult = await userManager.CreateAsync(invitedUser, temporaryPassword);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Kullanıcı oluşturulamadı. E-posta daha önce kaydedilmiş olabilir." });
            }
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = companyId, UserId = invitedUser.Id, RoleKey = roleKey });
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "CompanyMembership", TargetId = invitedUser.Id,
                Action = "Invited", Details = $"{roleKey} rolüyle davet oluşturuldu."
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Kullanıcı daveti çakıştı. Listeyi yenileyip tekrar deneyin." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(invitedUser);
            try
            {
                await AccountEndpoints.SendAccountInvitationEmailAsync(email, name, token, configuration, cancellationToken);
                return Results.Created($"/api/members/{invitedUser.Id}", new { invitationSent = true, userId = invitedUser.Id, message = "Davet oluşturuldu; kullanıcıya parola belirleme bağlantısı gönderildi." });
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Results.Created($"/api/members/{invitedUser.Id}", new { invitationSent = false, userId = invitedUser.Id, message = "Davet kaydedildi ancak e-posta gönderilemedi. E-posta ayarlarını kontrol edip aynı daveti yeniden deneyin." });
            }
        }).RequireAuthorization("settings.manage").RequireRateLimiting("login");

        api.MapPatch("/members/{userId:guid}/role", async (Guid userId, [FromBody] ChangeMemberRoleRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var nextRole = request.RoleKey?.Trim();
            var allowedRoles = new[] { "admin", "asset_manager", "asset_operator", "auditor", "viewer", "hr_operator" };
            if (nextRole is null || !allowedRoles.Contains(nextRole, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["roleKey"] = ["Desteklenmeyen şirket rolü."] });
            if (userId == actorId)
                return Results.Conflict(new { message = "Kendi rolünüzü bu ekrandan değiştiremezsiniz; şirket erişiminizin kesilmesi önleniyor." });

            var membership = await db.CompanyMemberships.SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.UserId == userId, cancellationToken);
            if (membership is null) return Results.NotFound();
            if (membership.RoleKey == "owner")
                return Results.Conflict(new { message = "Şirket sahibi rolü normal rol düzenlemesiyle değiştirilemez." });
            var actorRole = user.FindFirstValue("company_role");
            if (actorRole != "owner" && (membership.RoleKey == "admin" || nextRole == "admin"))
                return Results.Forbid();
            if (membership.RoleKey == nextRole) return Results.Ok(new { membership.UserId, membership.RoleKey });

            var previousRole = membership.RoleKey;
            membership.RoleKey = nextRole;
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "CompanyMembership", TargetId = userId,
                Action = "RoleChanged", Details = $"{previousRole} → {nextRole}"
            });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { membership.UserId, membership.RoleKey });
        }).RequireAuthorization("settings.manage");

        api.MapGet("/assets/export.csv", async ([FromQuery] string? q, [FromQuery] int page,
            [FromQuery] int pageSize, HttpContext context, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 5000 : pageSize, 1, 10000);
            var query = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.AssetTag.Contains(term) || x.Name.Contains(term) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(term)));
            }
            var total = await query.CountAsync(cancellationToken);
            var rows = await query.OrderBy(x => x.AssetTag).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new ExportRow(x.AssetTag, x.Name, x.Status, x.SerialNumber, x.Location,
                    db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                    x.PurchaseDate, x.PurchaseCost, x.Currency)).ToListAsync(cancellationToken);
            var csv = new StringBuilder();
            csv.AppendLine("Etiket;Ad;Durum;Seri No;Konum;Personel No;Alış Tarihi;Alış Bedeli;Para Birimi");
            foreach (var row in rows)
                csv.AppendLine(string.Join(';', new[] { Cell(row.AssetTag), Cell(row.Name), Cell(row.Status),
                    Cell(row.SerialNumber), Cell(row.Location), Cell(row.EmployeeNumber),
                    Cell(row.PurchaseDate?.ToString("yyyy-MM-dd")), Cell(row.PurchaseCost?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)), Cell(row.Currency) }));
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            context.Response.Headers["X-Total-Count"] = total.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Page-Size"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Has-More"] = ((long)page * pageSize < total).ToString();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-varliklar-{DateTime.UtcNow:yyyyMMdd}-sayfa-{page}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/dashboard/summary", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var assets = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
            var statusCounts = await assets.GroupBy(x => x.Status)
                .Select(group => new { status = group.Key, count = group.Count() })
                .ToDictionaryAsync(row => row.status, row => row.count, cancellationToken);
            var summary = new
            {
                totalAssets = statusCounts.Values.Sum(),
                availableAssets = statusCounts.GetValueOrDefault(AssetStatuses.Available),
                assignedAssets = statusCounts.GetValueOrDefault(AssetStatuses.Deployed),
                maintenanceAssets = statusCounts.GetValueOrDefault(AssetStatuses.Maintenance),
                archivedAssets = statusCounts.GetValueOrDefault(AssetStatuses.Archived),
                activeEmployees = await db.Employees.CountAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken),
                recentActivityCount = await db.AssetEvents.CountAsync(x => x.CompanyId == companyId && x.OccurredAt >= DateTimeOffset.UtcNow.AddDays(-7), cancellationToken)
            };
            return Results.Ok(summary);
        }).RequireAuthorization("asset.read");

        return endpoints;
    }

    private sealed record ExportRow(string AssetTag, string Name, string Status, string? SerialNumber,
        string? Location, string? EmployeeNumber, DateOnly? PurchaseDate, decimal? PurchaseCost, string Currency);
    private sealed record UpdateCompanyProfileRequest(string? Name, string? ContactEmail, string? BrandPrimaryColor);
    public sealed record LabelPrintSettingsRequest(string? Barcode1d, string? Barcode2d, decimal WidthMm,
        decimal HeightMm, decimal OffsetXmm, decimal OffsetYmm, decimal PaddingMm, decimal CodeSizeMm,
        string? QrTarget, bool ShowName, bool ShowSerial, bool ShowModel, bool ShowLocation, bool ShowStatus);
    private static readonly LabelPrintSettingsRequest DefaultLabelPrintSettings = new("code128", "qrcode", 86, 48,
        0, 0, 4, 25, "tag", true, true, false, false, false);

    private static (string? ContentType, string Extension) DetectLogo(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", "png");
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return ("image/jpeg", "jpg");
        if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") return ("image/webp", "webp");
        return (null, "");
    }

    private static bool TryScope(ClaimsPrincipal user, out Guid companyId, out Guid actorId)
    {
        companyId = Guid.Empty;
        actorId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("company_id"), out companyId) &&
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Cell(string? value)
    {
        var safe = value?.Trim() ?? "";
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }

    private sealed record SelectedAssetExportRow(string AssetTag, string Name, string Status, string? Category,
        string? Brand, string? Model, string? SerialNumber, string? Location, string? EmployeeNumber,
        string? EmployeeName, DateOnly? PurchaseDate, decimal? PurchaseCost, string Currency);
}
