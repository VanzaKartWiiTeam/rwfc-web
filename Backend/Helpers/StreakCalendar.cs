namespace RetroRewindWebsite.Helpers;

/// <summary>
/// Streak days roll over at midnight US Eastern time, where most of the player base is.
/// </summary>
public static class StreakCalendar
{
    public const string TimeZoneId = "America/New_York";

    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    public static DateOnly DayOf(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone));

    public static DateOnly Today() => DayOf(DateTime.UtcNow);

    public static int MonthKey(DateOnly day) => day.Year * 100 + day.Month;
}
