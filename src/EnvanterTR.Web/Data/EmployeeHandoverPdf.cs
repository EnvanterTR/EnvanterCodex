using System.Globalization;
using System.Buffers.Binary;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace EnvanterTR.Web.Data;

public static partial class EmployeeHandoverEndpoints
{
    private static readonly Lazy<bool> PdfFontsReady = new(() =>
    {
        GlobalFontSettings.FontResolver ??= new HandoverFontResolver();
        MigraDoc.PredefinedFontsAndChars.ErrorFontName = "EnvanterTR Noto";
        return true;
    });

    private static async Task<IResult> DownloadPdfAsync(Guid employeeId, Guid documentId, ClaimsPrincipal user,
        HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        if (!TryContext(user, out var companyId, out var userId)) return Results.Forbid();
        var record = await db.EmployeeHandoverDocuments.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == companyId && x.EmployeeId == employeeId && x.Id == documentId, cancellationToken);
        if (record is null || !await CanAccessDocumentLocationsAsync(db, companyId, userId, [record], cancellationToken))
            return Results.NotFound();
        if (!TryReadVerifiedSignedSnapshot(record, out var snapshot))
            return Results.Conflict(new { message = "PDF için iki imza tamamlanmalı; belge ve imza özetleri doğrulanmalıdır." });

        var companyName = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
            .Select(x => x.Name).SingleAsync(cancellationToken);
        var bytes = CreateHandoverPdf(record, snapshot!, companyName);
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        var safeNumber = new string(record.DocumentNumber.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-').ToArray());
        return Results.File(bytes, "application/pdf", $"{safeNumber}.pdf");
    }

    private static bool TryReadVerifiedSignedSnapshot(EmployeeHandoverDocument record, out HandoverSnapshot? snapshot)
    {
        snapshot = null;
        if (record.RecipientSignedAt is null || record.IssuerSignedAt is null ||
            string.IsNullOrWhiteSpace(record.RecipientName) || string.IsNullOrWhiteSpace(record.IssuerName)) return false;
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(record.SnapshotJson)),
                    Convert.FromHexString(record.SnapshotSha256))) return false;
            snapshot = JsonSerializer.Deserialize<HandoverSnapshot>(record.SnapshotJson, JsonOptions);
            if (snapshot is null || snapshot.Version != 1 || snapshot.CompanyId != record.CompanyId ||
                snapshot.EmployeeId != record.EmployeeId || snapshot.Assets is null || snapshot.Accessories is null ||
                snapshot.EulaAcceptances is null) return false;
            return VerifySignature(record.RecipientSignaturePngBase64, record.RecipientSignatureSha256) &&
                   VerifySignature(record.IssuerSignaturePngBase64, record.IssuerSignatureSha256);
        }
        catch (Exception exception) when (exception is FormatException or JsonException) { return false; }
    }

    private static bool VerifySignature(string? base64, string? expectedHash)
    {
        if (base64 is null || expectedHash is null || !TryReadPng("data:image/png;base64," + base64, out var bytes)) return false;
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), Convert.FromHexString(expectedHash)) ||
                !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)) return false;
            var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
            var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
            if (width is 0 or > 4096 || height is 0 or > 4096 || (ulong)width * height > 4_000_000) return false;
            using var input = new MemoryStream(bytes, writable: false);
            using var image = PdfSharp.Drawing.XImage.FromStream(input);
            return image.PixelWidth == width && image.PixelHeight == height;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or NotSupportedException)
        { return false; }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private static byte[] CreateHandoverPdf(EmployeeHandoverDocument record, HandoverSnapshot snapshot, string companyName)
    {
        _ = PdfFontsReady.Value;
        var document = new Document();
        document.Info.Title = $"{record.DocumentNumber} - Zimmet teslim belgesi";
        document.Info.Author = "EnvanterTR";
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = "EnvanterTR Noto";
        normal.Font.Size = 9;
        normal.Font.Color = Color.FromRgb(24, 43, 38);
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        foreach (var name in new[] { StyleNames.Heading1, StyleNames.Heading2 })
        {
            var style = document.Styles[name]!;
            style.Font.Name = "EnvanterTR Noto";
            style.Font.Bold = true;
            style.Font.Color = Color.FromRgb(30, 90, 63);
            style.Font.Size = name == StyleNames.Heading1 ? 21 : 12;
            style.ParagraphFormat.SpaceBefore = Unit.FromPoint(12);
            style.ParagraphFormat.SpaceAfter = Unit.FromPoint(8);
            style.ParagraphFormat.KeepWithNext = true;
        }
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = section.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(2);
        section.PageSetup.FooterDistance = Unit.FromCentimeter(0.8);
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Font.Size = 8;
        footer.AddText($"EnvanterTR | {record.DocumentNumber} | Sayfa ");
        footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField();
        section.AddParagraph("Zimmet teslim belgesi", StyleNames.Heading1);
        section.AddParagraph(companyName).Format.Font.Bold = true;
        section.AddParagraph($"Belge: {record.DocumentNumber} | Oluşturulma: {PdfDate(record.CreatedAt)}");
        section.AddParagraph($"Çalışan: {snapshot.EmployeeName} | Personel no: {snapshot.EmployeeNumber}");
        section.AddParagraph($"Birim: {snapshot.Department ?? "-"} | Görev: {snapshot.JobTitle ?? "-"}");

        section.AddParagraph($"Varlıklar ({snapshot.Assets.Count})", StyleNames.Heading2);
        var assets = PdfTable(section, [3.3, 5.8, 3.5, 4.4], ["Etiket", "Varlık", "Seri numarası", "Durum / konum"]);
        foreach (var asset in snapshot.Assets)
            PdfRow(assets, [asset.AssetTag, asset.Name, asset.SerialNumber ?? "-", $"{PdfAssetStatus(asset.Status)}\n{asset.Location ?? "-"}"]);
        if (snapshot.Assets.Count == 0) PdfRow(assets, ["-", "Bu belgede varlık yok.", "-", "-"]);

        section.AddParagraph($"Aksesuarlar ({snapshot.Accessories.Count})", StyleNames.Heading2);
        var accessories = PdfTable(section, [8, 5, 4], ["Aksesuar", "Model", "Adet"]);
        foreach (var accessory in snapshot.Accessories)
            PdfRow(accessories, [accessory.Name, accessory.ModelNumber ?? "-", accessory.Quantity.ToString(CultureInfo.InvariantCulture)]);
        if (snapshot.Accessories.Count == 0) PdfRow(accessories, ["Bu belgede aksesuar yok.", "-", "-"]);

        section.AddParagraph($"Lisans ve EULA kabul kayıtları ({snapshot.EulaAcceptances.Count})", StyleNames.Heading2);
        foreach (var eula in snapshot.EulaAcceptances)
        {
            section.AddParagraph($"{eula.LicenseName} {eula.ProductVersion} | EULA sürümü: {eula.EulaVersion}").Format.Font.Bold = true;
            section.AddParagraph($"Dosya: {eula.FileName} | Kabul: {eula.AcceptedByEmail} | {PdfDate(eula.AcceptedAt)}");
            section.AddParagraph($"EULA SHA-256: {PdfHash(eula.Sha256)}").Format.Font.Size = 8;
        }
        if (snapshot.EulaAcceptances.Count == 0) section.AddParagraph("Bu belgede lisans/EULA kabul kaydı yok.");

        section.AddParagraph("Teslim ve imzalar", StyleNames.Heading2);
        var signatures = PdfTable(section, [8.5, 8.5], ["Teslim alan", "Teslim eden yetkili"]);
        var signatureRow = signatures.AddRow();
        signatureRow.KeepWith = 0;
        foreach (var (cell, name, at, png, hash) in new[]
        {
            (signatureRow.Cells[0], record.RecipientName!, record.RecipientSignedAt!.Value, record.RecipientSignaturePngBase64!, record.RecipientSignatureSha256!),
            (signatureRow.Cells[1], record.IssuerName!, record.IssuerSignedAt!.Value, record.IssuerSignaturePngBase64!, record.IssuerSignatureSha256!)
        })
        {
            cell.AddParagraph(name).Format.Font.Bold = true;
            cell.AddParagraph(PdfDate(at));
            var image = cell.AddParagraph().AddImage("base64:" + png);
            image.Width = Unit.FromCentimeter(6.5);
            image.Height = Unit.FromCentimeter(2.2);
            cell.AddParagraph("İmza SHA-256: " + PdfHash(hash)).Format.Font.Size = 7;
        }
        section.AddParagraph("Belge bütünlüğü", StyleNames.Heading2);
        section.AddParagraph("İçerik SHA-256: " + PdfHash(record.SnapshotSha256)).Format.Font.Size = 8;
        section.AddParagraph($"Şirket kimliği: {record.CompanyId} | Çalışan kimliği: {record.EmployeeId}").Format.Font.Size = 7;
        section.AddParagraph("Bu çıktı, kayıt anındaki sabit zimmet ve imza verilerinden üretilmiştir. Görsel imzalar nitelikli elektronik imza veya bağımsız kimlik doğrulaması değildir.").Format.Font.Size = 8;
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var output = new MemoryStream();
        renderer.PdfDocument.Save(output, closeStream: false);
        renderer.PdfDocument.Dispose();
        return output.ToArray();
    }

    private static Table PdfTable(Section section, double[] widths, string[] headings)
    {
        var table = section.AddTable();
        table.Borders.Color = Color.FromRgb(207, 220, 211);
        table.Borders.Width = Unit.FromPoint(0.5);
        table.TopPadding = table.BottomPadding = Unit.FromPoint(5);
        foreach (var width in widths) table.AddColumn(Unit.FromCentimeter(width));
        var row = table.AddRow();
        row.HeadingFormat = true;
        row.Format.Font.Bold = true;
        row.Shading.Color = Color.FromRgb(235, 244, 237);
        for (var i = 0; i < headings.Length; i++) row.Cells[i].AddParagraph(headings[i]);
        return table;
    }

    private static void PdfRow(Table table, string[] values)
    {
        var row = table.AddRow();
        for (var i = 0; i < values.Length; i++) row.Cells[i].AddParagraph(values[i]);
    }

    private static string PdfDate(DateTimeOffset value) => value.UtcDateTime.ToString("dd.MM.yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);
    private static string PdfAssetStatus(string value) => value switch
    {
        "Available" => "Kullanılabilir", "Deployed" => "Atanmış", "Pending" => "Beklemede",
        "Maintenance" => "Bakımda", "Unusable" => "Kullanılamaz", "Archived" => "Arşiv", _ => value
    };
    private static string PdfHash(string value) => string.Join(" ", Enumerable.Range(0, (value.Length + 15) / 16)
        .Select(i => value.Substring(i * 16, Math.Min(16, value.Length - i * 16))));

    private sealed class HandoverFontResolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
            familyName == "EnvanterTR Noto" ? new FontResolverInfo(bold ? "NotoSans-Bold" : "NotoSans-Regular") : null;

        public byte[]? GetFont(string faceName)
        {
            if (faceName is not ("NotoSans-Regular" or "NotoSans-Bold")) return null;
            using var stream = typeof(EmployeeHandoverEndpoints).Assembly.GetManifestResourceStream(
                $"EnvanterTR.Web.Resources.Fonts.{faceName}.ttf") ?? throw new InvalidOperationException("PDF yazı tipi bulunamadı.");
            using var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }
    }
}
