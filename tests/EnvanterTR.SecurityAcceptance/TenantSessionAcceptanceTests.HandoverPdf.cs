using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharp.Pdf.IO;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    private const string HandoverPdfTestSignaturePng = "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cglYghVQZj69CsPfh34oedDK7QhW6P+4F2DU6jBJwDBNq/AHlAoTsMrekEA9tZsgjGnQ/75Uhf0n92uwqtCIAGOTgzENBlOVbJD7pwgBAKpmct4rAABQNQIWAEBgAhYAQGACFgBAYAIWAEBgAhYAQGACFgBAYAIWAEBgAhYAQGACFgBAYAIWAEBgAhYAQGACFgBAYAIWAEBgAhYAQGACFgBAYAIWAEBgAhYAQGACFgBAYAIWAEBgAhYAQGACFgBAYAIWAECVAtaXX34ZrV69Olq2bFm0atWq6MiRI9GBAweiWbNmRSdOnGg879prr42++uqraOrUqelzm5199tnpz3fffTdaunRp+m/KlCmN29u2bWs8p9XrEqeffnrj+cm/559/PvPyXn311ejqq6+OlixZEq1duzbdhmadltPqfdu1S6fldNqO5m39+OOPo3Xr1jWev3///uj666+P3n777cxt16rdkm1P2mDXrl0d16OTTn2beP3116NrrrkmmjdvXvrzjTfe6Nq+zV577bXo1FNPjb777rv/e+9+tqPV8lrVWbNW/Trxda+88kraJ/Pnz49uuumm6JtvvsncRq3Wq5f9Ylh9C1BLcY7mzJkTHzlyJL29bdu2eP369ent22+/PX7zzTfT2x988EHj8bPOOiteuHBhvHPnzsYykscmmvhYt+e0+n2W5W3fvj1etmxZ/Mcff6T3P/zww3j58uU9Lydru3R7XZbH165dG+/atSu9vWrVqvjzzz/vaR3btdu+ffviWbNmdVyPTjr17UcffRQvWLAg/vnnn9P7yc/k/o4dOzqua7Obb745fuSRR+KtW7e2fO9et6PV8rptd7t+ba6nNWvWxEePHk3vb968OV69evW45Xer/6zbOcq+BaijXAPWxRdfHB86dCi9nUwqn376aXr7wIED8ZVXXhn/888/6US6f//+xuD+ySefxIsXLy5EwEomv4kBZcOGDY0Jsp/16tQu3V6X5fGvv/46vuGGG9IJ/t577+15Hdu124kTJ+Jzzz2343p00qlvV6xYEX/22Wfjnr9nz5545cqVHdd1zO+//54uI9n2devWtXzvXraj3fK6bXe7fm2up7179zae/9tvv8W33HJLfPz48Uz138t2jrJvAeoo11OEzzzzTLRo0aLonnvuiXbv3p3eTsycOTOaPXt2dP/990eXXnppdMUVVzRes3z58vTnzp07o7wlp9iuuuqqcY8lp3hOPvnkobRLCDNmzIjmzp0bPfzww9HmzZuDLXf79u2NvulXu749ePDg/7VzctoqOZ2cRXJqdM2aNem2Hz58ODp69OhA29HL8nrp16Sekrofc+aZZ0bvvfdedNJJJ2Wq/37XaxR9C1A7eSe8n376KT2dMXv27Hjjxo2Nx5O/widPnpz+HDP213NyimTRokXjHms28bGpU6fGS5YsGfcveazd7yceLWn3l/5FF10U//XXX5m3tdt6Nb9vu3Zpt82tljfWdhOff/fdd8eXXXZZfPjw4a7r2On+2PvNnz8/PcLx7bffdt2uJ554In3snXfeabncVn07ffr0+M8//xz3/OS07CWXXJKpXe6666709NzcuXPTPktOxbVqt6zb0W55WY7utOrXsdddeOGFjXp67rnn0vecMWNGpjbKsp3D7lsA/pVbwPr+++/T0zzN95MJJusEsHTp0vR0SZ6nCJNTNV988cW4Uyl33nlnz8sZtF16eXz37t3p6aNk8k2u1+m2jHPOOadxiurYsWPxeeed1/K5W7ZsiTdt2tRxPTrp1LfJqcDmNhnbjubrk9q9b7Lu8+bNa9xPrud66KGH2r6u23Z0Wl6n7e7Ur2OvS66vaj5FmFxrdtppp2Vqo163c5R9C1BHuZ0inDRpUrR+/frGJ6l+/PHH9HRgVk8//XS0cePGKE8PPPBA9OSTT0Z///13ev+tt95q3M6rXTo5fvx4emrw2WefTT/Flnyq7P333+/4muuuuy7asWNH4xRUcr+VZHl79+4Nsp4T+/bRRx+NHnvssejXX39N7//yyy/R448/nj7WzZ49e6I5c+Y07ien5ZJTXu10245el9dLv953333RU089FR07diy9//LLL487PdipjfpZrzz6FqAupuT1xueff356vdKtt96afvw8mUi2bt2a+fWLFy+OTjnllIEDTSK5ViX5yPmY5KsANm3a1PV1t912W3To0KH0awOmTZsWXXDBBemkOOj7DtIunbz44ovRypUro8svvzy9/8ILL0Q33nhj+tgZZ5zR8jUvvfRStGHDhkZ7JF9L0Upy3c++ffvSr9fotz3b9W3y1QbJ1xUkX1GQfAVBsvwHH3wwWrFiRddlJQGy+fqh5GsGkn5KrutKrvXrdTuSx9otL/ndwoULG79bsGBBtGXLlsz1fscdd6TLSa7Dmj59eno/CcFZ2qjX7cyrbwHqYlJyGCvvlQAAqBLf5A4AEJiABQAQmIAFABCYgAUAEJiABQAQmIAFABCYgAUAEJiABQAQmIAFABCYgAUAEJiABQAQmIAFABCYgAUAEJiABQAQmIAFABCYgAUAEJiABQAQmIAFABCYgAUAEJiABQAQmIAFABCYgAUAEJiABQAQmIAFABCF9V+rHPfprGNk1QAAAABJRU5ErkJggg==";
    [Fact]
    public async Task Handover_pdf_is_signed_scoped_integrity_checked_and_paginated()
    {
        var employeeId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.AssetCategories.Where(x => x.CompanyId == _companyId).Select(x => x.Id).FirstAsync();
            if (!await db.CompanyMemberships.AnyAsync(x => x.CompanyId == _otherCompanyId && x.UserId == _userId))
                db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin" });
            db.Employees.Add(new Employee { Id = employeeId, CompanyId = _companyId, EmployeeNumber = "PDF-" + suffix,
                DisplayName = "Örnek Çalışan - İmza kabul testi", Department = "Bilgi İşlem", IsActive = true });
            for (var i = 0; i < 85; i++) db.Assets.Add(new Asset
            {
                CompanyId = _companyId, AssetTag = $"PDF-{suffix}-{i:D3}",
                Name = $"Türkçe içerik {i:D3}: Çç Ğğ İı Öö Şş Üü - uzun varlık adı ve ekipman açıklaması",
                SerialNumber = $"SERIAL-{i:D3}", CategoryId = categoryId, Status = "Deployed",
                AssignedEmployeeId = employeeId, Location = "Merkez / Bilgi İşlem"
            });
            await db.SaveChangesAsync();
        }
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        using var csrfBody = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfBody.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(x => x.Split(';', 2)[0])
            .First(x => x.Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using var created = await SendAsRoleAsync(HttpMethod.Post, $"/api/employees/{employeeId}/handover-documents",
            "admin", JsonContent.Create(new { }), token, cookie);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetGuid();
        var url = $"/api/employees/{employeeId}/handover-documents/{id}/pdf";
        using (var unsigned = await SendAsRoleAsync(HttpMethod.Get, url, "admin"))
            Assert.Equal(HttpStatusCode.Conflict, unsigned.StatusCode);
        // Synthetic fixture only: no real user's signature or credential is used.
        const string png = HandoverPdfTestSignaturePng;
        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var record = await verify.EmployeeHandoverDocuments.SingleAsync(x => x.CompanyId == _companyId && x.Id == id);
        record.RecipientName = "Örnek Çalışan - İmza kabul testi";
        record.IssuerName = "Test Yetkilisi";
        record.RecipientSignedAt = record.IssuerSignedAt = DateTimeOffset.UtcNow;
        record.RecipientSignaturePngBase64 = record.IssuerSignaturePngBase64 = png;
        record.RecipientSignatureSha256 = record.IssuerSignatureSha256 =
            Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(png))).ToLowerInvariant();
        await verify.SaveChangesAsync();
        using (var foreign = await SendAsUserAsync(HttpMethod.Get, url, _userId, _otherCompanyId))
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using (var wrongEmployee = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/employees/{_ownSecondEmployeeId}/handover-documents/{id}/pdf", "admin"))
            Assert.Equal(HttpStatusCode.NotFound, wrongEmployee.StatusCode);
        using (var anonymousClient = _factory.CreateClient())
        using (var anonymous = await anonymousClient.GetAsync(url))
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using (var pdf = await SendAsRoleAsync(HttpMethod.Get, url, "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
            Assert.True(pdf.Headers.CacheControl?.NoStore);
            Assert.Equal("attachment", pdf.Content.Headers.ContentDisposition?.DispositionType);
            var bytes = await pdf.Content.ReadAsByteArrayAsync();
            using var input = new MemoryStream(bytes);
            using var parsed = PdfReader.Open(input, PdfDocumentOpenMode.Import);
            Assert.True(parsed.PageCount >= 3, $"85 long asset rows must paginate, got {parsed.PageCount}.");
            var output = Environment.GetEnvironmentVariable("ENVANTERTR_HANDOVER_PDF_EVIDENCE");
            if (!string.IsNullOrWhiteSpace(output)) await File.WriteAllBytesAsync(output, bytes);
        }
        var membership = await verify.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
        membership.LocationAccessMode = CompanyLocationAccess.Restricted;
        await verify.SaveChangesAsync();
        using (var outsideLocationScope = await SendAsRoleAsync(HttpMethod.Get, url, "admin"))
            Assert.Equal(HttpStatusCode.NotFound, outsideLocationScope.StatusCode);
        membership.LocationAccessMode = CompanyLocationAccess.All;
        await verify.SaveChangesAsync();
        var json = record.SnapshotJson;
        record.SnapshotJson = json.Replace("SERIAL-000", "TAMPER-000", StringComparison.Ordinal);
        await verify.SaveChangesAsync();
        using (var tampered = await SendAsRoleAsync(HttpMethod.Get, url, "admin"))
            Assert.Equal(HttpStatusCode.Conflict, tampered.StatusCode);
        record.SnapshotJson = json;
        var signatureHash = record.RecipientSignatureSha256;
        record.RecipientSignatureSha256 = new string('0', 64);
        await verify.SaveChangesAsync();
        using (var badSignature = await SendAsRoleAsync(HttpMethod.Get, url, "admin"))
            Assert.Equal(HttpStatusCode.Conflict, badSignature.StatusCode);
        record.RecipientSignatureSha256 = signatureHash;
        var oversizedPng = Convert.FromBase64String(png);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(oversizedPng.AsSpan(16, 4), 100_000);
        record.RecipientSignaturePngBase64 = Convert.ToBase64String(oversizedPng);
        record.RecipientSignatureSha256 = Convert.ToHexString(SHA256.HashData(oversizedPng));
        await verify.SaveChangesAsync();
        using (var oversizedSignature = await SendAsRoleAsync(HttpMethod.Get, url, "admin"))
            Assert.Equal(HttpStatusCode.Conflict, oversizedSignature.StatusCode);
        record.RecipientSignaturePngBase64 = png;
        record.RecipientSignatureSha256 = signatureHash;
        record.SnapshotSha256 = "invalid-hex";
        await verify.SaveChangesAsync();
        using (var corruptHash = await SendAsRoleAsync(HttpMethod.Get, url, "admin"))
            Assert.Equal(HttpStatusCode.Conflict, corruptHash.StatusCode);
    }
}
