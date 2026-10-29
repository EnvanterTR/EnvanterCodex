using System.Net;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Company_logo_and_employee_photo_reject_foreign_storage_links(bool companyLogo, bool directoryLink)
    {
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var payload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = payload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        byte[] ownBytes = [137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3];
        var route = companyLogo ? "/api/company/branding/logo" : $"/api/employees/{_ownEmployeeId}/photo";
        if (companyLogo)
        {
            using var noCsrfForm = new MultipartFormDataContent();
            noCsrfForm.Add(new ByteArrayContent(ownBytes), "logo", "own.png");
            using var noCsrf = await SendAsUserAsync(HttpMethod.Post, route, _userId, _companyId, noCsrfForm);
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        }
        async Task<HttpResponseMessage> UploadAsync()
        {
            using var multipart = new MultipartFormDataContent();
            multipart.Add(new ByteArrayContent(ownBytes), companyLogo ? "logo" : "file", "own.png");
            return await SendAsUserAsync(HttpMethod.Post, route, _userId, _companyId, multipart, token, cookie);
        }
        using var uploaded = await UploadAsync();
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        string name;
        int auditCount;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            name = companyLogo ? (await db.Companies.SingleAsync(x => x.Id == _companyId)).BrandLogoPath! : "profile.png";
            auditCount = await db.AuditEntries.CountAsync(x => x.CompanyId == _companyId);
        }
        var relative = companyLogo ? "" : Path.Combine("employees", _ownEmployeeId.ToString("N"));
        var ownDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), relative);
        var foreignDirectory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), relative);
        Directory.CreateDirectory(foreignDirectory);
        var ownFile = Path.Combine(ownDirectory, name);
        var foreignFile = Path.Combine(foreignDirectory, name);
        byte[] foreignBytes = [137, 80, 78, 71, 13, 10, 26, 10, 9, 8, 7];
        await File.WriteAllBytesAsync(foreignFile, foreignBytes);
        var link = directoryLink ? ownDirectory.TrimEnd(Path.DirectorySeparatorChar) : ownFile;
        var original = link + ".original";
        if (directoryLink) { Directory.Move(link, original); Directory.CreateSymbolicLink(link, foreignDirectory); }
        else { File.Move(link, original); File.CreateSymbolicLink(link, foreignFile); }
        try
        {
            using (var read = await SendAsAdminAsync(route)) Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
            using (var replace = await UploadAsync()) Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);
            if (!companyLogo)
            {
                using var remove = await SendAsUserAsync(HttpMethod.Delete, route, _userId, _companyId,
                    requestToken: token, antiforgeryCookie: cookie);
                Assert.Equal(HttpStatusCode.NotFound, remove.StatusCode);
            }
            Assert.Equal(foreignBytes, await File.ReadAllBytesAsync(foreignFile));
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(auditCount, await db.AuditEntries.CountAsync(x => x.CompanyId == _companyId));
            if (companyLogo) Assert.Equal(name, (await db.Companies.SingleAsync(x => x.Id == _companyId)).BrandLogoPath);
        }
        finally
        {
            if (directoryLink) { Directory.Delete(link); Directory.Move(original, link); }
            else { File.Delete(link); File.Move(original, link); }
        }
        using var restored = await SendAsAdminAsync(route);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(ownBytes, await restored.Content.ReadAsByteArrayAsync());
        using var normalReplace = await UploadAsync();
        Assert.Equal(HttpStatusCode.OK, normalReplace.StatusCode);
        Assert.Equal(foreignBytes, await File.ReadAllBytesAsync(foreignFile));
    }
}
