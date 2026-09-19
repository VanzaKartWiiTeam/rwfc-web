using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroRewindWebsite.Models.Entities.Player;

namespace RetroRewindWebsite.Data.Configurations;

public class PlayerStreakEntityConfiguration : IEntityTypeConfiguration<PlayerStreakEntity>
{
    public void Configure(EntityTypeBuilder<PlayerStreakEntity> entity)
    {
        entity.HasIndex(e => e.CurrentStreak);
        entity.HasIndex(e => e.LastActiveDay);
    }
}
