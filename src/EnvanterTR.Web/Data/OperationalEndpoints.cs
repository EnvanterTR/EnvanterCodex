using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.Net.Mail;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace EnvanterTR.Web.Data;

public static class OperationalEndpoints
{
    private const long CompanyLogoRequestLimitBytes = 2_162_688; // 2 MiB logo plus bounded multipart overhead.
    private const long EmployeePhotoMaxSizeBytes = 2_097_152;
    private const long EmployeeDocumentMaxSizeBytes = 10_485_760;
    private const long EmployeeDocumentRequestLimitBytes = 10_551_296;

    public static IEndpointRouteBuilder MapOperationalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api").RequireAuthorization();
        api.MapCompanyLocationAccessEndpoints();
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
            var requestPath = invocation.HttpContext.Request.Path;
            if (requestPath.StartsWithSegments("/api/employees") &&
                Guid.TryParse(Convert.ToString(invocation.HttpContext.Request.RouteValues.GetValueOrDefault("id")), out var employeeId))
            {
                var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId,
                    invocation.HttpContext.RequestAborted);
                if (allowedLocations is not null || await CompanyLocationAccess.GetEffectiveCompanyUnitIdsAsync(db,
                        companyId, actorId, invocation.HttpContext.RequestAborted) is not null)
                {
                    var employeeLocation = await db.Employees.AsNoTracking()
                        .Where(x => x.CompanyId == companyId && x.Id == employeeId)
                        .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(invocation.HttpContext.RequestAborted);
                    if (employeeLocation is not null && !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId,
                            actorId, employeeLocation.LocationCatalogId, employeeLocation.CompanyUnitId,
                            invocation.HttpContext.RequestAborted))
                        return Results.NotFound();
                }
            }
            if (TenantPlanFeatures.RequiresReports(invocation.HttpContext.Request.Path) &&
                !await TenantPlanFeatures.IsEnabledAsync(companyId, TenantPlanFeatures.Reports, db,
                    invocation.HttpContext.RequestAborted))
                return TenantPlanFeatures.FeatureUnavailable(TenantPlanFeatures.Reports);
            if (HttpMethods.IsGet(invocation.HttpContext.Request.Method) || HttpMethods.IsHead(invocation.HttpContext.Request.Method))
                return await next(invocation);
            var antiforgery = invocation.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try { await antiforgery.ValidateRequestAsync(invocation.HttpContext); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "İstek doğrulaması başarısız." }); }
            return await next(invocation);
        });

        api.MapGet("/company/storage-usage", (ClaimsPrincipal user, IConfiguration configuration,
            ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            try
            {
                return Results.Ok(CompanyMediaUsage.Measure(configuration["Storage:CompanyMediaPath"] ??
                    "/var/lib/envantertr/company-media", companyId, cancellationToken));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or OverflowException)
            {
                loggerFactory.CreateLogger("CompanyMediaUsage").LogWarning(error,
                    "Company media usage could not be measured for {CompanyId}", companyId);
                return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Şirket dosya kullanımı şu anda hesaplanamıyor.");
            }
        }).RequireAuthorization("settings.manage");

        api.MapGet("/company/profile", async (ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var company = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => new { x.Name, x.Slug, x.ContactEmail, x.BrandPrimaryColor, HasLogo = x.BrandLogoPath != null })
                .SingleOrDefaultAsync(cancellationToken);
            if (company is null) return Results.NotFound();
            var rootDomain = configuration["TenantRouting:RootDomain"]?.Trim().Trim('.');
            return Results.Ok(new { company.Name, company.Slug, company.ContactEmail, company.BrandPrimaryColor, company.HasLogo,
                tenantDomain = string.IsNullOrWhiteSpace(rootDomain) ? null : rootDomain,
                tenantUrl = string.IsNullOrWhiteSpace(rootDomain) || string.IsNullOrWhiteSpace(company.Slug)
                    ? null : $"https://{company.Slug}.{rootDomain}" });
        }).RequireAuthorization("settings.manage");

        api.MapGet("/company/branding", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
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
            try { return Results.Ok(new { isConfigured = true, settings = ReadLabelPrintDocument(raw).Settings }); }
            catch (JsonException) { return Results.Ok(new { isConfigured = false, settings = DefaultLabelPrintSettings }); }
        }).RequireAuthorization("company.member");

        api.MapGet("/company/label-templates", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var raw = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => x.LabelPrintSettingsJson).SingleOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(raw)) return Results.Ok(Array.Empty<LabelPrintTemplateRequest>());
            try { return Results.Ok(ReadLabelPrintDocument(raw).Templates); }
            catch (JsonException) { return Results.Ok(Array.Empty<LabelPrintTemplateRequest>()); }
        }).RequireAuthorization("company.member");

        api.MapPut("/company/label-settings", async ([FromBody] LabelPrintSettingsRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Barcode1d is not ("code128" or "code39" or "code93" or "code93ext" or "rationalizedCodabar" or "msi" or "ean5" or "ean13" or "upca" or "upce" or "none") ||
                request.Barcode2d is not ("qrcode" or "pdf417" or "datamatrix" or "azteccode" or "none") ||
                request.QrTarget is not ("tag" or "asset-url") ||
                request.WidthMm is < 25 or > 150 || request.HeightMm is < 15 or > 100 ||
                request.OffsetXmm is < -30 or > 30 || request.OffsetYmm is < -30 or > 30 ||
                request.PaddingMm is < 0 or > 15 || request.CodeSizeMm is < 10 or > 45)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["settings"] = ["Barkod türü ve yazdırma ölçülerini izin verilen aralıklarla kontrol edin."] });
            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            var existing = string.IsNullOrWhiteSpace(company.LabelPrintSettingsJson)
                ? new LabelPrintSettingsDocument(DefaultLabelPrintSettings, [])
                : ReadLabelPrintDocument(company.LabelPrintSettingsJson);
            company.LabelPrintSettingsJson = JsonSerializer.Serialize(existing with { Settings = request });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Company", TargetId = companyId, Action = "LabelSettingsUpdated",
                Details = "Şirketin barkod ve etiket yazdırma tercihleri güncellendi." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { isConfigured = true, settings = request });
        }).RequireAuthorization("settings.manage");

        api.MapPut("/company/label-templates", async ([FromBody] LabelPrintTemplatesRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var templates = request.Templates ?? [];
            if (templates.Count > 100 || templates.Any(template => template.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(template.Name) || template.Name.Trim().Length > 80 ||
                (template.Manufacturer?.Length ?? 0) > 80 || (template.PrinterModel?.Length ?? 0) > 120 ||
                (template.ProductCode?.Length ?? 0) > 80 || template.Settings is null || !IsValidLabelSettings(template.Settings)) ||
                templates.Select(template => template.Id).Distinct().Count() != templates.Count ||
                templates.Select(template => string.Join("|", template.Manufacturer?.Trim(), template.PrinterModel?.Trim(), template.ProductCode?.Trim(), template.Name.Trim()))
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count() != templates.Count)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["templates"] = ["Etiket şablonlarını kontrol edin. En fazla 100 benzersiz şablon kaydedilebilir; her şablon geçerli yazdırma ayarı içermelidir."] });
            if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(templates)) > 128 * 1024)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["templates"] = ["Etiket şablonları toplamı 128 KiB sınırını aşamaz."] });
            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            var existing = string.IsNullOrWhiteSpace(company.LabelPrintSettingsJson)
                ? new LabelPrintSettingsDocument(DefaultLabelPrintSettings, [])
                : ReadLabelPrintDocument(company.LabelPrintSettingsJson);
            company.LabelPrintSettingsJson = JsonSerializer.Serialize(existing with { Templates = templates });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Company", TargetId = companyId, Action = "LabelTemplatesUpdated",
                Details = $"Şirketin yazıcı/etiket şablon kataloğu güncellendi ({templates.Count} şablon)." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(templates);
        }).RequireAuthorization("settings.manage");

        api.MapPut("/company/profile", async ([FromBody] UpdateCompanyProfileRequest request,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var name = request.Name?.Trim();
            var email = request.ContactEmail?.Trim();
            var color = request.BrandPrimaryColor?.Trim();
            var slug = string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim().ToLowerInvariant();
            var tenantRoutingEnabled = !string.IsNullOrWhiteSpace(configuration["TenantRouting:RootDomain"]);
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200 ||
                (email is { Length: > 256 } || (!string.IsNullOrEmpty(email) && !MailAddress.TryCreate(email, out _))) ||
                color is null || !Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant) ||
                (tenantRoutingEnabled && slug is null) || (slug is not null && !TenantHostResolver.IsValidSlug(slug)))
                return Results.ValidationProblem(new Dictionary<string, string[]> {
                    ["profile"] = ["Şirket adı, geçerli e-posta, #RRGGBB rengi ve 2-63 karakterli uygun alt alan adı gereklidir."]
                });
            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            if (slug is not null && await db.Companies.AsNoTracking().AnyAsync(x => x.Id != companyId && x.Slug == slug, cancellationToken))
                return Results.Conflict(new { message = "Bu alt alan adı başka bir şirket tarafından kullanılıyor." });
            company.Name = name;
            if (slug is not null) company.Slug = slug;
            company.ContactEmail = string.IsNullOrWhiteSpace(email) ? null : email;
            company.BrandPrimaryColor = color.ToUpperInvariant();
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Company", TargetId = companyId, Action = "ProfileUpdated",
                Details = "Şirket adı, iletişim e-postası veya tema rengi güncellendi." });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Results.Conflict(new { message = "Bu alt alan adı başka bir şirket tarafından kullanılıyor." });
            }
            var rootDomain = configuration["TenantRouting:RootDomain"]?.Trim().Trim('.');
            return Results.Ok(new { company.Name, company.Slug, company.ContactEmail, company.BrandPrimaryColor,
                HasLogo = company.BrandLogoPath is not null,
                tenantUrl = string.IsNullOrWhiteSpace(rootDomain) ? null : $"https://{company.Slug}.{rootDomain}" });
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
            [FromQuery] DateOnly? startedFrom, [FromQuery] DateOnly? startedTo,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var scopedEmployees = EmployeesInScope(db, companyId, employeeAccessScope);
            var query = scopedEmployees;
            if (startedFrom is DateOnly start && startedTo is DateOnly end && start > end)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["startedFrom"] = ["İşe başlama başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (active is bool isActive) query = query.Where(x => x.IsActive == isActive);
            if (startedFrom is DateOnly from) query = query.Where(x => x.StartedOn >= from);
            if (startedTo is DateOnly to) query = query.Where(x => x.StartedOn <= to);
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
                var searchPattern = $"%{term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
                query = query.Where(x => EF.Functions.ILike(x.EmployeeNumber, searchPattern, "\\") || EF.Functions.ILike(x.DisplayName, searchPattern, "\\") ||
                    (x.Email != null && EF.Functions.ILike(x.Email, searchPattern, "\\")) || (x.Department != null && EF.Functions.ILike(x.Department, searchPattern, "\\")) ||
                    (x.JobTitle != null && EF.Functions.ILike(x.JobTitle, searchPattern, "\\")) || (x.Phone != null && EF.Functions.ILike(x.Phone, searchPattern, "\\")) ||
                    (allowedLocations == null
                        ? x.Location != null && EF.Functions.ILike(x.Location, searchPattern, "\\")
                        : db.CompanyLocations.Any(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                            allowedLocations.Contains(location.Id) && EF.Functions.ILike(location.Name, searchPattern, "\\"))) ||
                    (x.CompanyUnitId.HasValue && db.CompanyUnits.Any(unit => unit.CompanyId == companyId && unit.Id == x.CompanyUnitId &&
                        (allowedCompanyUnits == null || allowedCompanyUnits.Contains(unit.Id)) && EF.Functions.ILike(unit.Name, searchPattern, "\\"))) ||
                    db.CompanyLocations.Any(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                        (allowedLocations == null || allowedLocations.Contains(location.Id)) && location.CompanyUnitId.HasValue &&
                        db.CompanyUnits.Any(unit => unit.CompanyId == companyId && unit.Id == location.CompanyUnitId && EF.Functions.ILike(unit.Name, searchPattern, "\\"))) ||
                    scopedEmployees.Any(manager => manager.Id == x.ManagerEmployeeId &&
                        EF.Functions.ILike(manager.DisplayName, searchPattern, "\\")));
            }
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderBy(x => x.DisplayName).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.EmployeeNumber, x.DisplayName, x.Email, x.Department, x.JobTitle, x.Phone,
                    Location = allowedLocations == null ? x.Location : db.CompanyLocations
                        .Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId && allowedLocations.Contains(location.Id))
                        .Select(location => location.Name).FirstOrDefault(),
                    CompanyUnitId = x.CompanyUnitId ?? db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                            (allowedLocations == null || allowedLocations.Contains(location.Id)))
                        .Select(location => location.CompanyUnitId).FirstOrDefault(),
                    CompanyUnitName = x.CompanyUnitId.HasValue
                        ? db.CompanyUnits.Where(unit => unit.CompanyId == companyId && unit.Id == x.CompanyUnitId &&
                            (allowedCompanyUnits == null || allowedCompanyUnits.Contains(unit.Id))).Select(unit => unit.Name).FirstOrDefault()
                        : db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                            (allowedLocations == null || allowedLocations.Contains(location.Id)))
                        .Select(location => db.CompanyUnits.Where(unit => unit.CompanyId == companyId && unit.Id == location.CompanyUnitId)
                            .Select(unit => unit.Name).FirstOrDefault()).FirstOrDefault(),
                    x.LocationCatalogId, x.StartedOn, x.IsActive,
                    ManagerEmployeeId = !x.ManagerEmployeeId.HasValue || scopedEmployees.Any(manager => manager.Id == x.ManagerEmployeeId)
                        ? x.ManagerEmployeeId : null,
                    ManagerName = scopedEmployees.Where(manager => manager.Id == x.ManagerEmployeeId)
                        .Select(manager => manager.DisplayName).FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { items, total, page, pageSize });
        }).RequireAuthorization("people.read");

        var exportEmployees = async Task<IResult> ([FromQuery] string? q, [FromQuery] string? department,
            [FromQuery] Guid? managerId, [FromQuery] bool? active,
            [FromQuery] DateOnly? startedFrom, [FromQuery] DateOnly? startedTo, [FromQuery] string? language, HttpContext context,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var exportLanguage = (language ?? "tr").Trim().ToLowerInvariant();
            if (exportLanguage is not ("tr" or "en" or "ru" or "uk" or "mk"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["language"] = ["Dil tr, en, ru, uk veya mk olmalıdır."] });
            if (q?.Trim().Length > 160 || department?.Trim().Length > 160)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["filter"] = ["Arama ve departman filtresi 160 karakteri aşamaz."] });

            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var scopedEmployees = EmployeesInScope(db, companyId, employeeAccessScope);
            var query = scopedEmployees;
            if (startedFrom is DateOnly start && startedTo is DateOnly end && start > end)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["startedFrom"] = ["İşe başlama başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (active is bool isActive) query = query.Where(x => x.IsActive == isActive);
            if (startedFrom is DateOnly from) query = query.Where(x => x.StartedOn >= from);
            if (startedTo is DateOnly to) query = query.Where(x => x.StartedOn <= to);
            if (managerId is Guid selectedManager) query = query.Where(x => x.ManagerEmployeeId == selectedManager);
            if (!string.IsNullOrWhiteSpace(department))
            {
                var selectedDepartment = department.Trim();
                var pattern = selectedDepartment.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                query = query.Where(x => x.Department != null && EF.Functions.ILike(x.Department.Trim(), pattern, "\\"));
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                var searchPattern = $"%{term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
                query = query.Where(x => EF.Functions.ILike(x.EmployeeNumber, searchPattern, "\\") || EF.Functions.ILike(x.DisplayName, searchPattern, "\\") ||
                    (x.Email != null && EF.Functions.ILike(x.Email, searchPattern, "\\")) || (x.Department != null && EF.Functions.ILike(x.Department, searchPattern, "\\")) ||
                    (x.JobTitle != null && EF.Functions.ILike(x.JobTitle, searchPattern, "\\")) || (x.Phone != null && EF.Functions.ILike(x.Phone, searchPattern, "\\")) ||
                    (allowedLocations == null
                        ? x.Location != null && EF.Functions.ILike(x.Location, searchPattern, "\\")
                        : db.CompanyLocations.Any(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                            allowedLocations.Contains(location.Id) && EF.Functions.ILike(location.Name, searchPattern, "\\"))) ||
                    (x.CompanyUnitId.HasValue && db.CompanyUnits.Any(unit => unit.CompanyId == companyId && unit.Id == x.CompanyUnitId &&
                        (allowedCompanyUnits == null || allowedCompanyUnits.Contains(unit.Id)) && EF.Functions.ILike(unit.Name, searchPattern, "\\"))) ||
                    scopedEmployees.Any(manager => manager.Id == x.ManagerEmployeeId &&
                        EF.Functions.ILike(manager.DisplayName, searchPattern, "\\")));
            }

            var total = await query.CountAsync(cancellationToken);
            if (total > 50000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["Tek dosyada en fazla 50.000 çalışan dışa aktarılabilir; filtreleri daraltın."] });
            var rows = await query.OrderBy(x => x.DisplayName)
                .Select(x => new
                {
                    x.EmployeeNumber, x.DisplayName, x.Email, x.Department, x.JobTitle, x.Phone,
                    Location = allowedLocations == null ? x.Location : db.CompanyLocations
                        .Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId && allowedLocations.Contains(location.Id))
                        .Select(location => location.Name).FirstOrDefault(),
                    x.StartedOn, x.IsActive,
                    ManagerName = scopedEmployees.Where(manager => manager.Id == x.ManagerEmployeeId)
                        .Select(manager => manager.DisplayName).FirstOrDefault(),
                    ManagerEmployeeNumber = scopedEmployees.Where(manager => manager.Id == x.ManagerEmployeeId)
                        .Select(manager => manager.EmployeeNumber).FirstOrDefault(),
                    x.CreatedAt
                }).ToListAsync(cancellationToken);
            static string Cell(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "\"\"";
                var trimmed = value.TrimStart();
                var safe = trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
                return "\"" + safe.Replace("\"", "\"\"") + "\"";
            }
            if (context.Request.Path.Value?.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var workbook = new XlsxExportWriter(EmployeeCsvImportMapping.ExportHeaders(exportLanguage));
                foreach (var row in rows)
                    workbook.WriteRow([row.EmployeeNumber, row.DisplayName, row.Email, row.Department, row.JobTitle, row.Phone,
                        row.Location, row.StartedOn?.ToString("yyyy-MM-dd"), row.ManagerName,
                        EmployeeCsvImportMapping.ExportStatus(row.IsActive, exportLanguage), row.CreatedAt.ToString("O"), row.ManagerEmployeeNumber]);
                context.Response.Headers["X-Total-Count"] = total.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await workbook.SendAsync(context, $"envantertr-calisanlar-{DateTime.UtcNow:yyyyMMdd}.xlsx", cancellationToken);
                return Results.Empty;
            }
            var csv = new StringBuilder("\uFEFF" + string.Join(',', EmployeeCsvImportMapping.ExportHeaders(exportLanguage).Select(Cell)) + "\r\n");
            foreach (var row in rows)
                csv.AppendJoin(',', new[] { Cell(row.EmployeeNumber), Cell(row.DisplayName), Cell(row.Email),
                    Cell(row.Department), Cell(row.JobTitle), Cell(row.Phone), Cell(row.Location),
                    Cell(row.StartedOn?.ToString("yyyy-MM-dd")), Cell(row.ManagerName), Cell(EmployeeCsvImportMapping.ExportStatus(row.IsActive, exportLanguage)),
                    Cell(row.CreatedAt.ToString("O")), Cell(row.ManagerEmployeeNumber) }).Append("\r\n");
            context.Response.Headers["X-Total-Count"] = total.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-calisanlar-{DateTime.UtcNow:yyyyMMdd}.csv");
        };
        api.MapGet("/employees/export.csv", exportEmployees).RequireAuthorization("people.read");
        api.MapGet("/employees/export.xlsx", exportEmployees).RequireAuthorization("people.read");

        api.MapPost("/employees", async ([FromBody] CreateEmployeeRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var number = request.EmployeeNumber?.Trim().ToUpperInvariant();
            var displayName = request.DisplayName?.Trim();
            var email = Clean(request.Email);
            var department = Clean(request.Department);
            var jobTitle = Clean(request.JobTitle);
            var phone = Clean(request.Phone);
            var location = Clean(request.Location) ?? await AssetEndpoints.GetDefaultLocationPathAsync(db, companyId, cancellationToken);
            var locationCatalogId = await AssetEndpoints.ResolveLocationCatalogIdAsync(db, companyId, location, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            if (string.IsNullOrWhiteSpace(number) || number.Length > 80 ||
                string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200 ||
                email is { Length: > 256 } || department is { Length: > 160 } || jobTitle is { Length: > 160 } ||
                phone is { Length: > 40 } || location is { Length: > 160 })
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["employee"] = ["Personel numarası ve görünen ad zorunludur; numara 80, ad 200 karakteri aşamaz."]
                });
            if (!employeeAccessScope.Allows(locationCatalogId, request.CompanyUnitId))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["location"] = ["Çalışan yalnızca erişim kapsamınızdaki etkin bir şirket konumuna eklenebilir."] });
            if (request.CompanyUnitId is Guid companyUnitId &&
                !await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.Id == companyUnitId && x.IsActive &&
                    (allowedCompanyUnits == null || allowedCompanyUnits.Contains(x.Id)), cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["companyUnitId"] = ["Şirket birimi aynı şirkette, etkin ve erişim kapsamınızda olmalıdır."] });
            if (department is not null && await db.CompanyDepartments.AnyAsync(x => x.CompanyId == companyId && !x.IsActive && x.NormalizedName == NormalizeDepartmentName(department), cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["department"] = ["Arşivlenmiş departmana yeni çalışan atanamaz. Etkin bir departman seçin."] });
            if (await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.EmployeeNumber == number, cancellationToken))
                return Results.Conflict(new { message = "Bu personel numarası şirket içinde kullanılıyor." });
            if (request.ManagerEmployeeId is Guid managerId &&
                !await EmployeesInScope(db, companyId, employeeAccessScope)
                    .AnyAsync(x => x.Id == managerId && x.IsActive, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["managerEmployeeId"] = ["Yönetici erişilebilir konumda bulunan aynı şirketin etkin bir çalışanı olmalıdır."] });
            var employee = new Employee
            {
                CompanyId = companyId, EmployeeNumber = number, DisplayName = displayName,
                Email = email, Department = department, JobTitle = jobTitle, Phone = phone,
                Location = location, LocationCatalogId = locationCatalogId, CompanyUnitId = request.CompanyUnitId,
                StartedOn = request.StartedOn, ManagerEmployeeId = request.ManagerEmployeeId
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
                employee.JobTitle, employee.Phone, employee.Location, employee.LocationCatalogId, employee.StartedOn,
                employee.ManagerEmployeeId, employee.CompanyUnitId, employee.IsActive
            });
        }).RequireAuthorization("people.manage");

        api.MapPatch("/employees/{id:guid}", async (Guid id, [FromBody] UpdateEmployeeRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var employee = await db.Employees.FromSqlInterpolated($"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (employee is null) return Results.NotFound();
            if (!EmployeeIsInScope(employee.LocationCatalogId, employee.CompanyUnitId, employeeAccessScope)) return Results.NotFound();
            var companyUnitId = request.CompanyUnitId ?? (request.ClearCompanyUnit ? null : employee.CompanyUnitId);
            var number = request.EmployeeNumber?.Trim().ToUpperInvariant();
            var displayName = request.DisplayName?.Trim();
            var email = Clean(request.Email);
            var department = Clean(request.Department);
            var jobTitle = Clean(request.JobTitle);
            var phone = Clean(request.Phone);
            var location = Clean(request.Location);
            var locationCatalogId = await AssetEndpoints.ResolveLocationCatalogIdAsync(db, companyId, location, cancellationToken);
            if (string.IsNullOrWhiteSpace(number) || number.Length > 80 ||
                string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200 ||
                email is { Length: > 256 } || department is { Length: > 160 } || jobTitle is { Length: > 160 } ||
                phone is { Length: > 40 } || location is { Length: > 160 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employee"] = ["Personel numarası ve görünen ad zorunludur; numara 80, ad 200 karakteri aşamaz."] });
            if (!employeeAccessScope.Allows(locationCatalogId, companyUnitId))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["location"] = ["Çalışan yalnızca erişim kapsamınızdaki etkin bir şirket konumuna taşınabilir."] });
            if (companyUnitId is Guid requestedUnitId &&
                !await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.Id == requestedUnitId && x.IsActive &&
                    (allowedCompanyUnits == null || allowedCompanyUnits.Contains(x.Id)), cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["companyUnitId"] = ["Şirket birimi aynı şirkette, etkin ve erişim kapsamınızda olmalıdır."] });
            if (department is not null && await db.CompanyDepartments.AnyAsync(x => x.CompanyId == companyId && !x.IsActive && x.NormalizedName == NormalizeDepartmentName(department), cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["department"] = ["Arşivlenmiş departmana yeni çalışan atanamaz. Etkin bir departman seçin."] });
            if (await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.EmployeeNumber == number, cancellationToken))
                return Results.Conflict(new { message = "Bu personel numarası şirket içinde kullanılıyor." });
            var changes = new List<string>();
            if (employee.EmployeeNumber != number) { changes.Add("personel no"); employee.EmployeeNumber = number; }
            if (employee.DisplayName != displayName) { changes.Add("ad soyad"); employee.DisplayName = displayName; }
            if (employee.Email != email) { changes.Add("e-posta"); employee.Email = email; }
            if (employee.Department != department) { changes.Add("departman"); employee.Department = department; }
            if (employee.JobTitle != jobTitle) { changes.Add("görev unvanı"); employee.JobTitle = jobTitle; }
            if (employee.Phone != phone) { changes.Add("telefon"); employee.Phone = phone; }
            if (employee.Location != location) { changes.Add("konum"); employee.Location = location; }
            if (employee.LocationCatalogId != locationCatalogId) { changes.Add("konum kataloğu bağlantısı"); employee.LocationCatalogId = locationCatalogId; }
            if (employee.CompanyUnitId != companyUnitId) { changes.Add("şirket birimi"); employee.CompanyUnitId = companyUnitId; }
            if (employee.StartedOn != request.StartedOn) { changes.Add("işe başlama tarihi"); employee.StartedOn = request.StartedOn; }
            if (changes.Count == 0) return Results.Ok(new { employee.Id, employee.EmployeeNumber, employee.DisplayName, employee.Email, employee.Department, employee.JobTitle, employee.Phone, employee.Location, employee.LocationCatalogId, employee.CompanyUnitId, employee.StartedOn });
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee", TargetId = id,
                Action = "Updated", Details = "Güncellenen alanlar: " + string.Join(", ", changes)
            });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Çalışan kaydı çakıştı." }); }
            return Results.Ok(new { employee.Id, employee.EmployeeNumber, employee.DisplayName, employee.Email, employee.Department, employee.JobTitle, employee.Phone, employee.Location, employee.LocationCatalogId, employee.CompanyUnitId, employee.StartedOn });
        }).RequireAuthorization("people.manage");

        api.MapGet("/employees/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var scopedEmployees = EmployeesInScope(db, companyId, employeeAccessScope);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);
            var employee = await scopedEmployees.Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id, x.EmployeeNumber, x.DisplayName, x.Email, x.Department, x.JobTitle, x.Phone,
                    Location = allowedLocations == null ? x.Location : db.CompanyLocations
                        .Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId && allowedLocations.Contains(location.Id))
                        .Select(location => location.Name).FirstOrDefault(),
                    CompanyUnitId = x.CompanyUnitId ?? db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                            (allowedLocations == null || allowedLocations.Contains(location.Id)))
                        .Select(location => location.CompanyUnitId).FirstOrDefault(),
                    CompanyUnitName = x.CompanyUnitId.HasValue
                        ? db.CompanyUnits.Where(unit => unit.CompanyId == companyId && unit.Id == x.CompanyUnitId &&
                            (allowedCompanyUnits == null || allowedCompanyUnits.Contains(unit.Id))).Select(unit => unit.Name).FirstOrDefault()
                        : db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == x.LocationCatalogId &&
                            (allowedLocations == null || allowedLocations.Contains(location.Id)))
                        .Select(location => db.CompanyUnits.Where(unit => unit.CompanyId == companyId && unit.Id == location.CompanyUnitId)
                            .Select(unit => unit.Name).FirstOrDefault()).FirstOrDefault(),
                    x.LocationCatalogId, x.StartedOn, x.IsActive, x.CreatedAt,
                    ManagerEmployeeId = !x.ManagerEmployeeId.HasValue || scopedEmployees.Any(manager => manager.Id == x.ManagerEmployeeId)
                        ? x.ManagerEmployeeId : null,
                    ManagerName = scopedEmployees.Where(manager => manager.Id == x.ManagerEmployeeId)
                        .Select(manager => manager.DisplayName).FirstOrDefault()
            })
                .SingleOrDefaultAsync(cancellationToken);
            if (employee is null) return Results.NotFound();
            var role = user.FindFirstValue("company_role");
            var canReadAssetData = role is "owner" or "admin" or "asset_manager" or "asset_operator" or "auditor" or "viewer";
            var assignedQuery = db.Assets.AsNoTracking().Where(x => canReadAssetData && x.CompanyId == companyId && x.AssignedEmployeeId == id)
                .AssetsInLocationScope(companyId, allowedLocations);
            var assignedAssetCount = await assignedQuery.CountAsync(cancellationToken);
            var assignedAssets = await assignedQuery.OrderBy(x => x.AssetTag)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.AssetTag, x.Name, x.Status, x.Location })
                .ToListAsync(cancellationToken);
            var accessibleAccessories = db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations is not null)
                accessibleAccessories = accessibleAccessories.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
            var accessoryQuery = from assignment in db.AccessoryAssignments.AsNoTracking()
                                 join accessory in accessibleAccessories
                                     on new { assignment.CompanyId, Id = assignment.AccessoryId } equals new { accessory.CompanyId, Id = accessory.Id }
                                 where canReadAssetData && assignment.CompanyId == companyId && assignment.EmployeeId == id && assignment.ReturnedQuantity < assignment.Quantity
                                 orderby assignment.CheckedOutAt descending
                                 select new
                                 {
                                     assignment.Id, accessoryId = accessory.Id, accessoryName = accessory.Name, accessory.ModelNumber,
                                     assignment.Quantity, assignment.ReturnedQuantity,
                                     remaining = assignment.Quantity - assignment.ReturnedQuantity,
                                     assignment.CheckedOutAt, assignment.Notes
                                 };
            var assignedAccessoryCount = await accessoryQuery.CountAsync(cancellationToken);
            var assignedAccessories = await accessoryQuery.Take(1000).ToListAsync(cancellationToken);

            var licenseQuery = from assignment in db.SoftwareLicenseAssignments.AsNoTracking()
                               join license in db.SoftwareLicenses.AsNoTracking()
                                   on new { assignment.CompanyId, Id = assignment.LicenseId } equals new { license.CompanyId, Id = license.Id }
                               where canReadAssetData && assignment.CompanyId == companyId && assignment.EmployeeId == id && assignment.RevokedAt == null &&
                                   (allowedLocations == null || !assignment.AssetId.HasValue || db.Assets.Any(asset => asset.CompanyId == companyId &&
                                       asset.Id == assignment.AssetId && asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)))
                               orderby assignment.AssignedAt descending
                               select new
                               {
                                   assignment.Id, licenseId = license.Id, license.Name, license.Publisher,
                                   license.ProductVersion, license.ExpirationDate, assignment.AssignedAt, assignment.Notes
                               };
            var activeLicenseCount = await licenseQuery.CountAsync(cancellationToken);
            var activeLicenses = await licenseQuery.Take(100).ToListAsync(cancellationToken);

            var consumableQuery = from movement in db.ConsumableMovements.AsNoTracking()
                                  join consumable in db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId &&
                                      (allowedLocations == null || x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value)))
                                      on new { movement.CompanyId, Id = movement.ConsumableId } equals new { consumable.CompanyId, Id = consumable.Id }
                                  where canReadAssetData && movement.CompanyId == companyId && movement.EmployeeId == id && movement.MovementType == "Checkout"
                                  orderby movement.OccurredAt descending
                                  select new { movement.Id, consumableName = consumable.Name, movement.Quantity, movement.OccurredAt, movement.Notes };
            var consumableCheckoutCount = await consumableQuery.CountAsync(cancellationToken);
            var consumableCheckouts = await consumableQuery.Take(100).ToListAsync(cancellationToken);

            var visibleMaintenances = db.AssetMaintenances.AsNoTracking().Where(x => x.CompanyId == companyId)
                .InLocationScope(db, companyId, allowedLocations);
            var maintenanceQuery = from maintenance in visibleMaintenances
                                   join asset in db.Assets.AsNoTracking()
                                       on new { maintenance.CompanyId, Id = maintenance.AssetId }
                                       equals new { asset.CompanyId, Id = asset.Id }
                                   where canReadAssetData && maintenance.CompanyId == companyId &&
                                         maintenance.ResponsibleEmployeeId == id
                                   orderby maintenance.UpdatedAt descending, maintenance.Id
                                   select new
                                   {
                                       maintenance.Id,
                                       assetId = asset.Id,
                                       asset.AssetTag,
                                       assetName = asset.Name,
                                       maintenance.MaintenanceType,
                                       maintenance.Status,
                                       maintenance.PlannedOn,
                                       maintenance.StartedOn,
                                       maintenance.CompletedOn,
                                       maintenance.Supplier
                                   };
            var responsibleMaintenanceCount = await maintenanceQuery.CountAsync(cancellationToken);
            var responsibleMaintenances = await maintenanceQuery.Take(100).ToListAsync(cancellationToken);

            var reportsQuery = EmployeesInScope(db, companyId, employeeAccessScope)
                .Where(x => x.ManagerEmployeeId == id);
            var directReportCount = await reportsQuery.CountAsync(cancellationToken);
            var directReports = await reportsQuery.OrderBy(x => x.DisplayName).Take(100)
                .Select(x => new
                {
                    x.Id, x.EmployeeNumber, x.DisplayName, x.Department, x.IsActive,
                    AssignedAssetCount = canReadAssetData ? db.Assets.Count(asset => asset.CompanyId == companyId && asset.AssignedEmployeeId == x.Id &&
                        (allowedLocations == null || asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value))) : 0
                }).ToListAsync(cancellationToken);
            return Results.Ok(new
            {
                employee, assignedAssets, assignedAssetCount,
                assignedAccessories, assignedAccessoryCount,
                activeLicenses, activeLicenseCount,
                consumableCheckouts, consumableCheckoutCount,
                responsibleMaintenances, responsibleMaintenanceCount,
                directReports, directReportCount, assetsVisible = canReadAssetData, page, pageSize
            });
        }).RequireAuthorization("people.read");

        api.MapGet("/employees/{id:guid}/private-notes", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId,
                    employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            return Results.Ok(new { notes = employee.PrivateNotes, maxLength = 4000 });
        }).RequireAuthorization("people.files.read");

        api.MapPut("/employees/{id:guid}/private-notes", async (Guid id,
            [FromBody] UpdateEmployeePrivateNotesRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var employee = await db.Employees.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId, employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            if (notes is { Length: > 4000 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["notes"] = ["Özel çalışan notu 4.000 karakteri aşamaz."] });
            if (employee.PrivateNotes == notes) return Results.Ok(new { notes = employee.PrivateNotes, updated = false });
            employee.PrivateNotes = notes;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Employee", TargetId = id, Action = "PrivateNotesUpdated",
                Details = notes is null ? "İK özel notları temizlendi." : "İK özel notları güncellendi." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { notes = employee.PrivateNotes, updated = true });
        }).RequireAuthorization("people.files.manage");

        api.MapPost("/employees/{id:guid}/photo", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId, employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            var form = await request.ReadFormAsync(cancellationToken);
            var upload = form.Files.GetFile("file");
            if (upload is null || upload.Length is < 1 or > EmployeePhotoMaxSizeBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Profil fotoğrafı 1 bayt ile 2 MB arasında olmalıdır."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var detected = DetectLogo(bytes);
            if (detected.ContentType is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca doğrulanmış PNG, JPEG veya WebP fotoğraf yüklenebilir."] });

            var directory = EmployeePhotoDirectory(configuration, companyId, id);
            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(directory, $"upload-{Guid.NewGuid():N}.tmp");
            var destination = Path.Combine(directory, $"profile.{detected.Extension}");
            var previousPath = new[] { "png", "jpg", "webp" }
                .Select(extension => Path.Combine(directory, $"profile.{extension}"))
                .FirstOrDefault(path => System.IO.File.Exists(path));
            var previousBackupPath = previousPath is null ? null : Path.Combine(directory, $"replace-{Guid.NewGuid():N}.tmp");
            try
            {
                await System.IO.File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
                if (previousPath is not null) System.IO.File.Move(previousPath, previousBackupPath!);
                System.IO.File.Move(temporaryPath, destination);
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Employee", TargetId = id, Action = "ProfilePhotoUpdated",
                    Details = $"Profil fotoğrafı yüklendi ({detected.ContentType}, {bytes.LongLength} bayt)." });
                await db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                try { if (System.IO.File.Exists(temporaryPath)) System.IO.File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                try { if (System.IO.File.Exists(destination)) System.IO.File.Delete(destination); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                try { if (previousPath is not null && previousBackupPath is not null && System.IO.File.Exists(previousBackupPath)) System.IO.File.Move(previousBackupPath, previousPath, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                throw;
            }
            foreach (var extension in new[] { "png", "jpg", "webp" })
            {
                var oldPath = Path.Combine(directory, $"profile.{extension}");
                try { if (!string.Equals(oldPath, destination, StringComparison.Ordinal) && System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            try { if (previousBackupPath is not null && System.IO.File.Exists(previousBackupPath)) System.IO.File.Delete(previousBackupPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return Results.Ok(new { photoUrl = $"/api/employees/{id}/photo?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" });
        }).RequireAuthorization("people.manage")
            .WithMetadata(new RequestSizeLimitAttribute(EmployeePhotoMaxSizeBytes + 65_536));

        api.MapGet("/employees/{id:guid}/photo", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId, employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            var directory = EmployeePhotoDirectory(configuration, companyId, id);
            foreach (var candidate in new[] { ("png", "image/png"), ("jpg", "image/jpeg"), ("webp", "image/webp") })
            {
                var path = Path.Combine(directory, $"profile.{candidate.Item1}");
                if (!System.IO.File.Exists(path)) continue;
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                return Results.File(stream, candidate.Item2, enableRangeProcessing: false);
            }
            return Results.NotFound();
        }).RequireAuthorization("people.read");

        api.MapDelete("/employees/{id:guid}/photo", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId, employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            var directory = EmployeePhotoDirectory(configuration, companyId, id);
            var quarantined = new List<(string Original, string Temporary)>();
            try
            {
                foreach (var extension in new[] { "png", "jpg", "webp" })
                {
                    var path = Path.Combine(directory, $"profile.{extension}");
                    if (!System.IO.File.Exists(path)) continue;
                    var temporary = Path.Combine(directory, $"remove-{Guid.NewGuid():N}.tmp");
                    System.IO.File.Move(path, temporary);
                    quarantined.Add((path, temporary));
                }
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Employee", TargetId = id, Action = "ProfilePhotoRemoved", Details = "Profil fotoğrafı kaldırıldı." });
                await db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                foreach (var item in quarantined)
                    try { if (System.IO.File.Exists(item.Temporary)) System.IO.File.Move(item.Temporary, item.Original, true); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                throw;
            }
            foreach (var item in quarantined)
                try { if (System.IO.File.Exists(item.Temporary)) System.IO.File.Delete(item.Temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            return Results.NoContent();
        }).RequireAuthorization("people.manage");

        api.MapGet("/employees/{id:guid}/documents", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId, employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            var documents = await db.EmployeeDocuments.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.EmployeeId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.Length, x.CreatedAt,
                    uploadedBy = db.Users.Where(actor => actor.Id == x.UploadedByUserId).Select(actor => actor.DisplayName).FirstOrDefault() })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { documents, maxFiles = 30, maxFileSizeBytes = EmployeeDocumentMaxSizeBytes });
        }).RequireAuthorization("people.files.read");

        api.MapPost("/employees/{id:guid}/documents", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            var form = await request.ReadFormAsync(cancellationToken);
            var upload = form.Files.GetFile("file");
            if (upload is null || upload.Length is < 1 or > EmployeeDocumentMaxSizeBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Çalışan belgesi 1 bayt ile 10 MB arasında olmalıdır."] });
            await using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var detected = DetectEmployeeDocument(bytes);
            if (detected is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Yalnızca gerçek PDF, PNG, JPEG veya WebP dosyaları yüklenebilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var employee = await db.Employees.FromSqlInterpolated($"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId, employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            if (await db.EmployeeDocuments.CountAsync(x => x.CompanyId == companyId && x.EmployeeId == id, cancellationToken) >= 30)
                return Results.Conflict(new { message = "Bir çalışan profiline en fazla 30 belge eklenebilir." });

            var documentId = Guid.NewGuid();
            var storageFileName = $"{documentId:N}.{detected.Value.Extension}";
            var originalFileName = CleanEmployeeDocumentFileName(upload.FileName, detected.Value.Extension);
            var directory = EmployeeDocumentDirectory(configuration, companyId, id);
            Directory.CreateDirectory(directory);
            var destination = Path.Combine(directory, storageFileName);
            var document = new EmployeeDocument { Id = documentId, CompanyId = companyId, EmployeeId = id,
                UploadedByUserId = actorId, OriginalFileName = originalFileName, StorageFileName = storageFileName,
                ContentType = detected.Value.ContentType, Length = bytes.LongLength };
            try
            {
                await System.IO.File.WriteAllBytesAsync(destination, bytes, cancellationToken);
                db.EmployeeDocuments.Add(document);
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Employee", TargetId = id, Action = "DocumentAdded",
                    Details = $"Çalışan belgesi eklendi ({detected.Value.ContentType}, {bytes.LongLength} bayt)." });
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                try { if (System.IO.File.Exists(destination)) System.IO.File.Delete(destination); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                throw;
            }
            return Results.Created($"/api/employees/{id}/documents/{documentId}/file", new { document.Id,
                document.OriginalFileName, document.ContentType, document.Length, document.CreatedAt });
        }).RequireAuthorization("people.files.manage")
            .WithMetadata(new RequestSizeLimitAttribute(EmployeeDocumentRequestLimitBytes));

        api.MapGet("/employees/{id:guid}/documents/{documentId:guid}/file", async (Guid id, Guid documentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId,
                    employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            var document = await db.EmployeeDocuments.AsNoTracking().SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.EmployeeId == id && x.Id == documentId, cancellationToken);
            if (document is null || !IsSafeEmployeeDocumentFileName(document.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(EmployeeDocumentDirectory(configuration, companyId, id), document.StorageFileName);
            if (!System.IO.File.Exists(path)) return Results.NotFound();
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Results.File(stream, document.ContentType, document.OriginalFileName, enableRangeProcessing: false);
        }).RequireAuthorization("people.files.read");

        api.MapDelete("/employees/{id:guid}/documents/{documentId:guid}", async (Guid id, Guid documentId,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var document = await db.EmployeeDocuments.SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.EmployeeId == id && x.Id == documentId, cancellationToken);
            if (document is null) return Results.NotFound();
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, actorId,
                    employee.LocationCatalogId, employee.CompanyUnitId, cancellationToken))
                return Results.NotFound();
            db.EmployeeDocuments.Remove(document);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee",
                TargetId = id, Action = "DocumentRemoved", Details = $"Çalışan belgesi kaldırıldı ({document.ContentType}, {document.Length} bayt)." });
            await db.SaveChangesAsync(cancellationToken);
            var path = Path.Combine(EmployeeDocumentDirectory(configuration, companyId, id), document.StorageFileName);
            try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return Results.NoContent();
        }).RequireAuthorization("people.files.manage");

        api.MapPatch("/employees/{id:guid}/manager", async (Guid id, [FromBody] SetEmployeeManagerRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var companyLock = await db.Companies.FromSqlInterpolated(
                    $"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (companyLock is null) return Results.Forbid();
            var employee = await db.Employees.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (employee is null || !EmployeeIsInScope(employee.LocationCatalogId, employee.CompanyUnitId, employeeAccessScope)) return Results.NotFound();
            if (request.ManagerEmployeeId == id)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["managerEmployeeId"] = ["Bir çalışan kendisini yönetici olarak seçemez."] });

            string? managerName = null;
            if (request.ManagerEmployeeId is Guid managerId)
            {
                var manager = await EmployeesInScope(db, companyId, employeeAccessScope)
                    .Where(x => x.Id == managerId && x.IsActive)
                    .Select(x => new { x.Id, x.DisplayName }).SingleOrDefaultAsync(cancellationToken);
                if (manager is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["managerEmployeeId"] = ["Yönetici erişilebilir konumda bulunan aynı şirketin etkin bir çalışanı olmalıdır."] });
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
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var employee = await db.Employees.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (employee is null) return Results.NotFound();
            if (!EmployeeIsInScope(employee.LocationCatalogId, employee.CompanyUnitId, employeeAccessScope)) return Results.NotFound();
            if (employee.IsActive == request.IsActive)
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { employee.Id, employee.IsActive, returnedAssets = 0, returnedAccessories = 0, revokedLicenses = 0 });
            }
            if (!request.IsActive && await HasOutOfScopeEmployeeAssignmentsAsync(db, companyId, [id], allowedLocations, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["offboarding"] = ["Çalışanın konum kapsamı dışındaki zimmetlerini yalnızca şirket genelinde yetkili yönetici iade edebilir. Hiçbir kayıt değiştirilmedi."] });
            var offboarding = request.IsActive ? EmployeeOffboardingCounts.Empty :
                await ReturnEmployeeAssignmentsAsync(db, companyId, actorId, [id], cancellationToken);
            employee.IsActive = request.IsActive;
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee", TargetId = id,
                Action = request.IsActive ? "Reactivated" : "Deactivated",
                Details = request.IsActive ? "Çalışan kaydı yeniden etkinleştirildi." :
                    $"Çalışan kaydı pasife alındı; {offboarding.Assets} varlık iade edildi, {offboarding.Accessories} aksesuar ataması iade edildi, {offboarding.Licenses} lisans ataması kaldırıldı."
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { employee.Id, employee.IsActive, returnedAssets = offboarding.Assets,
                returnedAccessories = offboarding.Accessories, revokedLicenses = offboarding.Licenses });
        }).RequireAuthorization("people.manage");

        api.MapPost("/employees/bulk-offboarding-summary", async ([FromBody] BulkSetEmployeeActiveRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var ids = request.EmployeeIds;
            if (ids is null || ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeIds"] = ["1 ile 100 arasında, yinelenmeyen çalışan seçin."] });
            var employees = await EmployeesInScope(db, companyId, employeeAccessScope)
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.DisplayName, x.EmployeeNumber }).ToListAsync(cancellationToken);
            if (employees.Count != ids.Length) return Results.NotFound();
            var assets = await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId &&
                    x.AssignedEmployeeId != null && ids.Contains(x.AssignedEmployeeId.Value) &&
                    (allowedLocations == null || x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value)))
                .GroupBy(x => x.AssignedEmployeeId!.Value).Select(group => new { EmployeeId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, cancellationToken);
            var accessories = await (from assignment in db.AccessoryAssignments.AsNoTracking()
                join accessory in db.Accessories.AsNoTracking() on new { assignment.CompanyId, Id = assignment.AccessoryId } equals new { accessory.CompanyId, Id = accessory.Id }
                where assignment.CompanyId == companyId && assignment.EmployeeId != null && ids.Contains(assignment.EmployeeId.Value) &&
                    assignment.ReturnedQuantity < assignment.Quantity &&
                    (allowedLocations == null || accessory.LocationCatalogId.HasValue && allowedLocations.Contains(accessory.LocationCatalogId.Value))
                select assignment)
                .GroupBy(x => x.EmployeeId!.Value).Select(group => new { EmployeeId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, cancellationToken);
            var licenses = await db.SoftwareLicenseAssignments.AsNoTracking().Where(x => x.CompanyId == companyId &&
                    x.EmployeeId != null && ids.Contains(x.EmployeeId.Value) && x.RevokedAt == null &&
                    (allowedLocations == null || !x.AssetId.HasValue || db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == x.AssetId &&
                        asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value))))
                .GroupBy(x => x.EmployeeId!.Value).Select(group => new { EmployeeId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, cancellationToken);
            var reports = await EmployeesInScope(db, companyId, employeeAccessScope)
                .Where(x => x.IsActive && x.ManagerEmployeeId != null && ids.Contains(x.ManagerEmployeeId.Value))
                .GroupBy(x => x.ManagerEmployeeId!.Value).Select(group => new { EmployeeId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, cancellationToken);
            var items = employees.Select(employee => new
            {
                employee.Id, employee.DisplayName, employee.EmployeeNumber,
                AssetCount = assets.GetValueOrDefault(employee.Id),
                AccessoryCount = accessories.GetValueOrDefault(employee.Id),
                ActiveLicenseCount = licenses.GetValueOrDefault(employee.Id),
                ActiveDirectReportCount = reports.GetValueOrDefault(employee.Id)
            }).ToArray();
            var hasOutOfScopeAssignments = await HasOutOfScopeEmployeeAssignmentsAsync(db, companyId, ids, allowedLocations, cancellationToken);
            return Results.Ok(new
            {
                items,
                canCompleteOffboarding = !hasOutOfScopeAssignments,
                requiresCompanyAdmin = hasOutOfScopeAssignments,
                totals = new
                {
                    assetCount = items.Sum(item => item.AssetCount),
                    accessoryCount = items.Sum(item => item.AccessoryCount),
                    activeLicenseCount = items.Sum(item => item.ActiveLicenseCount),
                    activeDirectReportCount = items.Sum(item => item.ActiveDirectReportCount)
                }
            });
        }).RequireAuthorization("people.manage");

        api.MapPost("/employees/bulk-active", async ([FromBody] BulkSetEmployeeActiveRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var ids = request.EmployeeIds;
            if (ids is null || ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeIds"] = ["1 ile 100 arasında, yinelenmeyen çalışan seçin."] });
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var employees = await EmployeesInScope(db, companyId, employeeAccessScope, tracking: true)
                .Where(x => ids.Contains(x.Id))
                .OrderBy(x => x.Id).ToListAsync(cancellationToken);
            if (employees.Count != ids.Length) return Results.NotFound();
            var changed = employees.Where(employee => employee.IsActive != request.IsActive).ToArray();
            if (!request.IsActive && changed.Length > 0 && await HasOutOfScopeEmployeeAssignmentsAsync(db, companyId,
                    changed.Select(employee => employee.Id).ToArray(), allowedLocations, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["offboarding"] = ["Seçimdeki çalışanların konum kapsamı dışındaki zimmetlerini yalnızca şirket genelinde yetkili yönetici iade edebilir. Hiçbir kayıt değiştirilmedi."] });
            var offboarding = !request.IsActive && changed.Length > 0
                ? await ReturnEmployeeAssignmentsAsync(db, companyId, actorId, changed.Select(employee => employee.Id).ToArray(), cancellationToken)
                : EmployeeOffboardingCounts.Empty;
            foreach (var employee in changed)
            {
                employee.IsActive = request.IsActive;
                db.AuditEntries.Add(new AuditEntry
                {
                    CompanyId = companyId, ActorUserId = actorId, TargetType = "Employee", TargetId = employee.Id,
                    Action = request.IsActive ? "Reactivated" : "Deactivated",
                    Details = request.IsActive ? "Toplu işlemle çalışan kaydı yeniden etkinleştirildi." :
                        "Toplu pasife alma sırasında zimmetli varlıklar ve açık aksesuar atamaları iade edildi, lisans atamaları kaldırıldı."
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { requested = ids.Length, changed = changed.Length, isActive = request.IsActive,
                employeeIds = changed.Select(employee => employee.Id), returnedAssets = offboarding.Assets,
                returnedAccessories = offboarding.Accessories, revokedLicenses = offboarding.Licenses });
        }).RequireAuthorization("people.manage");

        api.MapGet("/employees/{id:guid}/offboarding-summary", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var employeeScope = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(cancellationToken);
            if (employeeScope is null || !EmployeeIsInScope(employeeScope.LocationCatalogId, employeeScope.CompanyUnitId, employeeAccessScope))
                return Results.NotFound();
            var assetCount = await db.Assets.AsNoTracking().CountAsync(x =>
                x.CompanyId == companyId && x.AssignedEmployeeId == id &&
                (allowedLocations == null || x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value)), cancellationToken);
            var accessoryCount = await db.AccessoryAssignments.AsNoTracking().CountAsync(x =>
                x.CompanyId == companyId && x.EmployeeId == id && x.ReturnedQuantity < x.Quantity &&
                (allowedLocations == null || db.Accessories.Any(accessory => accessory.CompanyId == companyId && accessory.Id == x.AccessoryId &&
                    accessory.LocationCatalogId.HasValue && allowedLocations.Contains(accessory.LocationCatalogId.Value))), cancellationToken);
            var activeLicenseCount = await db.SoftwareLicenseAssignments.AsNoTracking().CountAsync(x =>
                x.CompanyId == companyId && x.EmployeeId == id && x.RevokedAt == null &&
                (allowedLocations == null || !x.AssetId.HasValue || db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == x.AssetId &&
                    asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value))), cancellationToken);
            var activeDirectReportCount = await EmployeesInScope(db, companyId, employeeAccessScope)
                .CountAsync(x => x.ManagerEmployeeId == id && x.IsActive, cancellationToken);
            var hasOutOfScopeAssignments = await HasOutOfScopeEmployeeAssignmentsAsync(db, companyId, [id], allowedLocations, cancellationToken);
            return Results.Ok(new { assetCount, accessoryCount, activeLicenseCount,
                activeDirectReportCount,
                canCompleteOffboarding = !hasOutOfScopeAssignments,
                requiresCompanyAdmin = hasOutOfScopeAssignments,
                hasOutstandingItems = assetCount + accessoryCount + activeLicenseCount + activeDirectReportCount > 0 || hasOutOfScopeAssignments });
        }).RequireAuthorization("people.manage");

        // Employee values remain backward-compatible free text while this catalog lets a
        // company maintain departments before they have employees assigned.
        api.MapGet("/company-units", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] bool includeInactive = false) =>
        {
            if (!TryScope(user, out var companyId, out _)) return Results.Forbid();
            var currentUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var allowedCompanyUnits = await CompanyLocationAccess.GetEffectiveCompanyUnitIdsAsync(
                db, companyId, currentUserId, cancellationToken);
            var units = await db.CompanyUnits.AsNoTracking().Where(x => x.CompanyId == companyId &&
                    (includeInactive || x.IsActive) && (allowedCompanyUnits == null || allowedCompanyUnits.Contains(x.Id)))
                .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Code, x.UnitType, x.ParentCompanyUnitId, x.IsActive, x.CreatedAt, x.UpdatedAt })
                .ToListAsync(cancellationToken);
            var byId = units.ToDictionary(x => x.Id);
            string UnitPath(Guid id)
            {
                var item = byId[id];
                return item.ParentCompanyUnitId is Guid parentId && byId.ContainsKey(parentId)
                    ? $"{UnitPath(parentId)} / {item.Name}" : item.Name;
            }
            return Results.Ok(new { items = units.Select(x => new { x.Id, x.Name, x.Code, x.UnitType, x.ParentCompanyUnitId,
                parentName = x.ParentCompanyUnitId is Guid parentId && byId.TryGetValue(parentId, out var parent) ? parent.Name : null,
                path = UnitPath(x.Id), x.IsActive, x.CreatedAt, x.UpdatedAt }), total = units.Count });
        }).RequireAuthorization("people.read");

        api.MapPost("/company-units", async ([FromBody] CompanyUnitRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var name = request.Name?.Trim();
            var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();
            var unitType = string.IsNullOrWhiteSpace(request.UnitType) ? CompanyUnitTypes.Organizational : request.UnitType.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || name.Any(char.IsControl))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Birim adı 1–160 karakter olmalı ve kontrol karakteri içermemelidir."] });
            if (!CompanyUnitTypes.All.Contains(unitType, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["unitType"] = ["Birim türü organizasyon, tüzel kişilik veya şube olmalıdır."] });
            if (code is { Length: > 40 } || code?.Any(char.IsControl) == true)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Birim kodu en fazla 40 karakter olabilir ve kontrol karakteri içeremez."] });
            var normalized = NormalizeDepartmentName(name);
            if (await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.NormalizedName == normalized, cancellationToken))
                return Results.Conflict(new { message = "Bu şirket birimi adı zaten kayıtlı." });
            if (code is not null && await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken))
                return Results.Conflict(new { message = "Bu şirket birimi kodu zaten kayıtlı." });
            if (request.ParentCompanyUnitId is Guid parentId)
            {
                if (!await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.Id == parentId && x.IsActive, cancellationToken))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentCompanyUnitId"] = ["Üst birim aynı şirket içinde etkin olmalıdır."] });
                var cursor = parentId; var depth = 2; var visited = new HashSet<Guid>();
                while (true)
                {
                    if (!visited.Add(cursor)) return Results.Conflict(new { message = "Mevcut birim hiyerarşisinde döngü var; yeni birim eklenemedi." });
                    var ancestorId = await db.CompanyUnits.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == cursor)
                        .Select(x => x.ParentCompanyUnitId).SingleOrDefaultAsync(cancellationToken);
                    if (ancestorId is not Guid next) break;
                    cursor = next; depth++;
                }
                if (depth > 6) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentCompanyUnitId"] = ["Birim hiyerarşisi en fazla altı seviyeye kadar açılabilir."] });
            }
            var entry = new CompanyUnit { CompanyId = companyId, Name = name, NormalizedName = normalized,
                Code = code, UnitType = unitType, ParentCompanyUnitId = request.ParentCompanyUnitId };
            db.CompanyUnits.Add(entry);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "CompanyUnit", TargetId = entry.Id, Action = "Created", Details = $"Şirket birimi oluşturuldu: {name}; tür {unitType}." });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure })
            { return Results.Conflict(new { message = "Bu şirket birimi eşzamanlı olarak başka bir işlemde kaydedildi." }); }
            return Results.Created("/api/company-units", new { entry.Id, entry.Name, entry.Code, entry.UnitType, entry.ParentCompanyUnitId, entry.IsActive });
        }).RequireAuthorization("settings.manage");

        api.MapPut("/company-units/{id:guid}", async (Guid id, [FromBody] CompanyUnitRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var name = request.Name?.Trim();
            var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();
            var unitType = string.IsNullOrWhiteSpace(request.UnitType) ? CompanyUnitTypes.Organizational : request.UnitType.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || name.Any(char.IsControl))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Birim adı 1–160 karakter olmalı ve kontrol karakteri içermemelidir."] });
            if (!CompanyUnitTypes.All.Contains(unitType, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["unitType"] = ["Birim türü organizasyon, tüzel kişilik veya şube olmalıdır."] });
            if (code is { Length: > 40 } || code?.Any(char.IsControl) == true)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Birim kodu en fazla 40 karakter olabilir ve kontrol karakteri içeremez."] });
            var entry = await db.CompanyUnits.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (entry is null) return Results.NotFound();
            if (request.ParentCompanyUnitId == id) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentCompanyUnitId"] = ["Birim kendisinin üst birimi olamaz."] });
            if (request.ParentCompanyUnitId is Guid parentId)
            {
                var parent = await db.CompanyUnits.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == parentId && x.IsActive, cancellationToken);
                if (parent is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentCompanyUnitId"] = ["Üst birim aynı şirket içinde etkin olmalıdır."] });
            }
            var parentByUnit = await db.CompanyUnits.AsNoTracking().Where(x => x.CompanyId == companyId)
                .ToDictionaryAsync(x => x.Id, x => x.ParentCompanyUnitId, cancellationToken);
            parentByUnit[id] = request.ParentCompanyUnitId;
            foreach (var unitId in parentByUnit.Keys)
            {
                var visited = new HashSet<Guid>(); var cursor = unitId; var depth = 0;
                while (true)
                {
                    if (!visited.Add(cursor)) return Results.Conflict(new { message = "Mevcut birim hiyerarşisinde döngü var; kayıt değiştirilmedi." });
                    if (!parentByUnit.TryGetValue(cursor, out var parentUnitId) || parentUnitId is not Guid next) break;
                    cursor = next; depth++;
                    if (depth >= 6) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentCompanyUnitId"] = ["Düzenleme, birim ağacını altı seviyeden derin yapamaz."] });
                }
            }
            var normalized = NormalizeDepartmentName(name);
            if (await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.NormalizedName == normalized, cancellationToken))
                return Results.Conflict(new { message = "Bu adla başka bir şirket birimi var." });
            if (code is not null && await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.Code == code, cancellationToken))
                return Results.Conflict(new { message = "Bu kodla başka bir şirket birimi var." });
            var previousName = entry.Name;
            var previousUnitType = entry.UnitType;
            entry.Name = name; entry.NormalizedName = normalized; entry.Code = code;
            entry.UnitType = unitType; entry.ParentCompanyUnitId = request.ParentCompanyUnitId; entry.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "CompanyUnit", TargetId = entry.Id, Action = "Updated", Details = $"Şirket birimi güncellendi: {previousName} → {name}; tür {previousUnitType} → {unitType}." });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure })
            { return Results.Conflict(new { message = "Birim başka bir işlemle aynı anda güncellendi; yenileyip tekrar deneyin." }); }
            return Results.Ok(new { entry.Id, entry.Name, entry.Code, entry.UnitType, entry.ParentCompanyUnitId, entry.IsActive, entry.UpdatedAt });
        }).RequireAuthorization("settings.manage");

        api.MapPatch("/company-units/{id:guid}/active", async (Guid id, [FromBody] CompanyUnitActiveRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var entry = await db.CompanyUnits.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (entry is null) return Results.NotFound();
            if (entry.IsActive == request.IsActive) return Results.Ok(new { entry.Id, entry.IsActive });
            if (request.IsActive && entry.ParentCompanyUnitId is Guid parentId &&
                !await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.Id == parentId && x.IsActive, cancellationToken))
                return Results.Conflict(new { message = "Önce bu birimin arşivlenmiş üst birimini geri yükleyin." });
            if (!request.IsActive && await db.CompanyUnits.AnyAsync(x => x.CompanyId == companyId && x.ParentCompanyUnitId == id && x.IsActive, cancellationToken))
                return Results.Conflict(new { message = "Etkin alt birimler arşivlenmeden bu birim arşivlenemez." });
            entry.IsActive = request.IsActive; entry.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "CompanyUnit", TargetId = entry.Id, Action = request.IsActive ? "Restored" : "Archived",
                Details = $"Şirket birimi {(request.IsActive ? "etkinleştirildi" : "arşivlendi")}: {entry.Name}." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { entry.Id, entry.IsActive, entry.UpdatedAt });
        }).RequireAuthorization("settings.manage");

        api.MapGet("/departments", async ([FromQuery] Guid? locationId, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            if (allowedLocations is not null && locationId is Guid requestedLocation && !allowedLocations.Contains(requestedLocation))
                return Results.NotFound();
            if (locationId is Guid selectedLocation && !await db.CompanyLocations.AnyAsync(x =>
                x.CompanyId == companyId && x.Id == selectedLocation && x.IsActive, cancellationToken)) return Results.NotFound();
            var departmentsQuery = db.Employees.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Department != null && x.Department.Trim() != "");
            if (allowedLocations is not null)
                departmentsQuery = departmentsQuery.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
            var departments = await departmentsQuery
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
            var catalogQuery = db.CompanyDepartments.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations is not null)
                catalogQuery = catalogQuery.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
            var allCatalog = await catalogQuery
                .OrderBy(x => x.Name).ToListAsync(cancellationToken);
            var catalog = locationId is Guid departmentLocationId
                ? allCatalog.Where(x => x.LocationCatalogId == departmentLocationId).ToList()
                : allCatalog;
            var catalogNames = catalog.Select(x => x.NormalizedName).ToHashSet(StringComparer.Ordinal);
            var items = new Dictionary<string, (string name, int employeeCount, int activeEmployeeCount,
                int inactiveEmployeeCount, bool isCatalogEntry, bool isActive)>(StringComparer.Ordinal);
            foreach (var department in departments)
            {
                var key = NormalizeDepartmentName(department.name!);
                if (locationId is not null && !catalogNames.Contains(key)) continue;
                if (items.TryGetValue(key, out var current))
                    items[key] = (current.name, current.employeeCount + department.employeeCount,
                        current.activeEmployeeCount + department.activeEmployeeCount,
                        current.inactiveEmployeeCount + department.inactiveEmployeeCount,
                        catalogNames.Contains(key), true);
                else items[key] = (department.name!, department.employeeCount, department.activeEmployeeCount,
                    department.inactiveEmployeeCount, catalogNames.Contains(key), true);
            }
            foreach (var entry in catalog)
            {
                var key = entry.NormalizedName;
                if (items.TryGetValue(key, out var current))
                    items[key] = (current.name, current.employeeCount, current.activeEmployeeCount,
                        current.inactiveEmployeeCount, true, entry.IsActive);
                else items[key] = (entry.Name, 0, 0, 0, true, entry.IsActive);
            }
            var parents = allCatalog.ToDictionary(x => x.Id);
            var managerIds = catalog.Where(x => x.ManagerEmployeeId is not null).Select(x => x.ManagerEmployeeId!.Value).Distinct().ToArray();
            var managers = managerIds.Length == 0
                ? new Dictionary<Guid, (string displayName, bool isActive)>()
                : await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && managerIds.Contains(x.Id) &&
                        (allowedLocations == null || x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value)))
                    .ToDictionaryAsync(x => x.Id, x => (x.DisplayName, x.IsActive), cancellationToken);
            var locationsQuery = db.CompanyLocations.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations is not null) locationsQuery = locationsQuery.Where(x => allowedLocations.Contains(x.Id));
            var locations = await locationsQuery
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
            string DepartmentPath(CompanyDepartment entry)
            {
                if (entry.ParentDepartmentId is not Guid parentId || !parents.TryGetValue(parentId, out var parent)) return entry.Name;
                return $"{DepartmentPath(parent)} / {entry.Name}";
            }
            var catalogItems = catalog.ToDictionary(x => x.NormalizedName, StringComparer.Ordinal);
            var result = items.Values.OrderBy(x => x.name, StringComparer.CurrentCultureIgnoreCase)
                .Select(x => new { name = x.name, employeeCount = x.employeeCount,
                    activeEmployeeCount = x.activeEmployeeCount, inactiveEmployeeCount = x.inactiveEmployeeCount,
                    isCatalogEntry = x.isCatalogEntry, isActive = x.isActive,
                    departmentId = catalogItems.TryGetValue(NormalizeDepartmentName(x.name), out var departmentEntry) ? departmentEntry.Id : (Guid?)null,
                    parentDepartmentId = catalogItems.TryGetValue(NormalizeDepartmentName(x.name), out var entry) ? entry.ParentDepartmentId : (Guid?)null,
                    parentDepartmentName = entry?.ParentDepartmentId is Guid parentId && parents.TryGetValue(parentId, out var parent) ? DepartmentPath(parent) : null,
                    departmentPath = entry is null ? x.name : DepartmentPath(entry),
                    locationCatalogId = entry?.LocationCatalogId,
                    locationName = entry?.LocationCatalogId is Guid selectedId && locations.TryGetValue(selectedId, out var locationName) ? locationName : null,
                    managerEmployeeId = entry?.ManagerEmployeeId,
                    managerName = entry?.ManagerEmployeeId is Guid managerId && managers.TryGetValue(managerId, out var manager) ? manager.Item1 : null,
                    managerIsActive = entry?.ManagerEmployeeId is Guid activeManagerId && managers.TryGetValue(activeManagerId, out var managerState) ? managerState.Item2 : (bool?)null
                }).ToArray();
            return Results.Ok(new { items = result, total = result.Length });
        }).RequireAuthorization("people.read");

        api.MapPatch("/departments/{id:guid}/manager", async (Guid id, [FromBody] SetDepartmentManagerRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();
            var department = await db.CompanyDepartments.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (department is null) return Results.NotFound();
            if (!AssetLocationIsInScope(department.LocationCatalogId, allowedLocations)) return Results.NotFound();
            if (!department.IsActive) return Results.Conflict(new { message = "Arşivlenmiş departmana yetkili atanamaz." });
            Employee? manager = null;
            if (request.EmployeeId is Guid employeeId)
            {
                manager = await db.Employees.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == employeeId && x.IsActive &&
                    (allowedLocations == null || x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value)), cancellationToken);
                if (manager is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Yetkili, bu şirkette kayıtlı etkin bir çalışan olmalıdır."] });
            }
            if (department.ManagerEmployeeId == request.EmployeeId)
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { department.Id, department.ManagerEmployeeId, managerName = manager?.DisplayName });
            }
            var previousManager = department.ManagerEmployeeId is Guid previousId
                ? await db.Employees.Where(x => x.CompanyId == companyId && x.Id == previousId).Select(x => x.DisplayName).SingleOrDefaultAsync(cancellationToken)
                : null;
            department.ManagerEmployeeId = request.EmployeeId; department.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Department", TargetId = id, Action = "ManagerChanged",
                Details = $"Departman yetkilisi: {previousManager ?? "atanmamış"} → {manager?.DisplayName ?? "atanmamış"}." });
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { department.Id, department.ManagerEmployeeId, managerName = manager?.DisplayName });
        }).RequireAuthorization("people.manage");

        api.MapPatch("/departments/{id:guid}/active", async (Guid id, [FromBody] CompanyUnitActiveRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var entry = await db.CompanyDepartments.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (entry is null) return Results.NotFound();
            if (!AssetLocationIsInScope(entry.LocationCatalogId, allowedLocations)) return Results.NotFound();
            if (request.IsActive && entry.ParentDepartmentId is Guid parentId &&
                !await db.CompanyDepartments.AnyAsync(x => x.CompanyId == companyId && x.Id == parentId && x.IsActive, cancellationToken))
                return Results.Conflict(new { message = "Önce üst departmanı geri yükleyin." });
            if (!request.IsActive && await db.CompanyDepartments.AnyAsync(x => x.CompanyId == companyId && x.ParentDepartmentId == id && x.IsActive, cancellationToken))
                return Results.Conflict(new { message = "Önce etkin alt departmanları arşivleyin." });
            if (entry.IsActive == request.IsActive) return Results.Ok(new { entry.Id, entry.IsActive, entry.UpdatedAt });
            entry.IsActive = request.IsActive; entry.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Department", TargetId = id, Action = request.IsActive ? "Restored" : "Archived",
                Details = $"Departman {(request.IsActive ? "geri yüklendi" : "arşivlendi")}: {entry.Name}. Çalışan kayıtları korundu." });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { entry.Id, entry.IsActive, entry.UpdatedAt });
        }).RequireAuthorization("people.manage");

        api.MapPost("/departments/{id:guid}/move-employees", async (Guid id, [FromBody] MoveDepartmentEmployeesRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var source = await db.CompanyDepartments.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (source is null) return Results.NotFound();
            var target = await db.CompanyDepartments.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == request.TargetDepartmentId && x.IsActive, cancellationToken);
            if (target is null || target.Id == source.Id)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["targetDepartmentId"] = ["Aynı şirkette etkin ve kaynak departmandan farklı bir hedef seçin."] });
            if (!AssetLocationIsInScope(source.LocationCatalogId, allowedLocations) ||
                !AssetLocationIsInScope(target.LocationCatalogId, allowedLocations)) return Results.NotFound();
            var batchSize = request.BatchSize is >= 1 and <= 100 ? request.BatchSize : 100;
            var sourcePattern = source.Name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var employeeIds = allowedLocations is null
                ? await db.Employees.FromSqlInterpolated($"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Department\" IS NOT NULL AND lower(btrim(\"Department\")) = lower({source.Name}) ORDER BY \"Id\" LIMIT {batchSize} FOR UPDATE")
                    .Select(x => x.Id).ToListAsync(cancellationToken)
                : await db.Employees.FromSqlInterpolated($"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Department\" IS NOT NULL AND lower(btrim(\"Department\")) = lower({source.Name}) AND \"LocationCatalogId\" = ANY({allowedLocations.ToArray()}) ORDER BY \"Id\" LIMIT {batchSize} FOR UPDATE")
                    .Select(x => x.Id).ToListAsync(cancellationToken);
            if (employeeIds.Count > 0)
                await db.Employees.Where(x => x.CompanyId == companyId && employeeIds.Contains(x.Id))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Department, target.Name), cancellationToken);
            var remainingQuery = db.Employees.Where(x => x.CompanyId == companyId && x.Department != null &&
                EF.Functions.ILike(x.Department.Trim(), sourcePattern));
            if (allowedLocations is not null)
                remainingQuery = remainingQuery.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
            var remaining = await remainingQuery.CountAsync(cancellationToken);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Department", TargetId = id, Action = "EmployeesMoved",
                Details = $"Departman çalışanları parti hâlinde taşındı: {source.Name} → {target.Name}; taşınan: {employeeIds.Count}; kalan: {remaining}." });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { sourceDepartmentId = id, sourceName = source.Name, targetDepartmentId = target.Id,
                targetName = target.Name, moved = employeeIds.Count, remaining, complete = remaining == 0 });
        }).RequireAuthorization("people.manage");

        api.MapPost("/departments", async ([FromBody] CreateDepartmentRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            if (!AssetLocationIsInScope(request.LocationCatalogId, allowedLocations)) return Results.NotFound();
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || name.Any(char.IsControl))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Departman adı 1–160 karakter olmalı ve kontrol karakteri içermemelidir."] });
            var normalized = NormalizeDepartmentName(name);
            var existsInCatalog = await db.CompanyDepartments.AnyAsync(x => x.CompanyId == companyId && x.NormalizedName == normalized, cancellationToken);
            var existsOnEmployee = await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.Department != null &&
                EF.Functions.ILike(x.Department.Trim(), name.Replace("\\", "\\\\", StringComparison.Ordinal)
                    .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)), cancellationToken);
            if (existsInCatalog || existsOnEmployee)
                return Results.Conflict(new { message = "Bu departman adı bu şirkette zaten kayıtlı." });
            if (request.ParentDepartmentId is Guid parentId)
            {
                var parent = await db.CompanyDepartments.AsNoTracking().SingleOrDefaultAsync(x =>
                    x.CompanyId == companyId && x.Id == parentId && x.IsActive, cancellationToken);
                if (parent is null || !AssetLocationIsInScope(parent.LocationCatalogId, allowedLocations))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentDepartmentId"] = ["Üst departman aynı şirket içinde etkin ve erişilebilir olmalıdır."] });
            }
            if (request.LocationCatalogId is Guid departmentLocation && !await db.CompanyLocations.AnyAsync(x =>
                x.CompanyId == companyId && x.Id == departmentLocation && x.IsActive, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["locationCatalogId"] = ["Departman konumu aynı şirkette etkin bir konum olmalıdır."] });
            if (request.ParentDepartmentId is Guid parentDepartmentId)
            {
                var seen = new HashSet<Guid>(); var cursor = parentDepartmentId; var depth = 1;
                while (seen.Add(cursor))
                {
                    var ancestor = await db.CompanyDepartments.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == cursor)
                        .Select(x => new { x.ParentDepartmentId }).SingleOrDefaultAsync(cancellationToken);
                    if (ancestor is null || ancestor.ParentDepartmentId is not Guid next) break;
                    cursor = next; depth++;
                }
                if (depth >= 6) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentDepartmentId"] = ["Departman ağacı en fazla altı seviyeye kadar açılabilir."] });
            }
            var entry = new CompanyDepartment { CompanyId = companyId, Name = name, NormalizedName = normalized,
                ParentDepartmentId = request.ParentDepartmentId, LocationCatalogId = request.LocationCatalogId };
            db.CompanyDepartments.Add(entry);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Department", TargetId = entry.Id, Action = "Created", Details = $"Departman kataloğa eklendi: {name}." });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure })
            {
                return Results.Conflict(new { message = "Bu departman adı eşzamanlı olarak başka bir işlemde kaydedildi." });
            }
            return Results.Created("/api/departments", new { entry.Id, entry.Name, entry.ParentDepartmentId, entry.LocationCatalogId, entry.IsActive, employeeCount = 0 });
        }).RequireAuthorization("people.manage");

        api.MapPut("/departments/{id:guid}", async (Guid id, [FromBody] UpdateDepartmentRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || name.Any(char.IsControl))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Departman adı 1–160 karakter olmalı ve kontrol karakteri içermemelidir."] });
            var entry = await db.CompanyDepartments.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
            if (entry is null) return Results.NotFound();
            if (!AssetLocationIsInScope(entry.LocationCatalogId, allowedLocations) ||
                !AssetLocationIsInScope(request.LocationCatalogId, allowedLocations)) return Results.NotFound();
            var normalized = NormalizeDepartmentName(name);
            var newNamePattern = name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
            var currentNamePattern = entry.Name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
            if (await db.CompanyDepartments.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.NormalizedName == normalized, cancellationToken) ||
                await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.Department != null &&
                    EF.Functions.ILike(x.Department.Trim(), newNamePattern) &&
                    !EF.Functions.ILike(x.Department.Trim(), currentNamePattern), cancellationToken))
                return Results.Conflict(new { message = "Bu adla başka bir departman var." });
            if (request.LocationCatalogId is Guid locationId && !await db.CompanyLocations.AnyAsync(x =>
                x.CompanyId == companyId && x.Id == locationId && x.IsActive, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["locationCatalogId"] = ["Departman konumu aynı şirkette etkin bir konum olmalıdır."] });

            var tree = await db.CompanyDepartments.AsNoTracking().Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);
            var byId = tree.ToDictionary(x => x.Id);
            var children = tree.Where(x => x.ParentDepartmentId is not null)
                .GroupBy(x => x.ParentDepartmentId!.Value).ToDictionary(group => group.Key, group => group.Select(x => x.Id).ToArray());
            var subtree = new HashSet<Guid>();
            var pending = new Stack<Guid>(); pending.Push(id);
            while (pending.TryPop(out var current))
            {
                if (!subtree.Add(current)) continue;
                if (children.TryGetValue(current, out var descendants)) foreach (var descendant in descendants) pending.Push(descendant);
            }
            if (allowedLocations is not null && subtree.Any(departmentId => !byId.TryGetValue(departmentId, out var department) ||
                    !AssetLocationIsInScope(department.LocationCatalogId, allowedLocations))) return Results.NotFound();
            int Depth(Guid departmentId)
            {
                var seen = new HashSet<Guid>(); var depth = 0; var cursor = departmentId;
                while (byId.TryGetValue(cursor, out var current))
                {
                    if (!seen.Add(cursor)) return 7;
                    depth++;
                    if (current.ParentDepartmentId is not Guid parentId) break;
                    cursor = parentId;
                }
                return depth;
            }
            var subtreeHeight = subtree.Max(Depth) - Depth(id) + 1;
            if (request.ParentDepartmentId is Guid parentId)
            {
                if (!byId.TryGetValue(parentId, out var parent) || !parent.IsActive ||
                    !AssetLocationIsInScope(parent.LocationCatalogId, allowedLocations))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentDepartmentId"] = ["Üst departman aynı şirkette etkin olmalıdır."] });
                if (subtree.Contains(parentId))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentDepartmentId"] = ["Departman kendisinin veya alt ekibinin üstüne taşınamaz."] });
                if (Depth(parentId) + 1 + subtreeHeight - 1 > 6)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentDepartmentId"] = ["Departman ağacı en fazla altı seviyeye kadar açılabilir."] });
            }
            else if (subtreeHeight > 6)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentDepartmentId"] = ["Departman ağacı en fazla altı seviyeye kadar açılabilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var previousName = entry.Name;
            entry.Name = name; entry.NormalizedName = normalized;
            entry.ParentDepartmentId = request.ParentDepartmentId; entry.LocationCatalogId = request.LocationCatalogId;
            entry.UpdatedAt = DateTimeOffset.UtcNow;
            var employeeCount = 0;
            if (!string.Equals(previousName, name, StringComparison.Ordinal))
            {
                var employeesToRename = db.Employees.Where(x => x.CompanyId == companyId && x.Department != null &&
                    EF.Functions.ILike(x.Department.Trim(), currentNamePattern));
                if (allowedLocations is not null)
                    employeesToRename = employeesToRename.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
                employeeCount = await employeesToRename.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Department, name), cancellationToken);
            }
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Department", TargetId = id, Action = "Updated",
                Details = $"Departman güncellendi: {previousName} → {name}; üst ekip ve konum doğrulandı." });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure })
            { return Results.Conflict(new { message = "Departman başka bir işlemle aynı anda güncellendi; yenileyip tekrar deneyin." }); }
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { entry.Id, entry.Name, entry.ParentDepartmentId, entry.LocationCatalogId, employeeCount });
        }).RequireAuthorization("people.manage");

        api.MapPatch("/departments/rename", async ([FromBody] RenameDirectoryEntryRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            if (allowedLocations is not null) return Results.Forbid();
            var currentName = request.CurrentName?.Trim();
            var newName = request.NewName?.Trim();
            if (string.IsNullOrWhiteSpace(currentName) || currentName.Length > 160 || currentName.Any(char.IsControl) ||
                string.IsNullOrWhiteSpace(newName) || newName.Length > 160 || newName.Any(char.IsControl))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Mevcut ve yeni departman adları 1–160 karakter olmalı ve kontrol karakteri içermemelidir."] });

            var currentPattern = currentName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
            var newPattern = newName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
            var currentNormalized = NormalizeDepartmentName(currentName);
            var newNormalized = NormalizeDepartmentName(newName);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken) is null) return Results.Forbid();
            var catalogEntry = await db.CompanyDepartments.SingleOrDefaultAsync(x => x.CompanyId == companyId &&
                x.NormalizedName == currentNormalized, cancellationToken);
            var employees = db.Employees.Where(employee => employee.CompanyId == companyId && employee.Department != null &&
                EF.Functions.ILike(employee.Department.Trim(), currentPattern));
            var count = await employees.CountAsync(cancellationToken);
            if (count == 0 && catalogEntry is null) return Results.NotFound();
            var collision = await db.Employees.AnyAsync(employee => employee.CompanyId == companyId && employee.Department != null &&
                EF.Functions.ILike(employee.Department.Trim(), newPattern) &&
                !EF.Functions.ILike(employee.Department.Trim(), currentPattern), cancellationToken) ||
                await db.CompanyDepartments.AnyAsync(entry => entry.CompanyId == companyId && entry.NormalizedName == newNormalized &&
                    entry.Id != (catalogEntry == null ? Guid.Empty : catalogEntry.Id), cancellationToken);
            if (collision && !request.Merge) return Results.Conflict(new { message = "Bu adla başka bir departman var. Birleştirme onayı gereklidir." });

            var updated = await employees.ExecuteUpdateAsync(setters => setters
                .SetProperty(employee => employee.Department, newName), cancellationToken);
            var targetEntry = collision ? await db.CompanyDepartments.SingleOrDefaultAsync(x => x.CompanyId == companyId &&
                x.NormalizedName == newNormalized, cancellationToken) : null;
            if (catalogEntry is not null && targetEntry is not null && catalogEntry.Id != targetEntry.Id)
                db.CompanyDepartments.Remove(catalogEntry);
            else if (catalogEntry is not null)
            {
                catalogEntry.Name = newName; catalogEntry.NormalizedName = newNormalized; catalogEntry.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (targetEntry is null)
                db.CompanyDepartments.Add(new CompanyDepartment { CompanyId = companyId, Name = newName, NormalizedName = newNormalized });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "Department", TargetId = companyId, Action = "Renamed",
                Details = collision
                    ? $"Departman “{currentName}” mevcut “{newName}” grubuyla birleştirildi; {updated} çalışan taşındı."
                    : $"Departman adı “{currentName}” → “{newName}”; {updated} çalışan güncellendi." });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { name = newName, employeeCount = updated });
        }).RequireAuthorization("people.manage");

        api.MapPost("/assets/{id:guid}/assign", async (Guid id, [FromBody] AssignAssetRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Note is { Length: > 2000 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 2000 karakter olabilir."] });
            if (request.EmployeeId == Guid.Empty)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Etkin çalışan bu şirkette bulunamadı."] });
            if (!ValidExpectedReturnDate(request.ExpectedReturnOn))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["expectedReturnOn"] = ["Beklenen iade tarihi bugün ile 10 yıl sonrası arasında olmalıdır."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var employee = await db.Employees.FromSqlInterpolated($"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.EmployeeId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !employee.IsActive)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Etkin çalışan bu şirkette bulunamadı."] });
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            if (!EmployeeIsInScope(employee.LocationCatalogId, employee.CompanyUnitId, employeeAccessScope)) return Results.NotFound();
            var asset = await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} AND \"DeletedAt\" IS NULL FOR UPDATE").IgnoreQueryFilters()
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            if (!AssetLocationIsInScope(asset.LocationCatalogId, allowedLocations)) return Results.NotFound();
            if (asset.AssignedEmployeeId is not null || asset.Status != AssetStatuses.Available)
                return Results.Conflict(new { message = "Yalnızca boşta ve kullanılabilir varlık zimmetlenebilir." });
            var reservationConflict = await CheckReservationForAssignmentAsync(db, companyId, asset.Id, request.EmployeeId,
                actorId, request.ExpectedReturnOn, cancellationToken);
            if (reservationConflict is not null) return Results.Conflict(new { message = reservationConflict });
            asset.AssignedEmployeeId = request.EmployeeId;
            asset.ExpectedReturnOn = request.ExpectedReturnOn;
            asset.Status = AssetStatuses.Deployed;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            db.AssetEvents.Add(new AssetEvent
            {
                CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                EmployeeId = request.EmployeeId, EventType = "Assigned", Details = AssignmentDetails(Clean(request.Note), request.ExpectedReturnOn)
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { asset.Id, asset.AssetTag, asset.Status, asset.AssignedEmployeeId, asset.ExpectedReturnOn, asset.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/return", async (Guid id, [FromBody] ReturnAssetRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Note is { Length: > 2000 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 2000 karakter olabilir."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var asset = await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} AND \"DeletedAt\" IS NULL FOR UPDATE").IgnoreQueryFilters()
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            if (!AssetLocationIsInScope(asset.LocationCatalogId, allowedLocations) ||
                asset.AssignedEmployeeId is Guid assignedEmployeeId &&
                !await EmployeeLocationIsInScopeAsync(db, companyId, assignedEmployeeId, employeeAccessScope, cancellationToken)) return Results.NotFound();
            if (asset.AssignedEmployeeId is not Guid employeeId)
                return Results.Conflict(new { message = "Bu varlık şu anda bir çalışana zimmetli değil." });
            asset.AssignedEmployeeId = null;
            var previousExpectedReturnOn = asset.ExpectedReturnOn;
            asset.ExpectedReturnOn = null;
            asset.Status = AssetStatuses.Available;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            db.AssetEvents.Add(new AssetEvent
            {
                CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                EmployeeId = employeeId, EventType = "Returned", Details = ReturnDetails(Clean(request.Note), previousExpectedReturnOn)
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { asset.Id, asset.AssetTag, asset.Status, asset.AssignedEmployeeId, asset.ExpectedReturnOn, asset.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapGet("/assets/trash", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
            var query = db.Assets.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.DeletedAt != null);
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (allowedLocations is not null)
                query = query.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(x => x.DeletedAt).ThenBy(x => x.AssetTag)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.AssetTag, x.Name, x.Status, x.SerialNumber, x.DeletedAt })
                .ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/trash", async (Guid id, [FromBody] AssetTrashRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Note is { Length: > 1500 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Not en fazla 1.500 karakter olabilir."] });
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var asset = await db.Assets.FromSqlInterpolated(
                $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} AND \"DeletedAt\" IS NULL FOR UPDATE").IgnoreQueryFilters()
                .SingleOrDefaultAsync(ct);
            if (asset is null) return Results.NotFound();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (!AssetLocationIsInScope(asset.LocationCatalogId, allowedLocations)) return Results.NotFound();
            if (asset.AssignedEmployeeId is not null || asset.Status == AssetStatuses.Deployed)
                return Results.Conflict(new { message = "Önce varlığı çalışandan iade alın; zimmetli kayıtlar çöp kutusuna taşınamaz." });
            if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.ParentAssetId == id, ct))
                return Results.Conflict(new { message = "Alt varlıkları bulunan kayıt taşınamaz. Önce alt varlık bağlantılarını kaldırın veya taşıyın." });
            if (await db.AssetMaintenances.AnyAsync(x => x.CompanyId == companyId && x.AssetId == id && (x.Status == "InProgress" || x.Status == "Scheduled"), ct))
                return Results.Conflict(new { message = "Açık veya planlanmış bakım kaydı bulunan varlık taşınamaz." });
            if (await db.AccessoryAssignments.AnyAsync(x => x.CompanyId == companyId && x.AssetId == id && x.Quantity > x.ReturnedQuantity, ct) ||
                await db.ComponentAllocations.AnyAsync(x => x.CompanyId == companyId && x.AssetId == id && x.Quantity > x.RemovedQuantity, ct) ||
                await db.SoftwareLicenseAssignments.AnyAsync(x => x.CompanyId == companyId && x.AssetId == id && x.RevokedAt == null, ct) ||
                await db.InventoryRequests.AnyAsync(x => x.CompanyId == companyId && x.ItemType == "Asset" && x.ItemId == id && x.Status == "Pending", ct))
                return Results.Conflict(new { message = "Açık talep, aksesuar, bileşen veya lisans ilişkileri çözülmeden varlık taşınamaz." });
            var now = DateTimeOffset.UtcNow; asset.DeletedAt = now; asset.DeletedByUserId = actorId; asset.UpdatedAt = now;
            var details = string.IsNullOrWhiteSpace(request.Note) ? "Varlık çöp kutusuna taşındı." : $"Varlık çöp kutusuna taşındı. {request.Note.Trim()}";
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id, ActorUserId = actorId, EventType = "Trashed", Details = details });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset", TargetId = id, Action = "Trashed", Details = $"{asset.AssetTag}: {details}" });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(new { asset.Id, asset.AssetTag, asset.DeletedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/restore-from-trash", async (Guid id, [FromBody] AssetTrashRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Note is { Length: > 1500 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Not en fazla 1.500 karakter olabilir."] });
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var asset = await db.Assets.FromSqlInterpolated(
                $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} AND \"DeletedAt\" IS NOT NULL FOR UPDATE").IgnoreQueryFilters()
                .SingleOrDefaultAsync(ct);
            if (asset is null) return Results.NotFound();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (!AssetLocationIsInScope(asset.LocationCatalogId, allowedLocations)) return Results.NotFound();
            if (asset.Status == AssetStatuses.ReturnedToSupplier)
                return Results.Conflict(new { message = "Tedarikçiye iade edilmiş varlık envantere geri alınamaz." });
            if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.AssetTag == asset.AssetTag, ct))
                return Results.Conflict(new { message = "Bu etiket etkin bir varlık tarafından kullanılıyor. Önce etiketi değiştirin; çöp kutusundaki kayıt korunuyor." });
            asset.DeletedAt = null; asset.DeletedByUserId = null; asset.UpdatedAt = DateTimeOffset.UtcNow;
            var details = string.IsNullOrWhiteSpace(request.Note) ? "Varlık çöp kutusundan geri alındı." : $"Varlık çöp kutusundan geri alındı. {request.Note.Trim()}";
            db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id, ActorUserId = actorId, EventType = "RestoredFromTrash", Details = details });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset", TargetId = id, Action = "RestoredFromTrash", Details = $"{asset.AssetTag}: {details}" });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(new { asset.Id, asset.AssetTag, asset.Status, asset.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/{id:guid}/restore", async (Guid id, [FromBody] ChangeAssetStatusRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (request.Note is { Length: > 1900 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 1900 karakter olabilir."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var asset = await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} AND \"DeletedAt\" IS NULL FOR UPDATE").IgnoreQueryFilters()
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            if (!AssetLocationIsInScope(asset.LocationCatalogId, allowedLocations)) return Results.NotFound();
            if (asset.Status != AssetStatuses.Archived)
                return Results.Conflict(new { message = "Yalnızca arşivlenmiş varlıklar geri alınabilir." });
            if (asset.AssignedEmployeeId is not null)
                return Results.Conflict(new { message = "Arşivlenmiş varlık beklenmedik biçimde zimmetli; önce zimmet ilişkisini çözün." });
            if (await db.AssetMaintenances.AnyAsync(x => x.CompanyId == companyId && x.AssetId == id && x.Status == "InProgress", cancellationToken))
                return Results.Conflict(new { message = "Açık bakım kaydı tamamlanmadan varlık arşivden geri alınamaz." });

            asset.Status = AssetStatuses.Available;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            var details = $"Arşiv → Kullanılabilir. {Clean(request.Note)}".Trim();
            db.AssetEvents.Add(new AssetEvent
            {
                CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                EventType = "StatusChanged", Details = details
            });
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset",
                TargetId = asset.Id, Action = "Restored", Details = details
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { asset.Id, asset.AssetTag, asset.Status, asset.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapPatch("/assets/{id:guid}/status", async (Guid id, [FromBody] ChangeAssetStatusRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var next = request.Status?.Trim();
            if (request.Note is { Length: > 1900 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 1900 karakter olabilir."] });
            var statusConfigJson = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => x.AssetStatusLabelsJson).SingleOrDefaultAsync(cancellationToken);
            var statusConfig = AssetStatusConfiguration.Parse(statusConfigJson);
            if (!AssetStatusConfiguration.IsManualStatus(next, statusConfig))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Yalnızca şirketin tanımlı manuel varlık durumları seçilebilir; zimmet ve tedarikçi iadesi ayrı işlemlerdir."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var asset = await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} AND \"DeletedAt\" IS NULL FOR UPDATE").IgnoreQueryFilters()
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null) return Results.NotFound();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            if (!AssetLocationIsInScope(asset.LocationCatalogId, allowedLocations)) return Results.NotFound();
            if (asset.AssignedEmployeeId is not null)
                return Results.Conflict(new { message = "Önce varlığı çalışandan iade alın." });
            if (asset.Status == AssetStatuses.Archived && next != AssetStatuses.Archived)
                return Results.Conflict(new { message = "Arşivlenen varlığı tekrar kullanıma almak için ayrı geri alma akışı gereklidir." });
            if (!AssetStatusConfiguration.CanTransition(asset.Status, next, statusConfig))
                return Results.Conflict(new { message = "Şirketin durum geçiş kuralı bu değişikliğe izin vermiyor." });
            if (next != AssetStatuses.Maintenance && await db.AssetMaintenances.AnyAsync(x =>
                    x.CompanyId == companyId && x.AssetId == id && x.Status == "InProgress", cancellationToken))
                return Results.Conflict(new { message = "Açık bakım kaydı tamamlanmadan varlığın durumu değiştirilemez." });
            if (asset.Status == next) return Results.Ok(new { asset.Id, asset.Status });
            var previous = asset.Status;
            asset.Status = next!;
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
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var ids = request.AssetIds ?? [];
            var next = request.Status?.Trim();
            if (ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["1 ile 100 arasında, tekrarsız varlık seçin."] });
            if (request.Note is { Length: > 1900 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Açıklama en fazla 1900 karakter olabilir."] });
            var statusConfigJson = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => x.AssetStatusLabelsJson).SingleOrDefaultAsync(cancellationToken);
            var statusConfig = AssetStatusConfiguration.Parse(statusConfigJson);
            if (!AssetStatusConfiguration.IsManualStatus(next, statusConfig))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Yalnızca şirketin tanımlı manuel varlık durumları seçilebilir; zimmet ve tedarikçi iadesi ayrı işlemlerdir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var assetsQuery = db.Assets.FromSqlInterpolated(
                    $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) AND \"DeletedAt\" IS NULL ORDER BY \"Id\" FOR UPDATE").IgnoreQueryFilters();
            if (allowedLocations is not null)
                assetsQuery = assetsQuery.Where(asset => asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value));
            var assets = await assetsQuery.ToListAsync(cancellationToken);
            if (assets.Count != ids.Length)
                return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya bu şirkete ait değil. Hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => asset.AssignedEmployeeId is not null))
                return Results.Conflict(new { message = "Seçimde çalışana zimmetli varlık var. Önce iade işlemi yapın; hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => asset.Status == AssetStatuses.Archived && next != AssetStatuses.Archived))
                return Results.Conflict(new { message = "Arşivlenmiş varlıklar toplu işlemle yeniden etkinleştirilemez; hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => !AssetStatusConfiguration.CanTransition(asset.Status, next, statusConfig)))
                return Results.Conflict(new { message = "Seçimde şirketin durum geçiş kuralının reddettiği bir varlık var; hiçbir kayıt değiştirilmedi." });
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

        api.MapPost("/assets/bulk-assign", async ([FromBody] BulkAssignAssetsRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var ids = request.AssetIds ?? [];
            if (ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["1 ile 100 arasında, tekrarsız varlık seçin."] });
            if (request.EmployeeId == Guid.Empty)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Etkin çalışan bu şirkette bulunamadı."] });
            if (request.Note is { Length: > 1900 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Toplu zimmet notu en fazla 1900 karakter olabilir."] });
            if (!ValidExpectedReturnDate(request.ExpectedReturnOn))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["expectedReturnOn"] = ["Beklenen iade tarihi bugün ile 10 yıl sonrası arasında olmalıdır."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var employee = await db.Employees.FromSqlInterpolated(
                    $"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.EmployeeId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (employee is null || !employee.IsActive)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Etkin çalışan bu şirkette bulunamadı."] });
            if (!EmployeeIsInScope(employee.LocationCatalogId, employee.CompanyUnitId, employeeAccessScope))
                return Results.NotFound(new { message = "Seçilen çalışan bulunamadı veya erişim kapsamınız dışında. Hiçbir kayıt değiştirilmedi." });
            var assets = await db.Assets.FromSqlInterpolated(
                    $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) AND \"DeletedAt\" IS NULL ORDER BY \"Id\" FOR UPDATE").IgnoreQueryFilters()
                .Where(asset => allowedLocations == null || (asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)))
                .ToListAsync(cancellationToken);
            if (assets.Count != ids.Length)
                return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya bu şirkete ait değil. Hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => asset.AssignedEmployeeId is not null || asset.Status != AssetStatuses.Available))
                return Results.Conflict(new { message = "Toplu zimmet için seçilen bütün varlıklar boşta ve kullanılabilir olmalı. Hiçbir kayıt değiştirilmedi." });

            var now = DateTimeOffset.UtcNow;
            var note = Clean(request.Note);
            foreach (var asset in assets)
            {
                var reservationConflict = await CheckReservationForAssignmentAsync(db, companyId, asset.Id, employee.Id,
                    actorId, request.ExpectedReturnOn, cancellationToken);
                if (reservationConflict is not null)
                    return Results.Conflict(new { message = $"{asset.AssetTag}: {reservationConflict} Toplu işlem uygulanmadı." });
                asset.AssignedEmployeeId = employee.Id;
                asset.ExpectedReturnOn = request.ExpectedReturnOn;
                asset.Status = AssetStatuses.Deployed;
                asset.UpdatedAt = now;
                var details = AssignmentDetails(string.IsNullOrWhiteSpace(note) ? "Toplu zimmet işlemi." : $"Toplu zimmet işlemi. {note}", request.ExpectedReturnOn);
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id,
                    ActorUserId = actorId, EmployeeId = employee.Id, EventType = "Assigned", Details = details });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Asset", TargetId = asset.Id, Action = "BulkAssigned", Details = details });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { updated = assets.Count, employeeId = employee.Id, employeeName = employee.DisplayName });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/bulk-return", async ([FromBody] BulkReturnAssetsRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedCompanyUnits = employeeAccessScope.AllowedCompanyUnits;
            var ids = request.AssetIds ?? [];
            if (ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["1 ile 100 arasında, tekrarsız varlık seçin."] });
            if (request.Note is { Length: > 1900 })
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Toplu iade notu en fazla 1900 karakter olabilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var assets = await db.Assets.FromSqlInterpolated(
                    $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({ids}) AND \"DeletedAt\" IS NULL ORDER BY \"Id\" FOR UPDATE").IgnoreQueryFilters()
                .Where(asset => allowedLocations == null || (asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)))
                .ToListAsync(cancellationToken);
            if (assets.Count != ids.Length)
                return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya bu şirkete ait değil. Hiçbir kayıt değiştirilmedi." });
            if (assets.Any(asset => asset.AssignedEmployeeId is null))
                return Results.Conflict(new { message = "Toplu iade için seçilen bütün varlıklar bir çalışana zimmetli olmalı. Hiçbir kayıt değiştirilmedi." });
            if (allowedLocations is not null || allowedCompanyUnits is not null)
            {
                var assignedEmployeeIds = assets.Select(asset => asset.AssignedEmployeeId!.Value).Distinct().ToArray();
                var assignedEmployees = await db.Employees.AsNoTracking().Where(employee => employee.CompanyId == companyId &&
                    assignedEmployeeIds.Contains(employee.Id)).Select(employee => new
                    { employee.Id, employee.LocationCatalogId, employee.CompanyUnitId }).ToListAsync(cancellationToken);
                if (assignedEmployees.Count != assignedEmployeeIds.Length || assignedEmployees.Any(employee =>
                        !EmployeeIsInScope(employee.LocationCatalogId, employee.CompanyUnitId, employeeAccessScope)))
                    return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya erişim kapsamınız dışında. Hiçbir kayıt değiştirilmedi." });
            }

            var now = DateTimeOffset.UtcNow;
            var note = Clean(request.Note);
            foreach (var asset in assets)
            {
                var previousEmployeeId = asset.AssignedEmployeeId!.Value;
                var previousExpectedReturnOn = asset.ExpectedReturnOn;
                asset.AssignedEmployeeId = null;
                asset.ExpectedReturnOn = null;
                asset.Status = AssetStatuses.Available;
                asset.UpdatedAt = now;
                var details = ReturnDetails(string.IsNullOrWhiteSpace(note) ? "Toplu iade işlemi." : $"Toplu iade işlemi. {note}", previousExpectedReturnOn);
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id,
                    ActorUserId = actorId, EmployeeId = previousEmployeeId, EventType = "Returned", Details = details });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                    TargetType = "Asset", TargetId = asset.Id, Action = "BulkReturned", Details = details });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { updated = assets.Count });
        }).RequireAuthorization("asset.write");

        api.MapPost("/assets/export-selected.csv", async ([FromBody] SelectedAssetExportRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var ids = request.AssetIds ?? [];
            if (ids.Length is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assetIds"] = ["1 ile 100 arasında, tekrarsız varlık seçin."] });
            var customFields = await db.AssetCustomFieldDefinitions.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
            var rows = await db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && ids.Contains(x.Id))
                .Where(x => allowedLocations == null || (x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value)))
                .OrderBy(x => x.AssetTag)
                .Select(x => new SelectedAssetExportRow(
                    x.AssetTag, x.Name, x.Status,
                    db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault(),
                    db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId).Select(b => b.Name).FirstOrDefault(),
                    db.AssetModels.Where(m => m.CompanyId == companyId && m.Id == x.ModelId).Select(m => m.Name).FirstOrDefault(),
                    x.SerialNumber, x.Location,
                    db.Assets.Where(parent => parent.CompanyId == companyId && parent.Id == x.ParentAssetId).Select(parent => parent.AssetTag).FirstOrDefault(),
                    db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                    db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.AssignedEmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    x.PurchaseDate, x.WarrantyExpiresAt, x.PurchaseCost, x.Currency, x.CustomFieldsJson, x.IsByod))
                .ToListAsync(cancellationToken);
            if (rows.Count != ids.Length) return Results.NotFound(new { message = "Seçilen varlıklardan biri bulunamadı veya bu şirkete ait değil." });
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(';', new[] { "Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi", "Garanti Bitiş Tarihi", "Alış Bedeli", "Para Birimi", "Üst Varlık Etiketi", "BYOD" }
                .Concat(customFields.Select(field => $"Özel alan: {field.Key}")).Select(Cell)));
            foreach (var row in rows)
                csv.AppendLine(string.Join(';', new[] { Cell(row.AssetTag), Cell(row.Name), Cell(row.Status),
                    Cell(row.Category), Cell(row.Brand), Cell(row.Model), Cell(row.SerialNumber),
                    Cell(row.Location), Cell(row.EmployeeNumber), Cell(row.EmployeeName),
                    Cell(row.PurchaseDate?.ToString("yyyy-MM-dd")),
                    Cell(row.WarrantyExpiresAt?.ToString("yyyy-MM-dd")),
                    Cell(row.PurchaseCost?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                    Cell(row.Currency), Cell(row.ParentAssetTag), Cell(row.IsByod ? "Evet" : "Hayır") }
                    .Concat(AssetCustomFieldEndpoints.ExportValues(row.CustomFieldsJson, customFields).Select(Cell))));
            return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
                "text/csv; charset=utf-8", $"envantertr-secili-varliklar-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/activity", async ([FromQuery] Guid? assetId, [FromQuery] Guid? employeeId,
            [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
            [FromQuery] string? eventType, [FromQuery] string? q,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (from is not null && to is not null && from.Value > to.Value)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedLocations = employeeAccessScope.AllowedLocations;
            var allowedEmployeeIds = employeeAccessScope.IsRestricted
                ? await GetAllowedEmployeeIdsAsync(db, companyId, employeeAccessScope, cancellationToken) : null;
            var query = db.AssetEvents.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations is not null)
            {
                var allowedAssetIds = db.Assets.AsNoTracking().Where(asset => asset.CompanyId == companyId &&
                    asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)).Select(asset => asset.Id);
                query = query.Where(x => allowedAssetIds.Contains(x.AssetId));
            }
            if (assetId is Guid selectedAsset) query = query.Where(x => x.AssetId == selectedAsset);
            if (employeeId is Guid selectedEmployee)
            {
                if (allowedEmployeeIds is not null && !allowedEmployeeIds.Contains(selectedEmployee))
                    query = query.Where(_ => false);
                else
                    query = query.Where(x => x.EmployeeId == selectedEmployee);
            }
            if (from is DateTimeOffset start) query = query.Where(x => x.OccurredAt >= start);
            if (to is DateTimeOffset end) query = query.Where(x => x.OccurredAt <= end);
            if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(x => x.EventType == eventType.Trim());
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                var matchingEmployees = db.Employees.AsNoTracking().Where(e => e.CompanyId == companyId &&
                    (e.DisplayName.Contains(term) || e.EmployeeNumber.Contains(term)));
                if (allowedEmployeeIds is not null) matchingEmployees = matchingEmployees.Where(e => allowedEmployeeIds.Contains(e.Id));
                query = query.Where(x => (x.Details != null &&
                        (allowedEmployeeIds == null || x.EmployeeId == null || allowedEmployeeIds.Contains(x.EmployeeId.Value)) &&
                        x.Details.Contains(term)) ||
                    db.Assets.Any(a => a.CompanyId == companyId && a.Id == x.AssetId &&
                        (a.AssetTag.Contains(term) || a.Name.Contains(term))) ||
                    matchingEmployees.Any(e => e.Id == x.EmployeeId));
            }
            var total = await query.CountAsync(cancellationToken);
            var entries = await query.OrderByDescending(x => x.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.AssetId, AssetTag = db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.AssetId).Select(a => a.AssetTag).FirstOrDefault(),
                    x.EventType,
                    Details = allowedEmployeeIds == null || x.EmployeeId == null || allowedEmployeeIds.Contains(x.EmployeeId.Value) ? x.Details : null,
                    EmployeeId = allowedEmployeeIds == null || x.EmployeeId == null || allowedEmployeeIds.Contains(x.EmployeeId.Value) ? x.EmployeeId : null,
                    EmployeeName = (allowedEmployeeIds == null || x.EmployeeId == null || allowedEmployeeIds.Contains(x.EmployeeId.Value))
                        ? db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault()
                        : null,
                    x.ActorUserId,
                    ActorName = (allowedEmployeeIds == null || !db.CompanyMemberships.Any(m => m.CompanyId == companyId &&
                        m.UserId == x.ActorUserId && m.EmployeeId.HasValue && !allowedEmployeeIds.Contains(m.EmployeeId.Value)))
                        ? db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault()
                        : null,
                    x.OccurredAt
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { entries, total, page, pageSize });
        }).RequireAuthorization("asset.read");

        api.MapGet("/audit", async ([FromQuery] string? q, [FromQuery] string? targetType, [FromQuery] string? action, DateTimeOffset? from, DateTimeOffset? to,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (!ValidateAuditFilters(q, targetType, action, from, to, out var filterError)) return filterError!;
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedLocations = employeeAccessScope.AllowedLocations;
            var allowedEmployeeIds = employeeAccessScope.IsRestricted
                ? await GetAllowedEmployeeIdsAsync(db, companyId, employeeAccessScope, cancellationToken) : null;
            var query = ApplyAuditLocationScope(db, companyId,
                FilterAuditEntries(db, companyId, q, targetType, action, from, to, allowedEmployeeIds), employeeAccessScope);
            var total = await query.CountAsync(cancellationToken);
            var entries = await query.OrderByDescending(x => x.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.TargetType, x.TargetId, x.Action,
                    Details = allowedLocations == null ? x.Details : null,
                    x.OccurredAt,
                    ActorName = (allowedEmployeeIds == null || !db.CompanyMemberships.Any(m => m.CompanyId == companyId &&
                        m.UserId == x.ActorUserId && m.EmployeeId.HasValue && !allowedEmployeeIds.Contains(m.EmployeeId.Value)))
                        ? db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault()
                        : null
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { entries, total, page, pageSize });
        }).RequireAuthorization("settings.manage");

        api.MapGet("/audit/actions", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var query = ApplyAuditLocationScope(db, companyId, db.AuditEntries.AsNoTracking().Where(x => x.CompanyId == companyId), employeeAccessScope);
            var actions = await query
                .Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
            return Results.Ok(new { actions });
        }).RequireAuthorization("settings.manage");

        api.MapGet("/audit/export.csv", async (string? q, string? targetType, string? action, DateTimeOffset? from, DateTimeOffset? to,
            HttpContext context, ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return; }
            if (!ValidateAuditFilters(q, targetType, action, from, to, out var filterError))
            {
                await filterError!.ExecuteAsync(context);
                return;
            }
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            var allowedLocations = employeeAccessScope.AllowedLocations;
            var allowedEmployeeIds = employeeAccessScope.IsRestricted
                ? await GetAllowedEmployeeIdsAsync(db, companyId, employeeAccessScope, cancellationToken) : null;
            var query = ApplyAuditLocationScope(db, companyId,
                FilterAuditEntries(db, companyId, q, targetType, action, from, to, allowedEmployeeIds), employeeAccessScope);
            var total = await query.CountAsync(cancellationToken);
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/csv; charset=utf-8";
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=envantertr-denetim-kayitlari-{DateTime.UtcNow:yyyyMMdd}.csv";
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["X-Total-Count"] = total.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await using var writer = new StreamWriter(context.Response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 16 * 1024, leaveOpen: true);
            await writer.WriteLineAsync("Tarih;İşlemi yapan;Kayıt türü;Kayıt kimliği;İşlem;Ayrıntı;Kayıt türü kodu;İşlem kodu".AsMemory(), cancellationToken);
            var rows = query.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id)
                .Select(x => new { x.OccurredAt, x.TargetType, x.TargetId, x.Action,
                    Details = allowedLocations == null ? x.Details : null,
                    ActorName = (allowedEmployeeIds == null || !db.CompanyMemberships.Any(m => m.CompanyId == companyId &&
                        m.UserId == x.ActorUserId && m.EmployeeId.HasValue && !allowedEmployeeIds.Contains(m.EmployeeId.Value)))
                        ? db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.DisplayName).FirstOrDefault()
                        : null });
            var written = 0;
            await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                var line = string.Join(';', new[] { Cell(row.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", System.Globalization.CultureInfo.InvariantCulture)),
                    Cell(row.ActorName ?? "Silinmiş kullanıcı"), Cell(AuditTargetLabel(row.TargetType)), Cell(row.TargetId?.ToString()),
                    Cell(AuditActionLabel(row.Action)), Cell(row.Details), Cell(row.TargetType), Cell(row.Action) });
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
                if (++written % 500 == 0) await writer.FlushAsync(cancellationToken);
            }
            await writer.FlushAsync(cancellationToken);
        }).RequireAuthorization("settings.manage");

        api.MapGet("/members", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken) is not null)
                return Results.Forbid();
            var members = await db.CompanyMemberships.AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.RoleKey == "owner" ? 0 : x.RoleKey == "admin" ? 1 : 2)
                .ThenBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.UserId, x.RoleKey, x.CreatedAt, x.LocationAccessMode, x.CompanyUnitAccessMode,
                    x.EmployeeId,
                    IsCurrentUser = x.UserId == actorId,
                    Name = db.Users.Where(u => u.Id == x.UserId).Select(u => u.DisplayName).FirstOrDefault(),
                    Email = db.Users.Where(u => u.Id == x.UserId).Select(u => u.Email).FirstOrDefault(),
                    IsInvitationPending = db.Users.Where(u => u.Id == x.UserId).Select(u => !u.EmailConfirmed).FirstOrDefault(),
                    EmployeeName = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    EmployeeNumber = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault()
                }).ToListAsync(cancellationToken);
            var customRoles = await db.CompanyCustomRoles.AsNoTracking().Where(role => role.CompanyId == companyId && role.IsActive)
                .OrderBy(role => role.Name).Select(role => role.RoleKey).ToArrayAsync(cancellationToken);
            return Results.Ok(new { members, total = members.Count,
                roles = new[] { "admin", "asset_manager", "asset_operator", "auditor", "viewer", "hr_operator" }.Concat(customRoles).ToArray() });
        }).RequireAuthorization("settings.manage");

        api.MapPost("/members/invite", async ([FromBody] InviteCompanyMemberRequest request,
            ClaimsPrincipal user, AppDbContext db, UserManager<EnvanterUser> userManager,
            IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken) is not null)
                return Results.Forbid();
            var name = request.FullName?.Trim();
            var email = request.Email?.Trim();
            var roleKey = request.RoleKey?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || string.IsNullOrWhiteSpace(email) ||
                email.Length > 256 || !MailAddress.TryCreate(email, out _) || roleKey is null ||
                roleKey == "owner" || !await CompanyRoleAuthorization.CanUseAsync(db, companyId, roleKey, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["invite"] = ["Ad, geçerli e-posta ve desteklenen rol gereklidir."] });

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                var existingMembership = await db.CompanyMemberships.SingleOrDefaultAsync(
                    x => x.CompanyId == companyId && x.UserId == existingUser.Id, cancellationToken);
                if (existingMembership is not null)
                {
                    if (existingUser.EmailConfirmed)
                        return Results.Conflict(new { message = "Bu kullanıcı bu şirket alanının üyesi. Rolünü kullanıcı listesinden değiştirebilirsiniz." });
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
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var lockedCompany = await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedCompany is null || !lockedCompany.IsActive) return Results.Forbid();
            if (!await CompanyRoleAuthorization.CanUseAsync(db, companyId, roleKey, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["invite"] = ["Ad, geçerli e-posta ve etkin şirket rolü gereklidir."] });
            var invitationRolePermissions = CompanyRoleAuthorization.IsBuiltIn(roleKey)
                ? CompanyRoleAuthorization.BuiltIns.Single(role => role.RoleKey == roleKey).PermissionKeys
                : await db.CompanyCustomRoles.AsNoTracking().Where(role => role.CompanyId == companyId && role.RoleKey == roleKey)
                    .Select(role => CompanyRoleAuthorization.ParsePermissionKeys(role.PermissionsJson)).SingleOrDefaultAsync(cancellationToken);
            if ((roleKey == "admin" || invitationRolePermissions?.Contains(CompanyRoleAuthorization.CompanySettings, StringComparer.Ordinal) == true) &&
                user.FindFirstValue("company_role") != "owner") return Results.Forbid();
            var now = DateTimeOffset.UtcNow;
            var subscription = await (from item in db.TenantSubscriptions.AsNoTracking()
                join plan in db.SubscriptionPlans.AsNoTracking() on item.PlanId equals plan.Id
                where item.CompanyId == companyId && (item.Status == "Trial" || item.Status == "Active") &&
                    item.StartsAt <= now && item.EndsAt > now
                select new { item.Id, plan.DeploymentKind, plan.MaxLoginUsers }).SingleOrDefaultAsync(cancellationToken);
            if (subscription is null)
                return Results.Conflict(new { message = "Şirketin etkin deneme veya aboneliği bulunamadı. Kullanıcı davetinden önce lisansı etkinleştirin." });
            var memberCount = await db.CompanyMemberships.CountAsync(x => x.CompanyId == companyId, cancellationToken);
            if (subscription.DeploymentKind != "cloud" && subscription.MaxLoginUsers is int seatLimit && memberCount >= seatLimit)
                return Results.Conflict(new { message = $"Bu plan {seatLimit} giriş hesabıyla sınırlı. Kullanıcı eklemek için planı yükseltin." });

            var invitedUser = existingUser;
            if (invitedUser is null)
            {
                var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!";
                invitedUser = new EnvanterUser
                {
                    UserName = email, Email = email, EmailConfirmed = false, DisplayName = name
                };
                var createResult = await userManager.CreateAsync(invitedUser, temporaryPassword);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Results.Conflict(new { message = "Kullanıcı oluşturulamadı. E-posta daha önce kaydedilmiş olabilir." });
                }
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

            if (invitedUser.EmailConfirmed)
            {
                try
                {
                    await AccountEndpoints.SendCompanyAccessAddedEmailAsync(email, invitedUser.DisplayName ?? name,
                        lockedCompany.Name, roleKey, configuration, cancellationToken);
                    return Results.Created($"/api/members/{invitedUser.Id}", new { invitationSent = true, userId = invitedUser.Id,
                        message = "Mevcut hesap şirket alanına eklendi; erişim bilgisi e-postayla gönderildi." });
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    return Results.Created($"/api/members/{invitedUser.Id}", new { invitationSent = false, userId = invitedUser.Id,
                        message = "Mevcut hesap şirket alanına eklendi; e-posta gönderilemedi. Kullanıcı mevcut hesabıyla şirketi seçerek giriş yapabilir." });
                }
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
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken) is not null)
                return Results.Forbid();
            var nextRole = request.RoleKey?.Trim();
            if (nextRole is null || nextRole == "owner")
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["roleKey"] = ["Desteklenmeyen şirket rolü."] });
            if (userId == actorId)
                return Results.Conflict(new { message = "Kendi rolünüzü bu ekrandan değiştiremezsiniz; şirket erişiminizin kesilmesi önleniyor." });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var lockedCompany = await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedCompany is null || !lockedCompany.IsActive) return Results.Forbid();
            if (!await CompanyRoleAuthorization.CanUseAsync(db, companyId, nextRole, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["roleKey"] = ["Desteklenmeyen şirket rolü."] });
            var membership = await db.CompanyMemberships.FromSqlInterpolated(
                    $"SELECT * FROM \"CompanyMemberships\" WHERE \"CompanyId\" = {companyId} AND \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (membership is null) return Results.NotFound();
            if (membership.RoleKey == "owner")
                return Results.Conflict(new { message = "Şirket sahibi rolü normal rol düzenlemesiyle değiştirilemez." });
            var actorRole = user.FindFirstValue("company_role");
            var targetRolePermissions = CompanyRoleAuthorization.IsBuiltIn(nextRole)
                ? CompanyRoleAuthorization.BuiltIns.Single(role => role.RoleKey == nextRole).PermissionKeys
                : await db.CompanyCustomRoles.AsNoTracking().Where(role => role.CompanyId == companyId && role.RoleKey == nextRole)
                    .Select(role => CompanyRoleAuthorization.ParsePermissionKeys(role.PermissionsJson)).SingleOrDefaultAsync(cancellationToken);
            var currentRolePermissions = CompanyRoleAuthorization.IsBuiltIn(membership.RoleKey)
                ? CompanyRoleAuthorization.BuiltIns.Single(role => role.RoleKey == membership.RoleKey).PermissionKeys
                : await db.CompanyCustomRoles.AsNoTracking().Where(role => role.CompanyId == companyId && role.RoleKey == membership.RoleKey)
                    .Select(role => CompanyRoleAuthorization.ParsePermissionKeys(role.PermissionsJson)).SingleOrDefaultAsync(cancellationToken);
            var targetElevated = nextRole == "admin" || targetRolePermissions?.Contains(CompanyRoleAuthorization.CompanySettings, StringComparer.Ordinal) == true;
            var currentElevated = membership.RoleKey == "admin" || currentRolePermissions?.Contains(CompanyRoleAuthorization.CompanySettings, StringComparer.Ordinal) == true;
            if (actorRole != "owner" && (currentElevated || targetElevated))
                return Results.Forbid();
            if (membership.RoleKey == nextRole)
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { membership.UserId, membership.RoleKey });
            }

            var previousRole = membership.RoleKey;
            membership.RoleKey = nextRole;
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "CompanyMembership", TargetId = userId,
                Action = "RoleChanged", Details = $"{previousRole} → {nextRole}"
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { membership.UserId, membership.RoleKey });
        }).RequireAuthorization("settings.manage");

        api.MapPatch("/members/{userId:guid}/employee", async (Guid userId,
            [FromBody] LinkCompanyMemberEmployeeRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken) is not null)
                return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var membership = await db.CompanyMemberships.FromSqlInterpolated(
                    $"SELECT * FROM \"CompanyMemberships\" WHERE \"CompanyId\" = {companyId} AND \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (membership is null) return Results.NotFound();
            string? employeeName = null;
            string? employeeNumber = null;
            if (request.EmployeeId is Guid employeeId)
            {
                var employee = await db.Employees.FromSqlInterpolated(
                        $"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {employeeId} FOR SHARE")
                    .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
                if (employee is null || !employee.IsActive)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Şirket içinde etkin bir çalışan seçin."] });
                employeeName = employee.DisplayName;
                employeeNumber = employee.EmployeeNumber;
            }
            if (membership.EmployeeId == request.EmployeeId)
                return Results.Ok(new { membership.UserId, membership.EmployeeId, employeeName, employeeNumber });
            membership.EmployeeId = request.EmployeeId;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "CompanyMembership", TargetId = userId, Action = "EmployeeLinkChanged",
                Details = request.EmployeeId is null ? "Giriş hesabının çalışan bağlantısı kaldırıldı."
                    : $"Giriş hesabı çalışan kaydına bağlandı ({employeeNumber})." });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Bu çalışan başka bir giriş hesabına bağlı. Önce mevcut bağlantıyı kaldırın." });
            }
            return Results.Ok(new { membership.UserId, membership.EmployeeId, employeeName, employeeNumber });
        }).RequireAuthorization("settings.manage");

        api.MapGet("/assets/export.csv", async ([FromQuery] string? q, HttpContext context, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 5000) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var customFields = await db.AssetCustomFieldDefinitions.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 5000 : pageSize, 1, 10000);
            var query = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (allowedLocations is not null)
                query = query.Where(asset => asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value));
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
                    x.PurchaseDate, x.WarrantyExpiresAt, x.PurchaseCost, x.Currency, x.CustomFieldsJson)).ToListAsync(cancellationToken);
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(';', new[] { "Etiket", "Ad", "Durum", "Seri No", "Konum", "Personel No", "Alış Tarihi", "Garanti Bitiş Tarihi", "Alış Bedeli", "Para Birimi" }
                .Concat(customFields.Select(field => $"Özel alan: {field.Key}")).Select(Cell)));
            foreach (var row in rows)
                csv.AppendLine(string.Join(';', new[] { Cell(row.AssetTag), Cell(row.Name), Cell(row.Status),
                    Cell(row.SerialNumber), Cell(row.Location), Cell(row.EmployeeNumber),
                    Cell(row.PurchaseDate?.ToString("yyyy-MM-dd")), Cell(row.WarrantyExpiresAt?.ToString("yyyy-MM-dd")), Cell(row.PurchaseCost?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)), Cell(row.Currency) }
                    .Concat(AssetCustomFieldEndpoints.ExportValues(row.CustomFieldsJson, customFields).Select(Cell))));
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            context.Response.Headers["X-Total-Count"] = total.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Page-Size"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Has-More"] = ((long)page * pageSize < total).ToString();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-varliklar-{DateTime.UtcNow:yyyyMMdd}-sayfa-{page}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/dashboard/summary", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken);
            var assets = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId);
            var accessories = db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId);
            var consumables = db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId);
            var components = db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId);
            var employees = db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId);
            var softwareLicenses = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId);
            var recentEvents = db.AssetEvents.AsNoTracking().Where(x => x.CompanyId == companyId && x.OccurredAt >= DateTimeOffset.UtcNow.AddDays(-7));
            var employeeAccessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, cancellationToken);
            if (employeeAccessScope.IsRestricted) employees = employeeAccessScope.Apply(employees);
            if (allowedLocations is not null)
            {
                assets = assets.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
                accessories = accessories.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
                consumables = consumables.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
                components = components.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
                softwareLicenses = softwareLicenses.Where(license => db.SoftwareLicenseAssignments.Any(assignment =>
                    assignment.CompanyId == companyId && assignment.LicenseId == license.Id && assignment.RevokedAt == null &&
                    (assignment.AssetId.HasValue && assets.Any(asset => asset.Id == assignment.AssetId.Value) ||
                     assignment.EmployeeId.HasValue && employees.Any(employee => employee.Id == assignment.EmployeeId.Value))));
                recentEvents = recentEvents.Where(e => assets.Any(asset => asset.Id == e.AssetId));
            }
            var statusCounts = await assets.GroupBy(x => x.Status)
                .Select(group => new { status = group.Key, count = group.Count() })
                .ToDictionaryAsync(row => row.status, row => row.count, cancellationToken);
            var summary = new
            {
                totalAssets = statusCounts.Values.Sum(),
                availableAssets = statusCounts.GetValueOrDefault(AssetStatuses.Available),
                assignedAssets = statusCounts.GetValueOrDefault(AssetStatuses.Deployed),
                pendingAssets = statusCounts.GetValueOrDefault(AssetStatuses.Pending),
                maintenanceAssets = statusCounts.GetValueOrDefault(AssetStatuses.Maintenance),
                unusableAssets = statusCounts.GetValueOrDefault(AssetStatuses.Unusable),
                archivedAssets = statusCounts.GetValueOrDefault(AssetStatuses.Archived),
                softwareLicenses = await softwareLicenses.CountAsync(cancellationToken),
                accessories = await accessories.CountAsync(cancellationToken),
                consumables = await consumables.CountAsync(cancellationToken),
                components = await components.CountAsync(cancellationToken),
                activeEmployees = await employees.CountAsync(x => x.IsActive, cancellationToken),
                recentActivityCount = await recentEvents.CountAsync(cancellationToken)
            };
            return Results.Ok(summary);
        }).RequireAuthorization("asset.read");

        return endpoints;
    }

    private sealed record ExportRow(string AssetTag, string Name, string Status, string? SerialNumber,
        string? Location, string? EmployeeNumber, DateOnly? PurchaseDate, DateOnly? WarrantyExpiresAt, decimal? PurchaseCost, string Currency, string? CustomFieldsJson);
    private sealed record CreateDepartmentRequest(string? Name, Guid? ParentDepartmentId = null, Guid? LocationCatalogId = null);
    private sealed record UpdateDepartmentRequest(string? Name, Guid? ParentDepartmentId = null, Guid? LocationCatalogId = null);
    private sealed record MoveDepartmentEmployeesRequest(Guid TargetDepartmentId, int BatchSize = 100);
    private sealed record SetDepartmentManagerRequest(Guid? EmployeeId);
    private sealed record CompanyUnitRequest(string? Name, string? Code, Guid? ParentCompanyUnitId = null, string? UnitType = null);
    private sealed record CompanyUnitActiveRequest(bool IsActive);
    private sealed record UpdateCompanyProfileRequest(string? Name, string? ContactEmail, string? BrandPrimaryColor, string? Slug = null);
    public sealed record LabelPrintSettingsRequest(string? Barcode1d, string? Barcode2d, decimal WidthMm,
        decimal HeightMm, decimal OffsetXmm, decimal OffsetYmm, decimal PaddingMm, decimal CodeSizeMm,
        string? QrTarget, bool ShowName, bool ShowSerial, bool ShowModel, bool ShowLocation, bool ShowStatus);
    public sealed record LabelPrintTemplateRequest(Guid Id, string Name, string? Manufacturer, string? PrinterModel,
        string? ProductCode, LabelPrintSettingsRequest Settings);
    public sealed record LabelPrintTemplatesRequest(List<LabelPrintTemplateRequest>? Templates);
    private sealed record LabelPrintSettingsDocument(LabelPrintSettingsRequest? Settings,
        List<LabelPrintTemplateRequest>? Templates);
    private static readonly LabelPrintSettingsRequest DefaultLabelPrintSettings = new("code128", "qrcode", 86, 48,
        0, 0, 4, 25, "tag", true, true, false, false, false);

    private static LabelPrintSettingsDocument ReadLabelPrintDocument(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("Settings", out _))
        {
            var stored = JsonSerializer.Deserialize<LabelPrintSettingsDocument>(raw);
            return new LabelPrintSettingsDocument(stored?.Settings ?? DefaultLabelPrintSettings,
                stored?.Templates ?? []);
        }
        return new LabelPrintSettingsDocument(JsonSerializer.Deserialize<LabelPrintSettingsRequest>(raw) ?? DefaultLabelPrintSettings, []);
    }

    private static bool IsValidLabelSettings(LabelPrintSettingsRequest request) =>
        request.Barcode1d is "code128" or "code39" or "code93" or "code93ext" or "rationalizedCodabar" or "msi" or "ean5" or "ean13" or "upca" or "upce" or "none" &&
        request.Barcode2d is "qrcode" or "pdf417" or "datamatrix" or "azteccode" or "none" &&
        request.QrTarget is "tag" or "asset-url" && request.WidthMm is >= 25 and <= 150 &&
        request.HeightMm is >= 15 and <= 100 && request.OffsetXmm is >= -30 and <= 30 &&
        request.OffsetYmm is >= -30 and <= 30 && request.PaddingMm is >= 0 and <= 15 &&
        request.CodeSizeMm is >= 10 and <= 45;

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

    private static bool AssetLocationIsInScope(Guid? locationId, HashSet<Guid>? allowedLocations) =>
        allowedLocations is null || locationId is Guid id && allowedLocations.Contains(id);

    private static bool EmployeeIsInScope(Guid? locationId, Guid? companyUnitId, EmployeeAccessScope employeeAccessScope) =>
        employeeAccessScope.Allows(locationId, companyUnitId);

    private static IQueryable<Employee> EmployeesInScope(AppDbContext db, Guid companyId,
        EmployeeAccessScope employeeAccessScope, bool tracking = false)
    {
        var query = (tracking ? db.Employees : db.Employees.AsNoTracking())
            .Where(employee => employee.CompanyId == companyId);
        return employeeAccessScope.Apply(query);
    }

    private static async Task<bool> EmployeeLocationIsInScopeAsync(AppDbContext db, Guid companyId, Guid employeeId,
        EmployeeAccessScope employeeAccessScope, CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == employeeId)
            .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(ct);
        return employee is not null && EmployeeIsInScope(employee.LocationCatalogId, employee.CompanyUnitId, employeeAccessScope);
    }

    private static string NormalizeDepartmentName(string value) => value.Trim().ToUpperInvariant();

    private static bool ValidExpectedReturnDate(DateOnly? date)
    {
        if (date is null) return true;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return date >= today && date <= today.AddYears(10);
    }

    private static async Task<string?> CheckReservationForAssignmentAsync(AppDbContext db, Guid companyId, Guid assetId,
        Guid employeeId, Guid actorId, DateOnly? expectedReturnOn, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var checkoutEndsOn = expectedReturnOn ?? DateOnly.MaxValue;
        var reservations = await db.AssetReservations.Where(x => x.CompanyId == companyId && x.AssetId == assetId &&
                (x.Status == "Pending" || x.Status == "Approved") && x.EndsOn >= today && x.StartsOn <= checkoutEndsOn)
            .OrderBy(x => x.StartsOn).ToListAsync(cancellationToken);
        var fulfilledReservation = reservations.FirstOrDefault(x => x.Status == "Approved" && x.EmployeeId == employeeId &&
            x.StartsOn <= today && x.EndsOn >= today);

        var conflicts = reservations.Where(x => x.Id != fulfilledReservation?.Id).ToArray();
        if (conflicts.Length > 0)
            return "Varlığın bekleyen veya onaylanmış rezervasyon tarihleri bu zimmetle çakışıyor.";
        if (fulfilledReservation is null) return null;
        if (expectedReturnOn is DateOnly returnDate && returnDate > fulfilledReservation.EndsOn)
            return "Zimmetin beklenen iade tarihi onaylı rezervasyonun bitişinden sonra olamaz.";

        var now = DateTimeOffset.UtcNow;
        fulfilledReservation.Status = "Completed";
        fulfilledReservation.ReviewedByUserId = actorId;
        fulfilledReservation.ReviewedAt = fulfilledReservation.UpdatedAt = now;
        db.AuditEntries.Add(new AuditEntry
        {
            CompanyId = companyId,
            ActorUserId = actorId,
            TargetType = "AssetReservation",
            TargetId = fulfilledReservation.Id,
            Action = "ReservationCompleted",
            Details = $"Zimmetle karşılandı · {fulfilledReservation.StartsOn:yyyy-MM-dd} – {fulfilledReservation.EndsOn:yyyy-MM-dd}"
        });
        return null;
    }

    private static async Task<bool> HasOutOfScopeEmployeeAssignmentsAsync(AppDbContext db, Guid companyId, Guid[] employeeIds,
        HashSet<Guid>? allowedLocations, CancellationToken cancellationToken)
    {
        if (employeeIds.Length == 0 || allowedLocations is null) return false;
        if (await db.Assets.AsNoTracking().AnyAsync(asset => asset.CompanyId == companyId &&
                asset.AssignedEmployeeId != null && employeeIds.Contains(asset.AssignedEmployeeId.Value) &&
                (!asset.LocationCatalogId.HasValue || !allowedLocations.Contains(asset.LocationCatalogId.Value)), cancellationToken))
            return true;
        if (await db.AccessoryAssignments.AsNoTracking().AnyAsync(assignment => assignment.CompanyId == companyId &&
                assignment.EmployeeId != null && employeeIds.Contains(assignment.EmployeeId.Value) &&
                assignment.ReturnedQuantity < assignment.Quantity &&
                !db.Accessories.Any(accessory => accessory.CompanyId == companyId && accessory.Id == assignment.AccessoryId &&
                    accessory.LocationCatalogId.HasValue && allowedLocations.Contains(accessory.LocationCatalogId.Value)), cancellationToken))
            return true;
        return await db.SoftwareLicenseAssignments.AsNoTracking().AnyAsync(assignment => assignment.CompanyId == companyId &&
            assignment.EmployeeId != null && employeeIds.Contains(assignment.EmployeeId.Value) && assignment.RevokedAt == null &&
            assignment.AssetId.HasValue &&
            !db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == assignment.AssetId &&
                asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)), cancellationToken);
    }

    private static async Task<EmployeeOffboardingCounts> ReturnEmployeeAssignmentsAsync(AppDbContext db, Guid companyId,
        Guid actorId, Guid[] employeeIds, CancellationToken cancellationToken)
    {
        if (employeeIds.Length == 0) return EmployeeOffboardingCounts.Empty;
        var now = DateTimeOffset.UtcNow;
        const string note = "Çalışan pasife alınırken otomatik iade edildi.";
        var assets = await db.Assets.Where(asset => asset.CompanyId == companyId &&
            asset.AssignedEmployeeId != null && employeeIds.Contains(asset.AssignedEmployeeId.Value))
            .OrderBy(asset => asset.Id).ToListAsync(cancellationToken);
        foreach (var asset in assets)
        {
            var previousEmployeeId = asset.AssignedEmployeeId!.Value;
            var expectedReturnOn = asset.ExpectedReturnOn;
            asset.AssignedEmployeeId = null;
            asset.ExpectedReturnOn = null;
            asset.Status = AssetStatuses.Available;
            asset.UpdatedAt = now;
            db.AssetEvents.Add(new AssetEvent
            {
                CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                EmployeeId = previousEmployeeId, EventType = "Returned", Details = ReturnDetails(note, expectedReturnOn),
                OccurredAt = now
            });
        }

        var accessoryAssignments = await db.AccessoryAssignments.Where(assignment => assignment.CompanyId == companyId &&
            assignment.EmployeeId != null && employeeIds.Contains(assignment.EmployeeId.Value) &&
            assignment.ReturnedQuantity < assignment.Quantity).OrderBy(assignment => assignment.Id).ToListAsync(cancellationToken);
        foreach (var assignment in accessoryAssignments)
        {
            var quantity = assignment.Quantity - assignment.ReturnedQuantity;
            assignment.ReturnedQuantity = assignment.Quantity;
            assignment.LastReturnedAt = now;
            db.AccessoryMovements.Add(new AccessoryMovement
            {
                CompanyId = companyId, AccessoryId = assignment.AccessoryId, AssignmentId = assignment.Id,
                EmployeeId = assignment.EmployeeId, ActorUserId = actorId, MovementType = "Checkin",
                Quantity = quantity, Notes = note, OccurredAt = now
            });
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "Accessory", TargetId = assignment.AccessoryId,
                Action = "checked_in", Details = $"Çalışan pasife alınırken {quantity} adet aksesuar otomatik iade alındı."
            });
        }
        var accessoryIds = accessoryAssignments.Select(assignment => assignment.AccessoryId).Distinct().ToArray();
        if (accessoryIds.Length > 0)
        {
            var accessories = await db.Accessories.Where(accessory => accessory.CompanyId == companyId && accessoryIds.Contains(accessory.Id))
                .ToListAsync(cancellationToken);
            foreach (var accessory in accessories) accessory.UpdatedAt = now;
        }

        var licenseAssignments = await db.SoftwareLicenseAssignments.Where(assignment => assignment.CompanyId == companyId &&
            assignment.EmployeeId != null && employeeIds.Contains(assignment.EmployeeId.Value) && assignment.RevokedAt == null)
            .OrderBy(assignment => assignment.Id).ToListAsync(cancellationToken);
        foreach (var assignment in licenseAssignments)
        {
            assignment.RevokedAt = now;
            db.SoftwareLicenseEvents.Add(new SoftwareLicenseEvent
            {
                CompanyId = companyId, LicenseId = assignment.LicenseId, EmployeeId = assignment.EmployeeId,
                AssetId = assignment.AssetId, ActorUserId = actorId, EventType = "Revoked",
                Details = note, OccurredAt = now
            });
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = actorId, TargetType = "SoftwareLicense",
                TargetId = assignment.LicenseId, Action = "Revoked",
                Details = "Çalışan pasife alınırken lisans ataması otomatik kaldırıldı."
            });
        }
        return new EmployeeOffboardingCounts(assets.Count, accessoryAssignments.Count, licenseAssignments.Count);
    }

    private sealed record EmployeeOffboardingCounts(int Assets, int Accessories, int Licenses)
    {
        public static EmployeeOffboardingCounts Empty { get; } = new(0, 0, 0);
    }

    private static string? AssignmentDetails(string? note, DateOnly? expectedReturnOn)
    {
        var parts = new List<string>();
        if (note is not null) parts.Add(note);
        if (expectedReturnOn is { } date) parts.Add($"Beklenen iade: {date:yyyy-MM-dd}");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string? ReturnDetails(string? note, DateOnly? expectedReturnOn)
    {
        var parts = new List<string>();
        if (note is not null) parts.Add(note);
        if (expectedReturnOn is { } date) parts.Add($"Planlı iade tarihi: {date:yyyy-MM-dd}");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EmployeePhotoDirectory(IConfiguration configuration, Guid companyId, Guid employeeId) =>
        Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media",
            companyId.ToString("N"), "employees", employeeId.ToString("N"));

    private static string EmployeeDocumentDirectory(IConfiguration configuration, Guid companyId, Guid employeeId) =>
        Path.Combine(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media",
            companyId.ToString("N"), "employees", employeeId.ToString("N"), "documents");

    private static string CleanEmployeeDocumentFileName(string? fileName, string detectedExtension)
    {
        var safe = Path.GetFileNameWithoutExtension(Path.GetFileName((fileName ?? "belge").Replace('\\', '/')));
        var filtered = new string(safe.Where(character => !char.IsControl(character) && character is not '/' and not '\\').ToArray()).Trim();
        if (filtered.Length > 175) filtered = filtered[^175..];
        if (string.IsNullOrWhiteSpace(filtered)) filtered = "belge";
        return $"{filtered}.{detectedExtension}";
    }

    private static bool IsSafeEmployeeDocumentFileName(string fileName) =>
        Regex.IsMatch(fileName, "^[a-fA-F0-9]{32}\\.(pdf|png|jpg|webp)$", RegexOptions.CultureInvariant);

    private static (string ContentType, string Extension)? DetectEmployeeDocument(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8)) return ("application/pdf", "pdf");
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return ("image/png", "png");
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return ("image/jpeg", "jpg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8))
            return ("image/webp", "webp");
        return null;
    }

    private static string Cell(string? value)
    {
        var safe = value?.Trim() ?? "";
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }

    private static string AuditTargetLabel(string value) => value switch
    {
        "Asset" => "Varlık", "Employee" => "Çalışan", "Company" => "Şirket", "Account" => "Hesap",
        "Accessory" => "Aksesuar", "AccessoryKit" => "Aksesuar kiti", "AccessoryPurchaseOrder" => "Aksesuar siparişi", "AssetPurchaseOrder" => "Varlık siparişi",
        "Consumable" => "Sarf malzemesi", "Component" => "Bileşen", "SoftwareLicense" => "Yazılım lisansı",
        "Supplier" => "Tedarikçi", "Location" => "Konum", "Department" => "Departman", "categories" => "Kategori",
        "brands" => "Marka", "models" => "Model", _ => value
    };

    private static string AuditActionLabel(string value) => value switch
    {
        "Created" or "created" => "Oluşturuldu", "Updated" or "updated" => "Güncellendi", "Renamed" => "Adı değiştirildi",
        "Deleted" => "Silindi", "Reactivated" => "Yeniden etkinleştirildi", "Deactivated" => "Pasife alındı",
        "BulkStatusChanged" => "Toplu durum değişikliği", "BulkAssigned" => "Toplu zimmet", "BulkReturned" => "Toplu iade",
        "BulkAudited" => "Toplu denetim", "BulkTransferred" => "Toplu konum transferi", "BulkUpdated" => "Toplu varlık düzenleme", "ProfileUpdated" => "Profil güncellendi",
        "BrandLogoUpdated" => "Şirket logosu güncellendi", "LabelSettingsUpdated" => "Etiket ayarları güncellendi",
        "LabelTemplatesUpdated" => "Yazıcı ve etiket şablonları güncellendi",
        "Invited" => "Kullanıcı davet edildi", "RoleChanged" => "Kullanıcı rolü değiştirildi", "ManagerChanged" => "Yönetici bağlantısı değiştirildi",
        "CsvImported" => "CSV içe aktarıldı", "XlsxImported" => "Excel içe aktarıldı", "AuditScheduled" => "Denetim planlandı", "AuditScheduleCleared" => "Denetim planı kaldırıldı",
        "ProfilePhotoUpdated" => "Profil fotoğrafı güncellendi", "ProfilePhotoRemoved" => "Profil fotoğrafı kaldırıldı",
        "LoginSucceeded" => "Oturum açıldı", "LoginFailed" => "Başarısız giriş denemesi", "Logout" => "Oturum kapatıldı", "PasswordChanged" => "Parola değiştirildi", "PasswordReset" => "Parola kurtarma ile yenilendi", "TwoFactorEnabled" => "İki aşamalı doğrulama açıldı",
        "TwoFactorRecoveryCodesRotated" => "Kurtarma kodları yenilendi", "TwoFactorDisabled" => "İki aşamalı doğrulama kapatıldı",
        "PrivateNotesUpdated" => "Özel notlar güncellendi", "DocumentAdded" => "Belge eklendi", "DocumentRemoved" => "Belge kaldırıldı",
        "AttachmentAdded" => "Ek dosya eklendi", "AttachmentRemoved" => "Ek dosya kaldırıldı", "CatalogCreated" => "Katalog kaydı oluşturuldu",
        "CatalogManagerChanged" => "Konum sorumlusu değiştirildi", "CatalogRenamed" => "Katalog kaydı yeniden adlandırıldı",
        "CatalogArchived" or "archived" => "Arşivlendi", "CatalogRestored" or "restored" => "Geri alındı",
        "asset-order.created" => "Varlık siparişi oluşturuldu", "asset-order.receipt" => "Varlık siparişi teslim alındı", "asset-order.cancelled" => "Varlık siparişi iptal edildi",
        "checked_out" or "bulk_checked_out" => "Aksesuar teslim edildi", "checked_in" or "bulk_checked_in" => "Aksesuar iade alındı",
        "accessory.created" => "Aksesuar oluşturuldu", "accessory.updated" => "Aksesuar güncellendi",
        "accessory-order.created" => "Aksesuar siparişi oluşturuldu", "accessory-order.received" => "Sipariş kalemi teslim alındı",
        "accessory-order.receipt" => "Sipariş teslim alındı", "accessory-order.cancelled" => "Sipariş iptal edildi",
        "consumable.created" => "Sarf malzemesi oluşturuldu", "consumable.updated" => "Sarf malzemesi güncellendi",
        "consumable.stock-adjusted" => "Sarf stoğu düzeltildi", "consumable.checked-out" => "Sarf teslim edildi",
        "component.created" => "Bileşen oluşturuldu", "component.removed" => "Bileşen varlıktan çıkarıldı",
        "supplier.created" => "Tedarikçi oluşturuldu", "supplier.updated" => "Tedarikçi güncellendi",
        "supplier.archived" => "Tedarikçi arşivlendi", "supplier.restored" => "Tedarikçi geri alındı", _ => value
    };

    private static bool ValidateAuditFilters(string? q, string? targetType, string? action, DateTimeOffset? from, DateTimeOffset? to, out IResult? error)
    {
        var errors = new Dictionary<string, string[]>();
        if (q?.Trim().Length > 120) errors["q"] = ["Arama metni 120 karakteri aşamaz."];
        if (targetType?.Trim().Length > 80) errors["targetType"] = ["Kayıt türü 80 karakteri aşamaz."];
        if (action?.Trim().Length > 100) errors["action"] = ["İşlem türü 100 karakteri aşamaz."];
        if (from.HasValue && to.HasValue && from.Value > to.Value) errors["to"] = ["Bitiş tarihi başlangıç tarihinden önce olamaz."];
        error = errors.Count == 0 ? null : Results.ValidationProblem(errors);
        return error is null;
    }

    private static async Task<HashSet<Guid>> GetAllowedEmployeeIdsAsync(AppDbContext db, Guid companyId,
        EmployeeAccessScope employeeAccessScope, CancellationToken cancellationToken) =>
        await EmployeesInScope(db, companyId, employeeAccessScope)
            .Select(employee => employee.Id).ToHashSetAsync(cancellationToken);

    private static IQueryable<AuditEntry> ApplyAuditLocationScope(AppDbContext db, Guid companyId,
        IQueryable<AuditEntry> query, EmployeeAccessScope employeeAccessScope)
    {
        if (!employeeAccessScope.IsRestricted) return query;
        var allowedLocations = employeeAccessScope.AllowedLocations;
        if (allowedLocations is null) return query;

        var allowedAssetIds = db.Assets.AsNoTracking().Where(asset => asset.CompanyId == companyId && asset.LocationCatalogId.HasValue &&
            allowedLocations.Contains(asset.LocationCatalogId.Value)).Select(asset => asset.Id);
        var allowedEmployeeIds = EmployeesInScope(db, companyId, employeeAccessScope).Select(employee => employee.Id);
        var allowedAccessoryIds = db.Accessories.AsNoTracking().Where(item => item.CompanyId == companyId && item.LocationCatalogId.HasValue &&
            allowedLocations.Contains(item.LocationCatalogId.Value)).Select(item => item.Id);
        var allowedConsumableIds = db.Consumables.AsNoTracking().Where(item => item.CompanyId == companyId && item.LocationCatalogId.HasValue &&
            allowedLocations.Contains(item.LocationCatalogId.Value)).Select(item => item.Id);
        var allowedComponentIds = db.InventoryComponents.AsNoTracking().Where(item => item.CompanyId == companyId && item.LocationCatalogId.HasValue &&
            allowedLocations.Contains(item.LocationCatalogId.Value)).Select(item => item.Id);
        var allowedAssetOrders = db.AssetPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId)
            .InLocationScope(db, companyId, allowedLocations).Select(order => order.Id);
        var allowedAccessoryOrders = db.AccessoryPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId)
            .InLocationScope(db, companyId, allowedLocations).Select(order => order.Id);
        var allowedStockOrders = db.StockPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId)
            .InLocationScope(db, companyId, allowedLocations).Select(order => order.Id);
        var allowedContracts = db.AssetContracts.AsNoTracking().Where(contract => contract.CompanyId == companyId)
            .InLocationScope(db, companyId, allowedLocations).Select(contract => contract.Id);
        var allowedLicenses = db.SoftwareLicenses.AsNoTracking().Where(license => license.CompanyId == companyId)
            .LicensesInLocationScope(db, companyId, allowedLocations).Select(license => license.Id);
        var allowedAssignments = db.SoftwareLicenseAssignments.AsNoTracking().Where(assignment => assignment.CompanyId == companyId &&
            (assignment.EmployeeId.HasValue || assignment.AssetId.HasValue) &&
            (!assignment.EmployeeId.HasValue || allowedEmployeeIds.Contains(assignment.EmployeeId.Value)) &&
            (!assignment.AssetId.HasValue || allowedAssetIds.Contains(assignment.AssetId.Value))).Select(assignment => assignment.Id);
        var allowedMaintenances = db.AssetMaintenances.AsNoTracking().Where(maintenance => maintenance.CompanyId == companyId &&
            allowedAssetIds.Contains(maintenance.AssetId)).Select(maintenance => maintenance.Id);
        var allowedTasks = db.CompanyTasks.AsNoTracking().Where(task => task.CompanyId == companyId && task.AssignedEmployeeId.HasValue &&
            allowedEmployeeIds.Contains(task.AssignedEmployeeId.Value)).Select(task => task.Id);
        var allowedMemberships = db.CompanyMemberships.AsNoTracking().Where(membership => membership.CompanyId == companyId &&
            membership.EmployeeId.HasValue && allowedEmployeeIds.Contains(membership.EmployeeId.Value)).Select(membership => membership.UserId);
        var allowedDepartments = db.CompanyDepartments.AsNoTracking().Where(department => department.CompanyId == companyId &&
            department.LocationCatalogId.HasValue && allowedLocations.Contains(department.LocationCatalogId.Value)).Select(department => department.Id);

        return query.Where(entry => entry.TargetId.HasValue &&
            (entry.TargetType == "Asset" && allowedAssetIds.Contains(entry.TargetId.Value) ||
             entry.TargetType == "Employee" && allowedEmployeeIds.Contains(entry.TargetId.Value) ||
             entry.TargetType == "Location" && allowedLocations.Contains(entry.TargetId.Value) ||
             entry.TargetType == "Accessory" && allowedAccessoryIds.Contains(entry.TargetId.Value) ||
             entry.TargetType == "Consumable" && allowedConsumableIds.Contains(entry.TargetId.Value) ||
             (entry.TargetType == "Component" || entry.TargetType == "InventoryComponent") && allowedComponentIds.Contains(entry.TargetId.Value) ||
             entry.TargetType == "AssetReservation" && db.AssetReservations.Any(reservation => reservation.CompanyId == companyId &&
                 reservation.Id == entry.TargetId.Value && allowedAssetIds.Contains(reservation.AssetId) && allowedEmployeeIds.Contains(reservation.EmployeeId)) ||
             entry.TargetType == "AssetPurchaseOrder" && allowedAssetOrders.Contains(entry.TargetId.Value) ||
             entry.TargetType == "AccessoryPurchaseOrder" && allowedAccessoryOrders.Contains(entry.TargetId.Value) ||
             entry.TargetType == "StockPurchaseOrder" && allowedStockOrders.Contains(entry.TargetId.Value) ||
             entry.TargetType == "AssetContract" && allowedContracts.Contains(entry.TargetId.Value) ||
             entry.TargetType == "SoftwareLicense" && allowedLicenses.Contains(entry.TargetId.Value) ||
             entry.TargetType == "SoftwareLicenseAssignment" && allowedAssignments.Contains(entry.TargetId.Value) ||
             entry.TargetType == "AssetMaintenance" && allowedMaintenances.Contains(entry.TargetId.Value) ||
             entry.TargetType == "CompanyTask" && allowedTasks.Contains(entry.TargetId.Value) ||
             entry.TargetType == "CompanyMembership" && allowedMemberships.Contains(entry.TargetId.Value) ||
             entry.TargetType == "Department" && allowedDepartments.Contains(entry.TargetId.Value)));
    }

    private static IQueryable<AuditEntry> FilterAuditEntries(AppDbContext db, Guid companyId, string? q,
        string? targetType, string? action, DateTimeOffset? from, DateTimeOffset? to, HashSet<Guid>? allowedEmployeeIds = null)
    {
        var query = db.AuditEntries.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(targetType)) query = query.Where(x => x.TargetType == targetType.Trim());
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action.Trim());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var matchingActors = db.Users.AsNoTracking().Where(user => user.DisplayName.Contains(term));
            if (allowedEmployeeIds is not null)
                matchingActors = matchingActors.Where(user => !db.CompanyMemberships.Any(membership => membership.CompanyId == companyId &&
                    membership.UserId == user.Id && membership.EmployeeId.HasValue && !allowedEmployeeIds.Contains(membership.EmployeeId.Value)));
            query = query.Where(x => x.Action.Contains(term) ||
                (allowedEmployeeIds == null && x.Details != null && x.Details.Contains(term)) ||
                matchingActors.Any(user => user.Id == x.ActorUserId));
        }
        if (from.HasValue) query = query.Where(x => x.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredAt <= to.Value);
        return query;
    }

    private sealed record SelectedAssetExportRow(string AssetTag, string Name, string Status, string? Category,
        string? Brand, string? Model, string? SerialNumber, string? Location, string? ParentAssetTag, string? EmployeeNumber,
        string? EmployeeName, DateOnly? PurchaseDate, DateOnly? WarrantyExpiresAt, decimal? PurchaseCost, string Currency, string? CustomFieldsJson, bool IsByod);
}
