using RetroRewindWebsite.Models.DTOs.Streak;

namespace RetroRewindWebsite.Services.Application;

public interface IStreakService
{
    /// <summary>
    /// Marks the day of <paramref name="raceTimestampUtc"/> as active for each profile.
    /// </summary>
    Task RecordActivityAsync(IEnumerable<long> profileIds, DateTime raceTimestampUtc);

    Task<StreakDto?> GetStreakAsync(long profileId);

    Task<List<StreakDto>> GetTopStreaksAsync(int limit);

    /// <summary>
    /// Fills in the missed days of the last broken streak, spending one monthly restore per day.
    /// </summary>
    Task<StreakActionResultDto?> RestoreAsync(long profileId);

    Task<StreakActionResultDto?> SetVacationAsync(long profileId, bool enabled);

    /// <summary>
    /// Builds the streaks from the race results already stored, if no streak exists yet.
    /// </summary>
    Task BackfillIfEmptyAsync();
}
