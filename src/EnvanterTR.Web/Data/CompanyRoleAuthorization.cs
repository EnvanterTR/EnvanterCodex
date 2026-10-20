using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record CompanyRoleDefinitionDto(string RoleKey, string Name, string[] PermissionKeys,
    bool BuiltIn, bool IsActive);

public static class CompanyRoleAuthorization
{
    public const string AssetsRead = "assets.read";
    public const string AssetsWrite = "assets.write";
    public const string PeopleRead = "people.read";
    public const string PeopleManage = "people.manage";
    public const string EmployeeDocuments = "people.documents";
    public const string CompanySettings = "company.settings";

    public static readonly IReadOnlyDictionary<string, string> PermissionNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [AssetsRead] = "Varlıkları görüntüleme",
            [AssetsWrite] = "Varlık ve stok kayıtlarını değiştirme",
            [PeopleRead] = "Çalışan kayıtlarını görüntüleme",
            [PeopleManage] = "Çalışan kayıtlarını yönetme",
            [EmployeeDocuments] = "Çalışan belgelerine erişme",
            [CompanySettings] = "Şirket kullanıcılarını, ayarlarını ve denetim kayıtlarını yönetme"
        };

    private static readonly CompanyRoleDefinitionDto[] BuiltInRoles =
    [
        Role("owner", "Şirket sahibi", AssetsRead, AssetsWrite, PeopleRead, PeopleManage, EmployeeDocuments, CompanySettings),
        Role("admin", "Yönetici", AssetsRead, AssetsWrite, PeopleRead, PeopleManage, EmployeeDocuments, CompanySettings),
        Role("asset_manager", "Envanter yöneticisi", AssetsRead, AssetsWrite, PeopleRead, PeopleManage),
        Role("asset_operator", "Envanter operatörü", AssetsRead, AssetsWrite, PeopleRead),
        Role("auditor", "Denetçi", AssetsRead, PeopleRead),
        Role("viewer", "Salt okunur", AssetsRead),
        Role("hr_operator", "İK operatörü", PeopleRead, PeopleManage, EmployeeDocuments)
    ];

    public static IReadOnlyList<CompanyRoleDefinitionDto> BuiltIns => BuiltInRoles;

    public static IReadOnlyList<string> ValidatePermissionKeys(IEnumerable<string>? permissionKeys)
    {
        if (permissionKeys is null) return [];
        return permissionKeys.Where(key => key is not null && PermissionNames.ContainsKey(key))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    public static bool ArePermissionKeysValid(IEnumerable<string>? permissionKeys)
    {
        if (permissionKeys is null) return false;
        var values = permissionKeys.ToArray();
        return values.Length <= PermissionNames.Count && values.All(key => key is not null && PermissionNames.ContainsKey(key)) &&
            values.Distinct(StringComparer.Ordinal).Count() == values.Length;
    }

    public static string SerializePermissionKeys(IEnumerable<string> permissionKeys) =>
        JsonSerializer.Serialize(ValidatePermissionKeys(permissionKeys));

    public static string[] ParsePermissionKeys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(json) ?? [];
            return ValidatePermissionKeys(values).ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static async Task<(bool Valid, string[] PermissionKeys)> ResolveAsync(AppDbContext db,
        Guid companyId, string roleKey, CancellationToken cancellationToken)
    {
        var builtIn = BuiltInRoles.FirstOrDefault(role => role.RoleKey == roleKey);
        if (builtIn is not null) return (true, builtIn.PermissionKeys);

        var customRole = await db.CompanyCustomRoles.AsNoTracking()
            .Where(role => role.CompanyId == companyId && role.RoleKey == roleKey && role.IsActive)
            .Select(role => new { role.PermissionsJson })
            .SingleOrDefaultAsync(cancellationToken);
        return customRole is null
            ? (false, [])
            : (true, ParsePermissionKeys(customRole.PermissionsJson));
    }

    public static string[] ToPolicyClaims(IEnumerable<string> permissionKeys)
    {
        var claims = new List<string>();
        foreach (var permission in ValidatePermissionKeys(permissionKeys))
        {
            switch (permission)
            {
                case AssetsRead: claims.Add("asset.read"); break;
                case AssetsWrite: claims.Add("asset.write"); break;
                case PeopleRead: claims.Add("people.read"); break;
                case PeopleManage: claims.Add("people.manage"); break;
                case EmployeeDocuments:
                    claims.Add("people.files.read");
                    claims.Add("people.files.manage");
                    break;
                case CompanySettings: claims.Add("settings.manage"); break;
            }
        }
        return claims.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    public static bool IsBuiltIn(string roleKey) => BuiltInRoles.Any(role => role.RoleKey == roleKey);

    public static async Task<bool> CanUseAsync(AppDbContext db, Guid companyId, string roleKey,
        CancellationToken cancellationToken) => IsBuiltIn(roleKey) || await db.CompanyCustomRoles.AsNoTracking()
            .AnyAsync(role => role.CompanyId == companyId && role.RoleKey == roleKey && role.IsActive, cancellationToken);

    private static CompanyRoleDefinitionDto Role(string key, string name, params string[] permissions) =>
        new(key, name, permissions, BuiltIn: true, IsActive: true);
}
