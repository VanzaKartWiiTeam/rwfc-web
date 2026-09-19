using Microsoft.EntityFrameworkCore;
using RetroRewindWebsite.Data;
using RetroRewindWebsite.Helpers;
using RetroRewindWebsite.Models.DTOs.Streak;
using RetroRewindWebsite.Models.Entities.Player;

namespace RetroRewindWebsite.Services.Application;

public class StreakService : IStreakService
{
    public const int MonthlyRestores = 5;

    private readonly LeaderboardDbContext _context;
    private readonly ILogger<StreakService> _logger;

    public StreakService(LeaderboardDbContext context, ILogger<StreakService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordActivityAsync(IEnumerable<long> profileIds, DateTime raceTimestampUtc)
    {
        var ids = profileIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        var day = StreakCalendar.DayOf(raceTimestampUtc);
        var streaks = await _context.PlayerStreaks
            .Where(s => ids.Contains(s.ProfileId))
            .ToDictionaryAsync(s => s.ProfileId);

        foreach (var profileId in ids)
        {
            if (!streaks.TryGetValue(profileId, out var streak))
            {
                streak = new PlayerStreakEntity { ProfileId = profileId };
                _context.PlayerStreaks.Add(streak);
            }

            if (RecordDay(streak, day))
                streak.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<StreakDto?> GetStreakAsync(long profileId)
    {
        var streak = await _context.PlayerStreaks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProfileId == profileId);

        return streak == null ? null : ToDto(streak, StreakCalendar.Today());
    }

    public async Task<List<StreakDto>> GetTopStreaksAsync(int limit)
    {
        var today = StreakCalendar.Today();
        var yesterday = today.AddDays(-1);

        var streaks = await _context.PlayerStreaks
            .AsNoTracking()
            .Where(s => s.VacationSince != null || s.LastActiveDay >= yesterday)
            .OrderByDescending(s => s.CurrentStreak)
            .ThenBy(s => s.StreakStartDay)
            .Take(limit)
            .ToListAsync();

        return streaks.Select(s => ToDto(s, today)).ToList();
    }

    public async Task<StreakActionResultDto?> RestoreAsync(long profileId)
    {
        var streak = await _context.PlayerStreaks.FirstOrDefaultAsync(s => s.ProfileId == profileId);
        if (streak == null)
            return null;

        var today = StreakCalendar.Today();
        RefreshRestoreMonth(streak, today);

        if (streak.VacationSince != null)
            return Fail(streak, today, "on_vacation");

        var option = GetRestoreOption(streak, today);
        if (option == null)
            return Fail(streak, today, "nothing_to_restore");

        var (cost, restoredStreak) = option.Value;
        if (cost > MonthlyRestores - streak.RestoresUsed)
            return Fail(streak, today, "not_enough_restores");

        if (streak.LastActiveDay < today.AddDays(-1))
        {
            // Broken and not restarted yet: the missed days run up to yesterday
            streak.LastActiveDay = today.AddDays(-1);
        }
        else
        {
            // A new streak already started: glue the lost one in front of it
            streak.StreakStartDay = streak.LostStreakStartDay;
            ClearLostStreak(streak);
        }

        streak.CurrentStreak = restoredStreak;
        streak.BestStreak = Math.Max(streak.BestStreak, restoredStreak);
        streak.RestoresUsed += cost;
        streak.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Restored streak of {ProfileId} to {Streak} days using {Cost} restores",
            profileId, restoredStreak, cost);

        return new StreakActionResultDto(true, null, ToDto(streak, today));
    }

    public async Task<StreakActionResultDto?> SetVacationAsync(long profileId, bool enabled)
    {
        var streak = await _context.PlayerStreaks.FirstOrDefaultAsync(s => s.ProfileId == profileId);
        if (streak == null)
            return null;

        var today = StreakCalendar.Today();
        var yesterday = today.AddDays(-1);

        if (enabled)
        {
            if (streak.VacationSince != null)
                return Fail(streak, today, "already_on_vacation");

            // Vacation protects a streak that is still alive, it cannot be declared after the fact
            if (streak.LastActiveDay == null || streak.LastActiveDay < yesterday)
                return Fail(streak, today, "streak_not_active");

            streak.VacationSince = today;
        }
        else
        {
            if (streak.VacationSince == null)
                return Fail(streak, today, "not_on_vacation");

            // Skip the vacation days: the streak continues as if the player last played yesterday
            if (streak.LastActiveDay < yesterday)
                streak.LastActiveDay = yesterday;

            streak.VacationSince = null;
        }

        streak.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vacation {State} for {ProfileId}", enabled ? "started" : "ended", profileId);

        return new StreakActionResultDto(true, null, ToDto(streak, today));
    }

    public async Task BackfillIfEmptyAsync()
    {
        if (await _context.PlayerStreaks.AnyAsync())
            return;

        var activeDays = await _context.Database
            .SqlQueryRaw<ActiveDayRow>($"""
                SELECT "ProfileId", ("RaceTimestamp" AT TIME ZONE '{StreakCalendar.TimeZoneId}')::date AS "Day"
                FROM "RaceResults"
                GROUP BY 1, 2
                ORDER BY 1, 2
                """)
            .ToListAsync();

        if (activeDays.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var days in activeDays.GroupBy(d => d.ProfileId))
        {
            var streak = new PlayerStreakEntity { ProfileId = days.Key, UpdatedAt = now };
            foreach (var row in days)
                RecordDay(streak, row.Day);

            _context.PlayerStreaks.Add(streak);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Backfilled streaks for {Count} profiles from {Days} active days",
            activeDays.Select(d => d.ProfileId).Distinct().Count(), activeDays.Count);
    }

    // ===== RULES =====

    /// <summary>
    /// Applies one active day to the streak. Returns false when nothing changed.
    /// </summary>
    private static bool RecordDay(PlayerStreakEntity streak, DateOnly day)
    {
        if (streak.VacationSince != null)
            return false;

        if (streak.LastActiveDay >= day)
            return false;

        if (streak.LastActiveDay == day.AddDays(-1))
        {
            streak.CurrentStreak++;
        }
        else
        {
            if (streak.CurrentStreak > 0)
            {
                streak.LostStreak = streak.CurrentStreak;
                streak.LostStreakStartDay = streak.StreakStartDay;
                streak.LostStreakLastDay = streak.LastActiveDay;
            }

            streak.CurrentStreak = 1;
            streak.StreakStartDay = day;
        }

        streak.LastActiveDay = day;
        streak.BestStreak = Math.Max(streak.BestStreak, streak.CurrentStreak);
        return true;
    }

    /// <summary>
    /// Returns how many missed days a restore would fill in and the resulting streak length,
    /// or null when there is nothing to restore. Restored days count towards the streak.
    /// </summary>
    private static (int Cost, int RestoredStreak)? GetRestoreOption(PlayerStreakEntity streak, DateOnly today)
    {
        if (streak.VacationSince != null || streak.LastActiveDay == null)
            return null;

        var yesterday = today.AddDays(-1);

        if (streak.LastActiveDay < yesterday)
        {
            var missed = yesterday.DayNumber - streak.LastActiveDay.Value.DayNumber;
            return (missed, streak.CurrentStreak + missed);
        }

        if (streak.LostStreak > 0 && streak.LostStreakLastDay != null && streak.StreakStartDay != null)
        {
            var missed = streak.StreakStartDay.Value.DayNumber - streak.LostStreakLastDay.Value.DayNumber - 1;
            if (missed > 0)
                return (missed, streak.LostStreak + missed + streak.CurrentStreak);
        }

        return null;
    }

    private static void RefreshRestoreMonth(PlayerStreakEntity streak, DateOnly today)
    {
        var month = StreakCalendar.MonthKey(today);
        if (streak.RestoreMonth != month)
        {
            streak.RestoreMonth = month;
            streak.RestoresUsed = 0;
        }
    }

    private static void ClearLostStreak(PlayerStreakEntity streak)
    {
        streak.LostStreak = 0;
        streak.LostStreakStartDay = null;
        streak.LostStreakLastDay = null;
    }

    private static StreakDto ToDto(PlayerStreakEntity streak, DateOnly today)
    {
        var onVacation = streak.VacationSince != null;
        var alive = onVacation || streak.LastActiveDay >= today.AddDays(-1);
        var restoresUsed = streak.RestoreMonth == StreakCalendar.MonthKey(today) ? streak.RestoresUsed : 0;
        var option = GetRestoreOption(streak, today);

        return new StreakDto(
            streak.ProfileId,
            alive ? streak.CurrentStreak : 0,
            streak.BestStreak,
            streak.LastActiveDay == today,
            onVacation,
            streak.VacationSince,
            streak.LastActiveDay,
            MonthlyRestores - restoresUsed,
            option?.Cost ?? 0,
            option?.RestoredStreak ?? 0,
            today);
    }

    private static StreakActionResultDto Fail(PlayerStreakEntity streak, DateOnly today, string error) =>
        new(false, error, ToDto(streak, today));

    private sealed class ActiveDayRow
    {
        public long ProfileId { get; set; }
        public DateOnly Day { get; set; }
    }
}
