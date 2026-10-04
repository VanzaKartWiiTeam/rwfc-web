using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetroRewindWebsite.Models.Entities.Player;

[Table("Badges")]
public class BadgeEntity
{
    // In-game font code point (e.g. 0xF085).
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public required string Role { get; set; }
    public required string Name { get; set; }
}
