using EnvanterTR.Web.Data;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class CompanyMediaUsageTests
{
    [Fact]
    public void Measures_only_company_files_including_hidden_files_and_excludes_link_targets()
    {
        var root = Path.Combine(Path.GetTempPath(), "envantertr-media-usage-" + Guid.NewGuid().ToString("N"));
        var companyId = Guid.NewGuid();
        var own = Path.Combine(root, companyId.ToString("N"));
        var foreign = Path.Combine(root, Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(own, "assets"));
            Directory.CreateDirectory(foreign);
            File.WriteAllBytes(Path.Combine(own, "logo.png"), new byte[11]);
            File.WriteAllBytes(Path.Combine(own, "assets", ".hidden"), new byte[7]);
            File.WriteAllBytes(Path.Combine(foreign, "private.pdf"), new byte[500]);
            if (!OperatingSystem.IsWindows()) Directory.CreateSymbolicLink(Path.Combine(own, "outside"), foreign);
            var result = CompanyMediaUsage.Measure(root, companyId, CancellationToken.None);
            Assert.Equal(18, result.Bytes);
            Assert.Equal(2, result.FileCount);
            Assert.InRange(result.MeasuredAt, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow);
            Assert.Equal(0, CompanyMediaUsage.Measure(root, Guid.NewGuid(), CancellationToken.None).Bytes);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }
}

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Company_media_usage_is_settings_authorized_and_tenant_scoped()
    {
        var own = Path.Combine(_mediaPath, _companyId.ToString("N"));
        var foreign = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"));
        Directory.CreateDirectory(Path.Combine(own, "assets"));
        Directory.CreateDirectory(foreign);
        await File.WriteAllBytesAsync(Path.Combine(own, "logo.png"), new byte[11]);
        await File.WriteAllBytesAsync(Path.Combine(own, "assets", "attachment.pdf"), new byte[7]);
        await File.WriteAllBytesAsync(Path.Combine(foreign, "private.pdf"), new byte[500]);
        using var response = await SendAsAdminAsync($"/api/company/storage-usage?companyId={_otherCompanyId}");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(18, payload.RootElement.GetProperty("bytes").GetInt64());
        Assert.Equal(2, payload.RootElement.GetProperty("fileCount").GetInt64());
        Assert.DoesNotContain("private.pdf", await response.Content.ReadAsStringAsync());
        await SetCompanyRoleAsync("viewer");
        using var denied = await SendAsRoleAsync(HttpMethod.Get, "/api/company/storage-usage", "viewer");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, denied.StatusCode);
    }
}
