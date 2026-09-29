namespace NaijaPrimeSchool.Infrastructure.Notifications;

internal static class QuietHours
{
    // Resolves the configured zone, falling back to UTC+1 (West Africa Time,
    // no daylight saving) if the host has no entry for the id.
    public static TimeZoneInfo ResolveZone(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("WAT", TimeSpan.FromHours(1), "West Africa Time", "WAT");
    }

    // When utcNow falls inside the [start, end) window (local to zone),
    // returns the UTC instant the window ends; otherwise null. A window with
    // start > end wraps midnight (21:00-07:00); start == end is no window.
    public static DateTimeOffset? QuietUntil(DateTimeOffset utcNow, TimeOnly start, TimeOnly end, TimeZoneInfo zone)
    {
        if (start == end) return null;

        var local = TimeZoneInfo.ConvertTime(utcNow, zone);
        var now = TimeOnly.FromTimeSpan(local.TimeOfDay);

        var wraps = start > end;
        var inside = wraps ? now >= start || now < end : now >= start && now < end;
        if (!inside) return null;

        // Before midnight in a wrapping window, the window ends tomorrow.
        var endDate = wraps && now >= start ? local.Date.AddDays(1) : local.Date;
        var localEnd = DateTime.SpecifyKind(endDate + end.ToTimeSpan(), DateTimeKind.Unspecified);
        return new DateTimeOffset(localEnd, zone.GetUtcOffset(localEnd)).ToUniversalTime();
    }
}
