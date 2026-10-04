using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroRewindWebsite.Models.Entities.Player;

namespace RetroRewindWebsite.Data.Configurations;

public class BadgeEntityConfiguration : IEntityTypeConfiguration<BadgeEntity>
{
    public void Configure(EntityTypeBuilder<BadgeEntity> entity)
    {
        entity.Property(e => e.Role).HasMaxLength(50);
        entity.Property(e => e.Name).HasMaxLength(100);
        entity.HasIndex(e => e.Role).IsUnique();

        entity.HasData(
            new BadgeEntity { Id = 0xF085, Role = "ROLE_MODERATOR", Name = "Moderator" },
            new BadgeEntity { Id = 0xF086, Role = "ROLE_LEADER", Name = "Creator (VanzaKart)" },
            new BadgeEntity { Id = 0xF087, Role = "ROLE_STAFF_GHOST", Name = "Staff Ghost" },
            new BadgeEntity { Id = 0xF088, Role = "ROLE_DEVELOPER", Name = "Developer" },
            new BadgeEntity { Id = 0xF089, Role = "ROLE_CREATIVE_DIRECTOR", Name = "Creative Director" },
            new BadgeEntity { Id = 0xF08A, Role = "ROLE_TRANSLATOR", Name = "Translator" },
            new BadgeEntity { Id = 0xF08B, Role = "ROLE_BETA_TESTER", Name = "Beta Tester" });
    }
}
