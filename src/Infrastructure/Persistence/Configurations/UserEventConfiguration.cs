using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Configurations;

public class UserEventConfiguration : IEntityTypeConfiguration<UserEvent>
{
    public void Configure(EntityTypeBuilder<UserEvent> entity)
    {
        entity.ToTable("event_participants");
        entity.HasKey(p => p.Id);

        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.EventId).HasColumnName("event_id");
        entity.Property(p => p.UserId).HasColumnName("user_id");
        entity.Property(p => p.Status).HasColumnName("status").IsRequired();
        entity.Property(p => p.CreatedAt).HasColumnName("created_at");

        entity.HasIndex(p => new { p.EventId, p.UserId }).IsUnique();
        entity.HasIndex(p => new { p.UserId, p.Status });

        entity.HasOne(p => p.User)
            .WithMany(pr => pr.UserEvents)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
