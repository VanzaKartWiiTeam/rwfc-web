using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetroRewindWebsite.Models.Entities.Player;

/// <summary>
/// Daily play streak of a profile. A day counts as active when the profile has at least one
/// race result (public or private room) on that day, measured in <see cref="Helpers.StreakCalendar"/> time.
/// The stored streak is not zeroed when a day is missed: a streak is broken when
/// <see cref="LastActiveDay"/> is older than yesterday and the profile is not on vacation.
/// </summary>
[Table("PlayerStreaks")]
public class PlayerStreakEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long ProfileId { get; set; }

    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateOnly? StreakStartDay { get; set; }
    public DateOnly? LastActiveDay { get; set; }

    // The previous streak, kept after it broke and a new one started, so it can still be restored
    public int LostStreak { get; set; }
    public DateOnly? LostStreakStartDay { get; set; }
    public DateOnly? LostStreakLastDay { get; set; }

    // While set, missed days do not break the streak and played days do not extend it
    public DateOnly? VacationSince { get; set; }

    // Restores are a fixed monthly allowance; RestoreMonth is yyyyMM of the month RestoresUsed refers to
    public int RestoreMonth { get; set; }
    public int RestoresUsed { get; set; }

    public DateTime UpdatedAt { get; set; }
}
