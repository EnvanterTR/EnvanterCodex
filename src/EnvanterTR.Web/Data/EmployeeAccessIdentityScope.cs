using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

internal static class EmployeeAccessIdentityScope
{
    public static async Task<HashSet<Guid>> HiddenUserIdsAsync(AppDbContext db, Guid companyId,
        EmployeeAccessScope employeeScope, IEnumerable<Guid?> userIds, CancellationToken cancellationToken)
    {
        var hidden = new HashSet<Guid>();
        if (!employeeScope.IsRestricted) return hidden;

        var candidates = userIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        if (candidates.Length == 0) return hidden;

        var memberships = await db.CompanyMemberships.AsNoTracking()
            .Where(membership => membership.CompanyId == companyId && candidates.Contains(membership.UserId) &&
                membership.EmployeeId.HasValue)
            .Select(membership => new { membership.UserId, EmployeeId = membership.EmployeeId!.Value })
            .ToListAsync(cancellationToken);
        if (memberships.Count == 0) return hidden;

        var linkedEmployeeIds = memberships.Select(membership => membership.EmployeeId).Distinct().ToArray();
        var visibleEmployeeIds = await employeeScope.Apply(db.Employees.AsNoTracking()
                .Where(employee => employee.CompanyId == companyId && employee.IsActive && linkedEmployeeIds.Contains(employee.Id)))
            .Select(employee => employee.Id).ToListAsync(cancellationToken);
        var visible = visibleEmployeeIds.ToHashSet();
        foreach (var membership in memberships)
            if (!visible.Contains(membership.EmployeeId)) hidden.Add(membership.UserId);
        return hidden;
    }
}
