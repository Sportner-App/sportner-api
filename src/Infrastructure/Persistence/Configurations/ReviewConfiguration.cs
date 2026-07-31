using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> entity)
    {
        entity.ToTable("reviews");
        entity.HasKey(r => r.Id);

        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.EventId).HasColumnName("event_id");
        entity.Property(r => r.ReviewerId).HasColumnName("reviewer_id");
        entity.Property(r => r.ReviewedId).HasColumnName("reviewed_id");
        entity.Property(r => r.Rating).HasColumnName("rating");
        entity.Property(r => r.Comment).HasColumnName("comment");
        entity.Property(r => r.CreatedAt).HasColumnName("created_at");

        entity.HasIndex(r => new { r.EventId, r.ReviewerId, r.ReviewedId }).IsUnique();

        entity.HasOne(r => r.Reviewer)
            .WithMany(p => p.ReviewsGiven)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(r => r.Reviewed)
            .WithMany(p => p.ReviewsReceived)
            .HasForeignKey(r => r.ReviewedId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
