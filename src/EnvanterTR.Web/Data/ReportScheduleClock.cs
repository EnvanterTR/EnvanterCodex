namespace EnvanterTR.Web.Data;

internal sealed class ReportScheduleException(string message) : InvalidOperationException(message);

internal static class ReportScheduleClock
{
    public static bool TryGetTimeZone(string? id, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100) return false;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }

    public static DateTimeOffset NextDue(ScheduledReport report, DateTimeOffset after)
    {
        if (!TryGetTimeZone(report.TimeZoneId, out var zone)) throw new ReportScheduleException("Invalid report time zone.");
        var localDate = TimeZoneInfo.ConvertTime(after, zone).Date;
        for (var day = 0; day < 370; day++)
        {
            var date = localDate.AddDays(day);
            if (report.Frequency == "weekly" && (int)date.DayOfWeek != report.WeekDay) continue;
            if (report.Frequency == "monthly" && date.Day != report.MonthDay) continue;
            var local = DateTime.SpecifyKind(date.AddHours(report.LocalHour).AddMinutes(report.LocalMinute), DateTimeKind.Unspecified);
            // A nonexistent DST time has no delivery that day. On a repeated local
            // time, pick only the first UTC occurrence after the scheduling point.
            if (zone.IsInvalidTime(local)) continue;
            var offsets = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local) : [zone.GetUtcOffset(local)];
            var candidates = offsets.Select(offset => new DateTimeOffset(local, offset).ToUniversalTime()).Order().ToArray();
            // Use the first occurrence only, so an autumn clock change does not
            // create a second delivery for the same scheduled local time.
            if (candidates[0] > after) return candidates[0];
        }
        throw new ReportScheduleException("No report delivery date could be calculated.");
    }
}
