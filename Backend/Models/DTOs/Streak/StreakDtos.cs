namespace RetroRewindWebsite.Models.DTOs.Streak;

public record StreakDto(
    long ProfileId,
    int CurrentStreak,          // 0 when the streak is broken
    int BestStreak,
    bool PlayedToday,
    bool OnVacation,
    DateOnly? VacationSince,
    DateOnly? LastActiveDay,
    int RestoresLeft,
    int RestoreCost,            // days a restore would fill in, 0 when there is nothing to restore
    int RestoredStreak,         // streak after a restore, 0 when there is nothing to restore
    DateOnly Today);

public record StreakActionResultDto(bool Success, string? Error, StreakDto Streak);

public record StreakRestoreRequest(long Pid);

public record StreakVacationRequest(long Pid, bool Enabled);
