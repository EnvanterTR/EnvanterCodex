using System.Net;
using System.Text;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Asset_csv_import_cannot_assign_an_employee_outside_the_direct_company_unit_scope()
    {
        var allowedUnitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var allowedLocationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");
        var employeeNumber = $"ASSET-CSV-SCOPE-EMP-{suffix}".ToUpperInvariant();
        var employeeName = $"Asset CSV scope employee {suffix}";
        var locationName = $"Asset CSV scope location {suffix}";
        string categoryName;

        await using (var fixture = _factory!.Services.CreateAsyncScope())
        {
            var db = fixture.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = allowedUnitId, CompanyId = _companyId, Name = $"Asset CSV allowed unit {suffix}", NormalizedName = $"ASSET CSV ALLOWED UNIT {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Asset CSV outside unit {suffix}", NormalizedName = $"ASSET CSV OUTSIDE UNIT {suffix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = allowedLocationId, CompanyId = _companyId,
                CompanyUnitId = allowedUnitId, Name = locationName, NormalizedName = locationName.ToUpperInvariant() });
            db.Employees.Add(new Employee { Id = employeeId, CompanyId = _companyId, EmployeeNumber = employeeNumber,
                DisplayName = employeeName, Location = locationName, LocationCatalogId = allowedLocationId, CompanyUnitId = outsideUnitId });
            var asset = await db.Assets.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            categoryName = await db.AssetCategories.AsNoTracking().Where(x => x.CompanyId == _companyId && x.Id == asset.CategoryId)
                .Select(x => x.Name).SingleAsync();
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.RemoveRange(await db.CompanyMembershipCompanyUnitAccesses
                .Where(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId).ToListAsync());
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = allowedUnitId });
            await db.SaveChangesAsync();
        }

        using var csrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        const string header = "Etiket;Ad;Durum;Kategori;Marka;Model;Seri No;Konum;Personel No;Çalışan;Alış Tarihi;Alış Bedeli;Para Birimi;BYOD";
        var assetTag = $"ASSET-CSV-SCOPE-{suffix}".ToUpperInvariant();
        var csv = header + "\r\n" + string.Join(';', assetTag, "Scoped import assignment", AssetStatuses.Deployed,
            categoryName, "", "", "", locationName, employeeNumber, employeeName, "", "", "TRY", "false");
        StringContent Content() => new(csv, Encoding.UTF8, "text/csv");

        using (var deniedPreview = await SendAsUserAsync(HttpMethod.Post, "/api/assets/import/preview.csv", _sameCompanyOtherUserId,
                   _companyId, Content(), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, deniedPreview.StatusCode);
            using var payload = JsonDocument.Parse(await deniedPreview.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Contains(payload.RootElement.GetProperty("errors").EnumerateArray(), error =>
                error.GetString()!.Contains("çalışan numarası", StringComparison.OrdinalIgnoreCase));
        }
        using (var deniedImport = await SendAsUserAsync(HttpMethod.Post, "/api/assets/import.csv", _sameCompanyOtherUserId,
                   _companyId, Content(), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.UnprocessableEntity, deniedImport.StatusCode);
        await using (var afterRejectedImport = _factory!.Services.CreateAsyncScope())
        {
            var db = afterRejectedImport.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Assets.AnyAsync(x => x.CompanyId == _companyId && x.AssetTag == assetTag));
        }

        await using (var grantEmployeeUnit = _factory!.Services.CreateAsyncScope())
        {
            var db = grantEmployeeUnit.ServiceProvider.GetRequiredService<AppDbContext>();
            var employee = await db.Employees.SingleAsync(x => x.CompanyId == _companyId && x.Id == employeeId);
            employee.CompanyUnitId = allowedUnitId;
            await db.SaveChangesAsync();
        }
        using (var allowedPreview = await SendAsUserAsync(HttpMethod.Post, "/api/assets/import/preview.csv", _sameCompanyOtherUserId,
                   _companyId, Content(), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, allowedPreview.StatusCode);
            using var payload = JsonDocument.Parse(await allowedPreview.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean());
        }
        using (var imported = await SendAsUserAsync(HttpMethod.Post, "/api/assets/import.csv", _sameCompanyOtherUserId,
                   _companyId, Content(), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
            using var payload = JsonDocument.Parse(await imported.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("imported").GetInt32());
        }
        await using var verify = _factory!.Services.CreateAsyncScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var importedAsset = await verifyDb.Assets.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.AssetTag == assetTag);
        Assert.Equal(employeeId, importedAsset.AssignedEmployeeId);
    }

    [Fact]
    public async Task Employee_csv_import_cannot_link_a_manager_outside_the_direct_company_unit_scope()
    {
        var allowedUnitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var allowedLocationId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");
        var managerNumber = $"CSV-SCOPE-MANAGER-{suffix}".ToUpperInvariant();
        var employeeNumber = $"CSV-SCOPE-EMPLOYEE-{suffix}".ToUpperInvariant();
        var managerName = $"CSV-SCOPE-OUTSIDE-MANAGER-{suffix}";
        var locationName = $"CSV Scope Location {suffix}";

        await using (var fixture = _factory!.Services.CreateAsyncScope())
        {
            var db = fixture.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = allowedUnitId, CompanyId = _companyId, Name = $"CSV allowed unit {suffix}", NormalizedName = $"CSV ALLOWED UNIT {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"CSV outside unit {suffix}", NormalizedName = $"CSV OUTSIDE UNIT {suffix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = allowedLocationId, CompanyId = _companyId,
                CompanyUnitId = allowedUnitId, Name = locationName, NormalizedName = locationName.ToUpperInvariant() });
            db.Employees.Add(new Employee { Id = managerId, CompanyId = _companyId, EmployeeNumber = managerNumber,
                DisplayName = managerName, Location = locationName, LocationCatalogId = allowedLocationId, CompanyUnitId = outsideUnitId });
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            var priorAccess = await db.CompanyMembershipCompanyUnitAccesses
                .Where(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId).ToListAsync();
            db.CompanyMembershipCompanyUnitAccesses.RemoveRange(priorAccess);
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = allowedUnitId });
            await db.SaveChangesAsync();
        }

        using var csrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        const string header = "Personel No,Ad Soyad,E-posta,Departman,Görev Unvanı,Telefon,Konum,İşe Başlama Tarihi,Yönetici,Durum,Kayıt Tarihi,Yönetici Personel No\n";
        var csv = header + $"{employeeNumber},Scoped Import Employee,scoped-import-{suffix}@example.invalid,IT,Support,,{locationName},2026-01-01,{managerName},Etkin,,{managerNumber}\n";
        StringContent Content() => new(csv, Encoding.UTF8, "text/csv");

        using (var denied = await SendAsUserAsync(HttpMethod.Post, "/api/employees/import/preview.csv", _sameCompanyOtherUserId,
                   _companyId, Content(), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, denied.StatusCode);
            using var payload = JsonDocument.Parse(await denied.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Contains(payload.RootElement.GetProperty("errors").EnumerateArray(),
                error => error.GetString()?.Contains(managerNumber, StringComparison.Ordinal) == true);
        }
        await using (var afterRejectedPreview = _factory!.Services.CreateAsyncScope())
        {
            var db = afterRejectedPreview.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Employees.AnyAsync(x => x.CompanyId == _companyId && x.EmployeeNumber == employeeNumber));
        }

        await using (var grantManagerUnit = _factory!.Services.CreateAsyncScope())
        {
            var db = grantManagerUnit.ServiceProvider.GetRequiredService<AppDbContext>();
            var manager = await db.Employees.SingleAsync(x => x.CompanyId == _companyId && x.Id == managerId);
            manager.CompanyUnitId = allowedUnitId;
            await db.SaveChangesAsync();
        }
        using (var accepted = await SendAsUserAsync(HttpMethod.Post, "/api/employees/import/preview.csv", _sameCompanyOtherUserId,
                   _companyId, Content(), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            using var payload = JsonDocument.Parse(await accepted.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean());
        }
        using (var commit = await SendAsUserAsync(HttpMethod.Post, "/api/employees/import.csv", _sameCompanyOtherUserId,
                   _companyId, Content(), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, commit.StatusCode);
            using var payload = JsonDocument.Parse(await commit.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("imported").GetInt32());
        }
        await using var verify = _factory!.Services.CreateAsyncScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var imported = await verifyDb.Employees.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.EmployeeNumber == employeeNumber);
        Assert.Equal(managerId, imported.ManagerEmployeeId);
    }

    [Fact]
    public async Task Explicit_location_restriction_blocks_locationless_unit_employees_and_unit_scope_blocks_outsiders()
    {
        var allowedUnitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var allowedLocationId = Guid.NewGuid();
        var allowedLocationlessEmployeeId = Guid.NewGuid();
        var outsideLocationlessEmployeeId = Guid.NewGuid();
        var locatedLegacyEmployeeId = Guid.NewGuid();
        var outsideUnitLocatedEmployeeId = Guid.NewGuid();
        var prefix = $"EMPLOYEE-SCOPE-{Guid.NewGuid():N}";

        await using (var fixtureScope = _factory!.Services.CreateAsyncScope())
        {
            var db = fixtureScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = allowedUnitId, CompanyId = _companyId, Name = $"Scope unit {prefix}", NormalizedName = $"SCOPE UNIT {prefix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Outside unit {prefix}", NormalizedName = $"OUTSIDE UNIT {prefix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = allowedLocationId, CompanyId = _companyId,
                CompanyUnitId = allowedUnitId, Name = $"Scope location {prefix}", NormalizedName = $"SCOPE LOCATION {prefix}" });
            db.Employees.AddRange(
                new Employee { Id = allowedLocationlessEmployeeId, CompanyId = _companyId, CompanyUnitId = allowedUnitId,
                    EmployeeNumber = $"{prefix}-UNIT-IN", DisplayName = $"{prefix} unit in" },
                new Employee { Id = outsideLocationlessEmployeeId, CompanyId = _companyId, CompanyUnitId = outsideUnitId,
                    EmployeeNumber = $"{prefix}-UNIT-OUT", DisplayName = $"{prefix} unit out" },
                new Employee { Id = locatedLegacyEmployeeId, CompanyId = _companyId, LocationCatalogId = allowedLocationId,
                    Location = $"Scope location {prefix}", EmployeeNumber = $"{prefix}-LOCATION-LEGACY", DisplayName = $"{prefix} location legacy" },
                new Employee { Id = outsideUnitLocatedEmployeeId, CompanyId = _companyId, LocationCatalogId = allowedLocationId,
                    Location = $"Scope location {prefix}", CompanyUnitId = outsideUnitId,
                    EmployeeNumber = $"{prefix}-UNIT-MISMATCH", DisplayName = $"{prefix} unit mismatch" });
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = allowedUnitId });
            await db.SaveChangesAsync();
        }

        using (var list = await SendAsUserAsync(HttpMethod.Get, $"/api/employees?q={Uri.EscapeDataString(prefix)}&pageSize=100",
                   _sameCompanyOtherUserId, _companyId))
        using (var payload = JsonDocument.Parse(await list.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == allowedLocationlessEmployeeId);
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == locatedLegacyEmployeeId);
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == outsideLocationlessEmployeeId);
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == outsideUnitLocatedEmployeeId);
        }

        await using (var fixtureScope = _factory!.Services.CreateAsyncScope())
        {
            var db = fixtureScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.LocationAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipLocationAccesses.Add(new CompanyMembershipLocationAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, LocationId = allowedLocationId });
            await db.SaveChangesAsync();
            var canReadLocationlessUnitEmployee = await CompanyLocationAccess.CanAccessEmployeeAsync(db, _companyId,
                _sameCompanyOtherUserId, null, allowedUnitId, CancellationToken.None);
            Assert.False(canReadLocationlessUnitEmployee);
        }

        using (var restrictedList = await SendAsUserAsync(HttpMethod.Get, $"/api/employees?q={Uri.EscapeDataString(prefix)}&pageSize=100",
                   _sameCompanyOtherUserId, _companyId))
        using (var payload = JsonDocument.Parse(await restrictedList.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, restrictedList.StatusCode);
            var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == locatedLegacyEmployeeId);
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == allowedLocationlessEmployeeId);
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == outsideLocationlessEmployeeId);
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == outsideUnitLocatedEmployeeId);
        }

        using (var hiddenDetail = await SendAsUserAsync(HttpMethod.Get, $"/api/employees/{allowedLocationlessEmployeeId}",
                   _sameCompanyOtherUserId, _companyId))
            Assert.Equal(HttpStatusCode.NotFound, hiddenDetail.StatusCode);
    }
}
