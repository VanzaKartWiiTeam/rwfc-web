using System.ComponentModel.DataAnnotations.Schema;

namespace RetroRewindWebsite.Models.Entities.Player;

[Table("PlayerBadges")]
public class PlayerBadgeEntity
{
    public int PlayerId { get; set; }
    public int BadgeId { get; set; }

    public virtual PlayerEntity Player { get; set; } = null!;
    public virtual BadgeEntity Badge { get; set; } = null!;
}
