using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> entity)
    {
        entity.ToTable("messages");
        entity.HasKey(m => m.Id);

        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.EventId).HasColumnName("event_id");
        entity.Property(m => m.UserId).HasColumnName("user_id");
        entity.Property(m => m.Content).HasColumnName("content").IsRequired();
        entity.Property(m => m.CreatedAt).HasColumnName("created_at");

        entity.HasOne(m => m.User)
            .WithMany(p => p.Messages)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
