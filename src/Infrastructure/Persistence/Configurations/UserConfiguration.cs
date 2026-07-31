using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("profiles");
        entity.HasKey(p => p.Id);

        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.Email).HasColumnName("email");
        entity.Property(p => p.PasswordHash).HasColumnName("password_hash");
        entity.Property(p => p.FullName).HasColumnName("full_name");
        entity.Property(p => p.AvatarUrl).HasColumnName("avatar_url");
        entity.Property(p => p.Bio).HasColumnName("bio");
        entity.Property(p => p.Sports).HasColumnName("sports").HasColumnType("text[]");
        entity.Property(p => p.IntroVideoUrl).HasColumnName("intro_video_url");
        entity.Property(p => p.IsOnboarded).HasColumnName("is_onboarded");
        entity.Property(p => p.BirthDate).HasColumnName("birth_date");
        entity.Property(p => p.SkillLevels)
            .HasColumnName("skill_levels")
            .HasColumnType("jsonb");
        entity.Property(p => p.AvgRating).HasColumnName("avg_rating");
        entity.Property(p => p.ReviewCount).HasColumnName("review_count");
        entity.Property(p => p.PushToken).HasColumnName("push_token");
        entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(p => p.Email).IsUnique();
    }
}
