using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> entity)
    {
        entity.ToTable("events");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by");
        entity.Property(e => e.Title).HasColumnName("title").IsRequired();
        entity.Property(e => e.Description).HasColumnName("description");
        entity.Property(e => e.SportType).HasColumnName("sport_type").IsRequired();
        entity.Property(e => e.EventDate).HasColumnName("event_date");
        entity.Property(e => e.MaxPlayers).HasColumnName("max_players");
        entity.Property(e => e.AddressText).HasColumnName("address_text");
        entity.Property(e => e.ParticipantsCount).HasColumnName("participants_count");
        entity.Property(e => e.Latitude).HasColumnName("latitude");
        entity.Property(e => e.Longitude).HasColumnName("longitude");

        entity.HasIndex(e => e.EventDate);

        entity.HasOne(e => e.Organizer)
            .WithMany(p => p.OrganizedEvents)
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.UserEvents)
            .WithOne(p => p.Event)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Messages)
            .WithOne(m => m.Event)
            .HasForeignKey(m => m.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Reviews)
            .WithOne(r => r.Event)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
