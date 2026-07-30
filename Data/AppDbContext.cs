using Microsoft.EntityFrameworkCore;
using SportnerApi.Models;

namespace SportnerApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Sport> Sports => Set<Sport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Profile>(entity =>
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
        });

        modelBuilder.Entity<Event>(entity =>
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
            entity.Ignore(e => e.Location);

            entity.HasOne(e => e.Organizer)
                .WithMany(p => p.OrganizedEvents)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Participants)
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
        });

        modelBuilder.Entity<EventParticipant>(entity =>
        {
            entity.ToTable("event_participants");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.EventId).HasColumnName("event_id");
            entity.Property(p => p.UserId).HasColumnName("user_id");
            entity.Property(p => p.Status).HasColumnName("status").IsRequired();
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");

            entity.HasOne(p => p.User)
                .WithMany(pr => pr.Participations)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
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
        });

        modelBuilder.Entity<Review>(entity =>
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

            entity.HasOne(r => r.Reviewer)
                .WithMany(p => p.ReviewsGiven)
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Reviewed)
                .WithMany(p => p.ReviewsReceived)
                .HasForeignKey(r => r.ReviewedId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sport>(entity =>
        {
            entity.ToTable("sports");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.Name).HasColumnName("name").IsRequired();
            entity.Property(s => s.IconName).HasColumnName("icon_name");
            entity.Property(s => s.Category).HasColumnName("category");
        });
    }
}
