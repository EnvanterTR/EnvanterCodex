using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static partial class EmployeeHandoverEndpoints
{
    private const int MaxSignatureBytes = 256 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapEmployeeHandoverEndpoints(this RouteGroupBuilder api)
    {
        var read = api.MapGroup("/employees/{employeeId:guid}/handover-documents")
            .RequireAuthorization("asset.read");
        read.MapGet("", ListAsync);
        read.MapGet("/{documentId:guid}", GetAsync);
        read.MapGet("/{documentId:guid}/pdf", DownloadPdfAsync);

        var write = api.MapGroup("/employees/{employeeId:guid}/handover-documents")
            .RequireAuthorization("asset.write");
        write.MapPost("", CreateAsync);

        api.MapPost("/employee-handover-documents/{documentId:guid}/sign", SignAsync)
            .RequireAuthorization("asset.write");
        return api;
    }

    private static async Task<IResult> CreateAsync(Guid employeeId, ClaimsPrincipal user, AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryContext(user, out var companyId, out var userId)) return Results.Forbid();
        var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.Id == employeeId && x.IsActive, cancellationToken);
        if (employee is null || !await CanAccessEmployeeAsync(db, companyId, userId, employee.LocationCatalogId,
                employee.CompanyUnitId, cancellationToken))
            return Results.NotFound();

        var assets = await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId &&
                x.AssignedEmployeeId == employeeId && x.DeletedAt == null)
            .OrderBy(x => x.AssetTag)
            .Select(x => new HandoverAssetSnapshot(x.Id, x.AssetTag, x.Name, x.SerialNumber, x.Status, x.Location,
                x.LocationCatalogId))
            .ToListAsync(cancellationToken);
        if (!await CanAccessLocationsAsync(db, companyId, userId, assets.Select(x => x.LocationCatalogId), cancellationToken))
            return Results.NotFound();

        var accessories = await db.AccessoryAssignments.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.TargetType == "Employee" && x.EmployeeId == employeeId && x.ReturnedQuantity < x.Quantity)
            .Join(db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId), assignment => assignment.AccessoryId,
                accessory => accessory.Id, (assignment, accessory) => new HandoverAccessorySnapshot(
                    assignment.Id, accessory.Id, accessory.Name, accessory.ModelNumber, assignment.Quantity - assignment.ReturnedQuantity,
                    assignment.CheckedOutAt, accessory.LocationCatalogId))
            .ToListAsync(cancellationToken);
        accessories = accessories.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        if (!await CanAccessLocationsAsync(db, companyId, userId, accessories.Select(x => x.LocationCatalogId), cancellationToken))
            return Results.NotFound();

        var eulaAcceptances = await (
            from acceptance in db.SoftwareLicenseEmployeeEulaAcceptances.AsNoTracking()
            join assignment in db.SoftwareLicenseAssignments.AsNoTracking()
                on new { acceptance.CompanyId, acceptance.LicenseId, acceptance.AssignmentId }
                equals new { assignment.CompanyId, assignment.LicenseId, AssignmentId = assignment.Id }
            join license in db.SoftwareLicenses.AsNoTracking()
                on new { acceptance.CompanyId, acceptance.LicenseId }
                equals new { license.CompanyId, LicenseId = license.Id }
            join attachment in db.SoftwareLicenseAttachments.AsNoTracking()
                on new { acceptance.CompanyId, acceptance.LicenseId, acceptance.EulaAttachmentId }
                equals new { attachment.CompanyId, LicenseId = attachment.LicenseId, EulaAttachmentId = attachment.Id }
            where acceptance.CompanyId == companyId && acceptance.EmployeeId == employeeId &&
                assignment.EmployeeId == employeeId && assignment.RevokedAt == null && license.RequiresEulaAcceptance
            orderby license.Name, acceptance.AcceptedAt
            select new HandoverEulaSnapshot(acceptance.Id, acceptance.LicenseId, license.Name, license.ProductVersion,
                acceptance.AssignmentId, acceptance.EulaAttachmentId, attachment.OriginalFileName, attachment.ContentType,
                attachment.Length, acceptance.EulaVersion, acceptance.EulaSha256, acceptance.AcceptedAt,
                acceptance.AcceptedByEmailSnapshot, attachment.IsCurrentEula)
        ).ToListAsync(cancellationToken);

        var snapshot = new HandoverSnapshot(1, companyId, employeeId, employee.DisplayName, employee.EmployeeNumber,
            employee.Department, employee.JobTitle, DateTimeOffset.UtcNow, assets, accessories, eulaAcceptances);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var now = DateTimeOffset.UtcNow;
        var document = new EmployeeHandoverDocument
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            CreatedByUserId = userId,
            DocumentNumber = $"ZMB-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22],
            SnapshotJson = snapshotJson,
            SnapshotSha256 = Sha256(snapshotJson)
        };
        db.EmployeeHandoverDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/employees/{employeeId}/handover-documents/{document.Id}", ToResponse(document));
    }

    private static async Task<IResult> ListAsync(Guid employeeId, ClaimsPrincipal user, AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryContext(user, out var companyId, out var userId)) return Results.Forbid();
        var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == employeeId)
            .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(cancellationToken);
        if (employee is null || !await CanAccessEmployeeAsync(db, companyId, userId, employee.LocationCatalogId,
                employee.CompanyUnitId, cancellationToken))
            return Results.NotFound();
        var documents = await db.EmployeeHandoverDocuments.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.EmployeeId == employeeId)
            .OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(cancellationToken);
        if (!await CanAccessDocumentLocationsAsync(db, companyId, userId, documents, cancellationToken)) return Results.NotFound();
        return Results.Ok(documents.Select(document => ToResponse(document)));
    }

    private static async Task<IResult> GetAsync(Guid employeeId, Guid documentId, ClaimsPrincipal user, AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryContext(user, out var companyId, out var userId)) return Results.Forbid();
        var document = await db.EmployeeHandoverDocuments.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.EmployeeId == employeeId && x.Id == documentId, cancellationToken);
        if (document is null || !await CanAccessDocumentLocationsAsync(db, companyId, userId, [document], cancellationToken))
            return Results.NotFound();
        return Results.Ok(ToResponse(document, includeSignatures: true));
    }

    private static async Task<IResult> SignAsync(Guid documentId, SignHandoverRequest request, ClaimsPrincipal user,
        AppDbContext db, CancellationToken cancellationToken)
    {
        if (!TryContext(user, out var companyId, out var userId)) return Results.Forbid();
        var role = request.SignerType?.Trim().ToLowerInvariant();
        if (role is not ("recipient" or "issuer")) return Results.ValidationProblem(new Dictionary<string, string[]>
            { ["signerType"] = ["İmzalayan recipient veya issuer olmalıdır."] });
        if (!TryReadPng(request.SignaturePngDataUrl, out var signatureBytes))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["signaturePngDataUrl"] = ["İmza geçerli PNG olmalı ve 256 KB sınırını aşmamalıdır."] });

        var document = await db.EmployeeHandoverDocuments.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.Id == documentId, cancellationToken);
        if (document is null || !await CanAccessDocumentLocationsAsync(db, companyId, userId, [document], cancellationToken))
            return Results.NotFound();
        var employee = await db.Employees.AsNoTracking().SingleAsync(x =>
            x.CompanyId == companyId && x.Id == document.EmployeeId, cancellationToken);

        var snapshot = JsonSerializer.Deserialize<HandoverSnapshot>(document.SnapshotJson, JsonOptions);
        if (snapshot is null || !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(document.SnapshotSha256), Convert.FromHexString(Sha256(document.SnapshotJson))))
            return Results.Conflict(new { message = "Belge özeti doğrulanamadı; imza alınmadı." });

        var signerName = role == "recipient" ? request.SignerName?.Trim() :
            await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.DisplayName).SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(signerName) || signerName.Length > 200 ||
            role == "recipient" && !string.Equals(signerName, employee.DisplayName, StringComparison.OrdinalIgnoreCase))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["signerName"] = ["Teslim alan imzasında çalışan adıyla eşleşen ad girilmelidir."] });

        var encoded = Convert.ToBase64String(signatureBytes);
        var signatureHash = Convert.ToHexString(SHA256.HashData(signatureBytes)).ToLowerInvariant();
        var signedAt = DateTimeOffset.UtcNow;
        var query = db.EmployeeHandoverDocuments.Where(x => x.CompanyId == companyId && x.Id == documentId);
        var affected = role == "recipient"
            ? await query.Where(x => x.RecipientSignedAt == null && x.RecipientSignaturePngBase64 == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RecipientName, signerName)
                    .SetProperty(x => x.RecipientSignaturePngBase64, encoded)
                    .SetProperty(x => x.RecipientSignatureSha256, signatureHash)
                    .SetProperty(x => x.RecipientSignedAt, signedAt), cancellationToken)
            : await query.Where(x => x.IssuerSignedAt == null && x.IssuerSignaturePngBase64 == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IssuerName, signerName)
                    .SetProperty(x => x.IssuerSignaturePngBase64, encoded)
                    .SetProperty(x => x.IssuerSignatureSha256, signatureHash)
                    .SetProperty(x => x.IssuerSignedAt, signedAt), cancellationToken);
        CryptographicOperations.ZeroMemory(signatureBytes);
        if (affected != 1)
            return Results.Conflict(new { message = "Bu taraf belgeyi daha önce imzaladı; kayıt değiştirilemez." });
        var updated = await db.EmployeeHandoverDocuments.AsNoTracking().SingleAsync(x =>
            x.CompanyId == companyId && x.Id == documentId, cancellationToken);
        return Results.Ok(ToResponse(updated, includeSignatures: true));
    }

    private static async Task<bool> CanAccessDocumentLocationsAsync(AppDbContext db, Guid companyId, Guid userId,
        IReadOnlyCollection<EmployeeHandoverDocument> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0) return true;
        var employeeIds = documents.Select(x => x.EmployeeId).Distinct().ToArray();
        var employees = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && employeeIds.Contains(x.Id))
            .Select(x => new { x.LocationCatalogId, x.CompanyUnitId }).ToListAsync(cancellationToken);
        foreach (var employee in employees)
            if (!await CanAccessEmployeeAsync(db, companyId, userId, employee.LocationCatalogId,
                    employee.CompanyUnitId, cancellationToken)) return false;
        var allowed = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, userId, cancellationToken);
        if (allowed is null) return true;
        foreach (var document in documents)
        {
            var snapshot = JsonSerializer.Deserialize<HandoverSnapshot>(document.SnapshotJson, JsonOptions);
            if (snapshot is null || snapshot.Assets.Any(x => x.LocationCatalogId is not Guid loc || !allowed.Contains(loc)) ||
                snapshot.Accessories.Any(x => x.LocationCatalogId is not Guid loc || !allowed.Contains(loc))) return false;
        }
        return true;
    }

    private static Task<bool> CanAccessEmployeeAsync(AppDbContext db, Guid companyId, Guid userId, Guid? locationId,
        Guid? companyUnitId, CancellationToken cancellationToken) =>
        CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, userId, locationId, companyUnitId, cancellationToken);

    private static async Task<bool> CanAccessLocationsAsync(AppDbContext db, Guid companyId, Guid userId,
        IEnumerable<Guid?> locations, CancellationToken cancellationToken)
    {
        var allowed = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, userId, cancellationToken);
        return allowed is null || locations.All(x => x is Guid locationId && allowed.Contains(locationId));
    }

    private static bool TryContext(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        var companyValid = Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
        var userValid = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return companyValid && userValid;
    }

    private static bool TryReadPng(string? dataUrl, out byte[] bytes)
    {
        bytes = [];
        const string prefix = "data:image/png;base64,";
        if (dataUrl is null || !dataUrl.StartsWith(prefix, StringComparison.Ordinal) || dataUrl.Length > MaxSignatureBytes * 4 / 3 + prefix.Length + 8)
            return false;
        try
        {
            bytes = Convert.FromBase64String(dataUrl[prefix.Length..]);
            ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
            if (bytes.Length is < 64 or > MaxSignatureBytes || !bytes.AsSpan().StartsWith(signature))
            {
                CryptographicOperations.ZeroMemory(bytes);
                bytes = [];
                return false;
            }
            return true;
        }
        catch (FormatException) { return false; }
    }

    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static object ToResponse(EmployeeHandoverDocument document, bool includeSignatures = false) => new
    {
        document.Id, document.CompanyId, document.EmployeeId, document.DocumentNumber,
        snapshot = includeSignatures ? document.SnapshotJson : null,
        document.SnapshotSha256, document.CreatedAt,
        recipient = new { document.RecipientName, document.RecipientSignedAt, document.RecipientSignatureSha256,
            signaturePngDataUrl = includeSignatures && document.RecipientSignaturePngBase64 is not null
                ? "data:image/png;base64," + document.RecipientSignaturePngBase64 : null },
        issuer = new { document.IssuerName, document.IssuerSignedAt, document.IssuerSignatureSha256,
            signaturePngDataUrl = includeSignatures && document.IssuerSignaturePngBase64 is not null
                ? "data:image/png;base64," + document.IssuerSignaturePngBase64 : null },
        isFullySigned = document.RecipientSignedAt is not null && document.IssuerSignedAt is not null
    };

    public sealed record SignHandoverRequest(string? SignerType, string? SignerName, string? SignaturePngDataUrl);
    private sealed record HandoverSnapshot(int Version, Guid CompanyId, Guid EmployeeId, string EmployeeName,
        string EmployeeNumber, string? Department, string? JobTitle, DateTimeOffset CreatedAt,
        IReadOnlyList<HandoverAssetSnapshot> Assets, IReadOnlyList<HandoverAccessorySnapshot> Accessories,
        IReadOnlyList<HandoverEulaSnapshot> EulaAcceptances);
    private sealed record HandoverAssetSnapshot(Guid Id, string AssetTag, string Name, string? SerialNumber,
        string Status, string? Location, Guid? LocationCatalogId);
    private sealed record HandoverAccessorySnapshot(Guid AssignmentId, Guid AccessoryId, string Name,
        string? ModelNumber, int Quantity, DateTimeOffset CheckedOutAt, Guid? LocationCatalogId);
    private sealed record HandoverEulaSnapshot(Guid AcceptanceId, Guid LicenseId, string LicenseName,
        string? ProductVersion, Guid AssignmentId, Guid AttachmentId, string FileName, string ContentType,
        long Length, string EulaVersion, string Sha256, DateTimeOffset AcceptedAt, string AcceptedByEmail,
        bool IsCurrentEula);
}
