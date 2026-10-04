using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroRewindWebsite.Models.Entities.Player;

namespace RetroRewindWebsite.Data.Configurations;

public class PlayerBadgeEntityConfiguration : IEntityTypeConfiguration<PlayerBadgeEntity>
{
    public void Configure(EntityTypeBuilder<PlayerBadgeEntity> entity)
    {
        entity.HasKey(e => new { e.PlayerId, e.BadgeId });
        entity.HasOne(e => e.Player)
            .WithMany()
            .HasForeignKey(e => e.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Badge)
            .WithMany()
            .HasForeignKey(e => e.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
