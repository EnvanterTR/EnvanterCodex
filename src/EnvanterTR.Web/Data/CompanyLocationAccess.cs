using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record SaveMemberLocationAccessRequest(string? Mode, Guid[]? LocationIds,
    string? CompanyUnitMode = null, Guid[]? CompanyUnitIds = null);

public static class CompanyLocationAccess
{
    public const string All = "all";
    public const string Restricted = "restricted";

    public static async Task<EmployeeAccessScope> GetEmployeeAccessScopeAsync(AppDbContext db, Guid companyId,
        Guid userId, CancellationToken cancellationToken)
    {
        var modes = await db.CompanyMemberships.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.UserId == userId)
            .Select(x => new { x.LocationAccessMode, x.CompanyUnitAccessMode })
            .SingleOrDefaultAsync(cancellationToken);
        if (modes is null || modes.LocationAccessMode is not (All or Restricted) ||
            modes.CompanyUnitAccessMode is not (All or Restricted))
            return new EmployeeAccessScope([], [], true, true);

        var allowedLocations = await GetEffectiveLocationIdsAsync(db, companyId, userId, cancellationToken);
        var allowedCompanyUnits = await GetEffectiveCompanyUnitIdsAsync(db, companyId, userId, cancellationToken);
        return new EmployeeAccessScope(allowedLocations, allowedCompanyUnits,
            modes.LocationAccessMode == Restricted, modes.CompanyUnitAccessMode == Restricted);
    }

    public static async Task<HashSet<Guid>?> GetEffectiveCompanyUnitIdsAsync(AppDbContext db, Guid companyId,
        Guid userId, CancellationToken cancellationToken)
    {
        var accessMode = await db.CompanyMemberships.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.UserId == userId)
            .Select(x => x.CompanyUnitAccessMode).SingleOrDefaultAsync(cancellationToken);
        if (accessMode is null || accessMode is not (All or Restricted)) return [];
        if (accessMode == All) return null;
        var units = await db.CompanyUnits.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .Select(x => new { x.Id, x.ParentCompanyUnitId }).ToListAsync(cancellationToken);
        var activeIds = units.Select(x => x.Id).ToHashSet();
        var selectedIds = await db.CompanyMembershipCompanyUnitAccesses.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.UserId == userId)
            .Select(x => x.CompanyUnitId).ToListAsync(cancellationToken);
        var children = units.Where(x => x.ParentCompanyUnitId.HasValue)
            .GroupBy(x => x.ParentCompanyUnitId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(x => x.Id).ToArray());
        var allowed = new HashSet<Guid>();
        var pending = new Queue<Guid>(selectedIds.Where(activeIds.Contains));
        while (pending.TryDequeue(out var id))
        {
            if (!allowed.Add(id) || !children.TryGetValue(id, out var descendants)) continue;
            foreach (var child in descendants) pending.Enqueue(child);
        }
        return allowed;
    }

    // A null result means unrestricted access. An empty set means restricted access to nothing.
    public static async Task<HashSet<Guid>?> GetEffectiveLocationIdsAsync(AppDbContext db, Guid companyId,
        Guid userId, CancellationToken cancellationToken)
    {
        var membership = await db.CompanyMemberships.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.UserId == userId)
            .Select(x => new { x.LocationAccessMode, x.CompanyUnitAccessMode })
            .SingleOrDefaultAsync(cancellationToken);
        if (membership is null) return [];
        if (membership.LocationAccessMode is not (All or Restricted) ||
            membership.CompanyUnitAccessMode is not (All or Restricted)) return [];
        var restrictLocations = membership.LocationAccessMode == Restricted;
        var restrictUnits = membership.CompanyUnitAccessMode == Restricted;
        if (!restrictLocations && !restrictUnits) return null;

        var locations = await db.CompanyLocations.AsNoTracking().Where(x => x.CompanyId == companyId)
            .Select(x => new { x.Id, x.ParentLocationId, x.CompanyUnitId }).ToListAsync(cancellationToken);
        var children = locations.Where(x => x.ParentLocationId.HasValue)
            .GroupBy(x => x.ParentLocationId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(x => x.Id).ToArray());
        HashSet<Guid> allowed = locations.Select(x => x.Id).ToHashSet();

        if (restrictLocations)
        {
            var selectedLocations = await db.CompanyMembershipLocationAccesses.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => x.LocationId).ToListAsync(cancellationToken);
            var selectedLocationScope = Expand(selectedLocations, children);
            allowed.IntersectWith(selectedLocationScope);
        }

        if (restrictUnits)
        {
            var allowedUnits = await GetEffectiveCompanyUnitIdsAsync(db, companyId, userId, cancellationToken) ?? [];
            var units = await db.CompanyUnits.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
                .Select(x => new { x.Id, x.ParentCompanyUnitId }).ToListAsync(cancellationToken);
            var unitChildren = units.Where(x => x.ParentCompanyUnitId.HasValue)
                .GroupBy(x => x.ParentCompanyUnitId!.Value)
                .ToDictionary(group => group.Key, group => group.Select(x => x.Id).ToArray());
            var locationUnit = locations.ToDictionary(x => x.Id, x => x.CompanyUnitId);
            var locationParent = locations.ToDictionary(x => x.Id, x => x.ParentLocationId);
            var matchingLocations = new HashSet<Guid>();
            foreach (var location in locations)
            {
                var cursor = location.Id;
                var seen = new HashSet<Guid>();
                Guid? inheritedUnit = null;
                while (seen.Add(cursor) && locationUnit.TryGetValue(cursor, out var unitId))
                {
                    if (unitId.HasValue) { inheritedUnit = unitId; break; }
                    if (locationParent.TryGetValue(cursor, out var parentId) && parentId is Guid parent) cursor = parent;
                    else break;
                }
                if (inheritedUnit is Guid id && allowedUnits.Contains(id)) matchingLocations.Add(location.Id);
            }
            allowed.IntersectWith(matchingLocations);
        }
        return allowed;

        static HashSet<Guid> Expand(IEnumerable<Guid> roots, Dictionary<Guid, Guid[]> descendantsById)
        {
            var expanded = new HashSet<Guid>();
            var pending = new Queue<Guid>(roots);
            while (pending.TryDequeue(out var id))
            {
                if (!expanded.Add(id) || !descendantsById.TryGetValue(id, out var descendants)) continue;
                foreach (var child in descendants) pending.Enqueue(child);
            }
            return expanded;
        }
    }

    public static async Task<bool> CanAccessLocationAsync(AppDbContext db, Guid companyId, Guid userId,
        Guid? locationId, CancellationToken cancellationToken)
    {
        var allowed = await GetEffectiveLocationIdsAsync(db, companyId, userId, cancellationToken);
        return allowed is null || locationId is Guid id && allowed.Contains(id);
    }

    public static async Task<bool> CanAccessEmployeeAsync(AppDbContext db, Guid companyId, Guid userId,
        Guid? locationId, Guid? companyUnitId, CancellationToken cancellationToken)
    {
        var scope = await GetEmployeeAccessScopeAsync(db, companyId, userId, cancellationToken);
        return scope.Allows(locationId, companyUnitId);
    }

    public static async Task<bool> CanAccessLocationPathAsync(AppDbContext db, Guid companyId, Guid userId,
        string? locationPath, CancellationToken cancellationToken)
    {
        var allowed = await GetEffectiveLocationIdsAsync(db, companyId, userId, cancellationToken);
        if (allowed is null) return true;
        if (string.IsNullOrWhiteSpace(locationPath)) return false;
        var locationId = await AssetEndpoints.ResolveLocationCatalogIdAsync(db, companyId, locationPath, cancellationToken);
        return locationId is Guid id && allowed.Contains(id);
    }
}

public sealed record EmployeeAccessScope(HashSet<Guid>? AllowedLocations, HashSet<Guid>? AllowedCompanyUnits,
    bool RestrictLocations, bool RestrictCompanyUnits)
{
    public bool IsRestricted => RestrictLocations || RestrictCompanyUnits;

    public bool Allows(Guid? locationId, Guid? companyUnitId)
    {
        if (!IsRestricted) return true;
        if (locationId is Guid location)
            return (AllowedLocations is null || AllowedLocations.Contains(location)) &&
                (AllowedCompanyUnits is null || !companyUnitId.HasValue || AllowedCompanyUnits.Contains(companyUnitId.Value));
        return !RestrictLocations && companyUnitId is Guid unit &&
            (AllowedCompanyUnits is null || AllowedCompanyUnits.Contains(unit));
    }

    public IQueryable<Employee> Apply(IQueryable<Employee> query)
    {
        if (!IsRestricted) return query;
        return query.Where(employee => employee.LocationCatalogId.HasValue
            ? (AllowedLocations == null || AllowedLocations.Contains(employee.LocationCatalogId.Value)) &&
              (AllowedCompanyUnits == null || !employee.CompanyUnitId.HasValue || AllowedCompanyUnits.Contains(employee.CompanyUnitId.Value))
            : !RestrictLocations && employee.CompanyUnitId.HasValue &&
              (AllowedCompanyUnits == null || AllowedCompanyUnits.Contains(employee.CompanyUnitId.Value)));
    }
}

public static class CompanyLocationAccessEndpoints
{
    public static IEndpointRouteBuilder MapCompanyLocationAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/members/{userId:guid}/location-access", async (Guid userId, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken) is not null)
                return Results.Forbid();
            var membership = await db.CompanyMemberships.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => new { x.LocationAccessMode, x.CompanyUnitAccessMode, x.RoleKey }).SingleOrDefaultAsync(cancellationToken);
            if (membership is null) return Results.NotFound();
            var selectedIds = await db.CompanyMembershipLocationAccesses.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => x.LocationId).ToArrayAsync(cancellationToken);
            var selectedUnitIds = await db.CompanyMembershipCompanyUnitAccesses.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => x.CompanyUnitId).ToArrayAsync(cancellationToken);
            var locations = await db.CompanyLocations.AsNoTracking().Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.Name).Select(x => new { x.Id, x.ParentLocationId, x.Name, x.IsActive })
                .ToArrayAsync(cancellationToken);
            var byId = locations.ToDictionary(x => x.Id);
            string Path(Guid id)
            {
                var names = new List<string>();
                var seen = new HashSet<Guid>();
                while (byId.TryGetValue(id, out var current) && seen.Add(id))
                {
                    names.Add(current.Name);
                    if (current.ParentLocationId is not Guid parent) break;
                    id = parent;
                }
                names.Reverse();
                return string.Join(" / ", names);
            }
            var companyUnits = await db.CompanyUnits.AsNoTracking().Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.Name).Select(x => new { x.Id, x.ParentCompanyUnitId, x.Name, x.Code, x.UnitType, x.IsActive })
                .ToArrayAsync(cancellationToken);
            return Results.Ok(new { userId, mode = membership.LocationAccessMode, membership.RoleKey,
                locationIds = selectedIds, locations = locations.Select(x => new { x.Id, x.ParentLocationId, x.Name,
                    path = Path(x.Id), x.IsActive }).ToArray(),
                companyUnitMode = membership.CompanyUnitAccessMode, companyUnitIds = selectedUnitIds,
                companyUnits });
        }).RequireAuthorization("settings.manage");

        endpoints.MapPut("/members/{userId:guid}/location-access", async (Guid userId,
            [FromBody] SaveMemberLocationAccessRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryScope(user, out var companyId, out var actorId)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken) is not null)
                return Results.Forbid();
            var mode = request.Mode?.Trim().ToLowerInvariant();
            var locationIds = request.LocationIds ?? [];
            var unitMode = request.CompanyUnitMode?.Trim().ToLowerInvariant();
            var companyUnitIds = request.CompanyUnitIds ?? [];
            if (mode is not (CompanyLocationAccess.All or CompanyLocationAccess.Restricted) ||
                locationIds.Length > 500 || locationIds.Distinct().Count() != locationIds.Length ||
                (mode == CompanyLocationAccess.Restricted && locationIds.Length == 0) ||
                (mode == CompanyLocationAccess.All && locationIds.Length != 0))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["locationAccess"] = ["Tüm konumlar için boş liste, sınırlı erişim için en az bir farklı konum seçin (en fazla 500)."] });
            if (unitMode is not null && (unitMode is not (CompanyLocationAccess.All or CompanyLocationAccess.Restricted) ||
                companyUnitIds.Length > 500 || companyUnitIds.Distinct().Count() != companyUnitIds.Length ||
                (unitMode == CompanyLocationAccess.Restricted && companyUnitIds.Length == 0) ||
                (unitMode == CompanyLocationAccess.All && companyUnitIds.Length != 0)))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["companyUnitAccess"] = ["Tüm şirket birimleri için boş liste, sınırlı erişim için en az bir farklı birim seçin (en fazla 500)."] });
            if (userId == actorId) return Results.Conflict(new { message = "Kendi konum erişiminizi değiştirmeniz engellendi; yönetici erişiminizin kesilmesi önleniyor." });

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var membership = await db.CompanyMemberships.FromSqlInterpolated(
                    $"SELECT * FROM \"CompanyMemberships\" WHERE \"CompanyId\" = {companyId} AND \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (membership is null) return Results.NotFound();
            var requestedUnitMode = unitMode ?? membership.CompanyUnitAccessMode;
            if (membership.RoleKey == "owner" && (mode == CompanyLocationAccess.Restricted || requestedUnitMode == CompanyLocationAccess.Restricted))
                return Results.Conflict(new { message = "Şirket sahibinin erişimi konum veya şirket birimiyle sınırlandırılamaz." });
            var activeCount = await db.CompanyLocations.CountAsync(x => x.CompanyId == companyId && x.IsActive && locationIds.Contains(x.Id), cancellationToken);
            if (activeCount != locationIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["locationIds"] = ["Seçilen konumların tümü bu şirkete ait ve etkin olmalıdır."] });
            var activeUnitCount = await db.CompanyUnits.CountAsync(x => x.CompanyId == companyId && x.IsActive && companyUnitIds.Contains(x.Id), cancellationToken);
            if (activeUnitCount != companyUnitIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["companyUnitIds"] = ["Seçilen şirket birimlerinin tümü bu şirkete ait ve etkin olmalıdır."] });

            var previousMode = membership.LocationAccessMode;
            var previousIds = await db.CompanyMembershipLocationAccesses.Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => x.LocationId).ToArrayAsync(cancellationToken);
            var previousUnitMode = membership.CompanyUnitAccessMode;
            var previousUnitIds = await db.CompanyMembershipCompanyUnitAccesses.Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => x.CompanyUnitId).ToArrayAsync(cancellationToken);
            if (previousMode == mode && previousIds.Order().SequenceEqual(locationIds.Order()) && previousUnitMode == requestedUnitMode &&
                (unitMode is null || previousUnitIds.Order().SequenceEqual(companyUnitIds.Order())))
            {
                await transaction.CommitAsync(cancellationToken);
                return Results.Ok(new { userId, mode, locationIds, companyUnitMode = previousUnitMode,
                    companyUnitIds = previousUnitIds, changed = false });
            }
            membership.LocationAccessMode = mode!;
            if (unitMode is not null) membership.CompanyUnitAccessMode = unitMode;
            await db.CompanyMembershipLocationAccesses.Where(x => x.CompanyId == companyId && x.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            if (mode == CompanyLocationAccess.Restricted)
                db.CompanyMembershipLocationAccesses.AddRange(locationIds.Select(id => new CompanyMembershipLocationAccess
                    { CompanyId = companyId, UserId = userId, LocationId = id }));
            if (unitMode is not null)
            {
                await db.CompanyMembershipCompanyUnitAccesses.Where(x => x.CompanyId == companyId && x.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);
                if (unitMode == CompanyLocationAccess.Restricted)
                    db.CompanyMembershipCompanyUnitAccesses.AddRange(companyUnitIds.Select(id => new CompanyMembershipCompanyUnitAccess
                        { CompanyId = companyId, UserId = userId, CompanyUnitId = id }));
            }
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
                TargetType = "CompanyMembership", TargetId = userId, Action = "LocationAccessChanged",
                Details = $"Konum: {previousMode} → {mode} ({previousIds.Length} → {locationIds.Length}); şirket birimi: {previousUnitMode} → {membership.CompanyUnitAccessMode} ({previousUnitIds.Length} → {(unitMode is null ? previousUnitIds.Length : companyUnitIds.Length)}). Alt konum/birimler seçilen üst öğeden devralır." });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { userId, mode, locationIds, companyUnitMode = membership.CompanyUnitAccessMode,
                companyUnitIds = unitMode is null ? previousUnitIds : companyUnitIds, changed = true });
        }).RequireAuthorization("settings.manage");
        return endpoints;
    }

    private static bool TryScope(ClaimsPrincipal user, out Guid companyId, out Guid actorId)
    {
        companyId = Guid.Empty;
        actorId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("company_id"), out companyId) &&
            Guid.TryParse(user.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out actorId);
    }
}
