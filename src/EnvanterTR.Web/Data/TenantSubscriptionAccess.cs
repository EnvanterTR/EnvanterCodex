namespace EnvanterTR.Web.Data;

/// <summary>Central subscription-period policy used by request authorization and the account session summary.</summary>
public static class TenantSubscriptionAccess
{
    public static bool IsReadOnly(string? deploymentKind, string? status,
        DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset now)
    {
        if (!string.Equals(deploymentKind, "cloud", StringComparison.OrdinalIgnoreCase))
            return false;

        if (status is not ("Active" or "Trial" or "Cancelled"))
            return true;

        return now < startsAt || now >= endsAt;
    }
}
