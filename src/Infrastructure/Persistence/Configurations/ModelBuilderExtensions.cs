using Microsoft.EntityFrameworkCore;
using Sportner.Domain.Badges;
using Sportner.Domain.Events;
using Sportner.Domain.Messaging;
using Sportner.Domain.Moderation;
using Sportner.Domain.Notifications;
using Sportner.Domain.Reviews;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;

namespace Sportner.Infrastructure.Persistence.Configurations;

internal static class ModelBuilderExtensions
{
    public static void ApplyDocumentedConstraints(this ModelBuilder modelBuilder)
    {
        ConfigureUniqueIndexes(modelBuilder);
        ConfigureQueryIndexes(modelBuilder);
        ConfigurePropertyMappings(modelBuilder);
        ConfigureDefaults(modelBuilder);
        ConfigureSmallIntColumns(modelBuilder);
        ConfigureRelationships(modelBuilder);
    }

    private static void ConfigureUniqueIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(entity => entity.PhoneNumber)
            .IsUnique();

        modelBuilder.Entity<UserProfile>()
            .HasIndex(entity => entity.UserId)
            .IsUnique();

        modelBuilder.Entity<UserProfile>()
            .HasIndex(entity => entity.Username)
            .IsUnique();

        modelBuilder.Entity<Sport>()
            .HasIndex(entity => entity.Name)
            .IsUnique();

        modelBuilder.Entity<Sport>()
            .HasIndex(entity => entity.Slug)
            .IsUnique();

        modelBuilder.Entity<UserSport>()
            .HasIndex(entity => new { entity.UserId, entity.SportId })
            .IsUnique();

        modelBuilder.Entity<UserStatistics>()
            .HasIndex(entity => entity.UserId)
            .IsUnique();

        modelBuilder.Entity<UserDevice>()
            .HasIndex(entity => entity.DeviceIdentifier)
            .IsUnique();

        modelBuilder.Entity<EventParticipant>()
            .HasIndex(entity => new { entity.EventId, entity.UserId })
            .IsUnique();

        modelBuilder.Entity<EventWaitlist>()
            .HasIndex(entity => new { entity.EventId, entity.UserId })
            .IsUnique();

        modelBuilder.Entity<EventWaitlist>()
            .HasIndex(entity => new { entity.EventId, entity.Position })
            .IsUnique();

        modelBuilder.Entity<EventReminderDispatch>()
            .HasIndex(entity => new { entity.EventId, entity.UserId, entity.WindowMinutes })
            .IsUnique();

        modelBuilder.Entity<Conversation>()
            .HasIndex(entity => entity.EventId)
            .IsUnique();

        modelBuilder.Entity<ConversationMember>()
            .HasIndex(entity => new { entity.ConversationId, entity.UserId })
            .IsUnique();

        modelBuilder.Entity<Review>()
            .HasIndex(entity => new
            {
                entity.EventId,
                entity.ReviewerUserId,
                entity.ReviewedUserId
            })
            .IsUnique();

        modelBuilder.Entity<Friendship>()
            .HasIndex(entity => new
            {
                entity.RequesterUserId,
                entity.AddresseeUserId
            })
            .IsUnique();

        modelBuilder.Entity<PostMedia>()
            .HasIndex(entity => new { entity.PostId, entity.DisplayOrder })
            .IsUnique();

        modelBuilder.Entity<PostLike>()
            .HasIndex(entity => new { entity.PostId, entity.UserId })
            .IsUnique();

        modelBuilder.Entity<NotificationSetting>()
            .HasIndex(entity => new { entity.UserId, entity.NotificationType })
            .IsUnique();

        modelBuilder.Entity<Badge>()
            .HasIndex(entity => entity.Code)
            .IsUnique();

        modelBuilder.Entity<UserBadge>()
            .HasIndex(entity => new { entity.UserId, entity.BadgeId })
            .IsUnique();

        modelBuilder.Entity<Report>()
            .HasIndex(entity => new
            {
                entity.ReporterUserId,
                entity.EntityType,
                entity.EntityId
            })
            .IsUnique();

        modelBuilder.Entity<ReportReason>()
            .HasIndex(entity => entity.Code)
            .IsUnique();
    }

    private static void ConfigureQueryIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(entity => entity.Status);
        modelBuilder.Entity<User>().HasIndex(entity => entity.LastSeenAt);

        modelBuilder.Entity<UserProfile>().HasIndex(entity => entity.City);
        modelBuilder.Entity<UserProfile>().HasIndex(entity => entity.AverageRating);

        modelBuilder.Entity<Sport>().HasIndex(entity => entity.DisplayOrder);
        modelBuilder.Entity<Sport>().HasIndex(entity => entity.IsActive);

        modelBuilder.Entity<UserSport>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserSport>().HasIndex(entity => entity.SportId);
        modelBuilder.Entity<UserSport>().HasIndex(entity => entity.SkillLevel);

        modelBuilder.Entity<UserSession>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserSession>().HasIndex(entity => entity.DeviceId);
        modelBuilder.Entity<UserSession>().HasIndex(entity => entity.ExpiresAt);
        modelBuilder.Entity<UserSession>().HasIndex(entity => entity.RevokedAt);

        modelBuilder.Entity<UserDevice>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserDevice>().HasIndex(entity => entity.LastSeenAt);

        modelBuilder.Entity<UserSavedLocation>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserSavedLocation>().HasIndex(entity => entity.City);

        modelBuilder.Entity<Event>().HasIndex(entity => entity.OrganizerUserId);
        modelBuilder.Entity<Event>().HasIndex(entity => entity.SportId);
        modelBuilder.Entity<Event>().HasIndex(entity => entity.EventDate);
        modelBuilder.Entity<Event>().HasIndex(entity => entity.Status);
        modelBuilder.Entity<Event>()
            .HasIndex(entity => new { entity.Status, entity.EventDate });

        modelBuilder.Entity<EventParticipant>().HasIndex(entity => entity.EventId);
        modelBuilder.Entity<EventParticipant>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<EventParticipant>().HasIndex(entity => entity.Status);

        modelBuilder.Entity<EventWaitlist>().HasIndex(entity => entity.EventId);
        modelBuilder.Entity<EventWaitlist>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<EventWaitlist>().HasIndex(entity => entity.Position);

        modelBuilder.Entity<Conversation>().HasIndex(entity => entity.Type);
        modelBuilder.Entity<Conversation>().HasIndex(entity => entity.IsClosed);

        modelBuilder.Entity<ConversationMember>()
            .HasIndex(entity => entity.ConversationId);
        modelBuilder.Entity<ConversationMember>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<ConversationMember>().HasIndex(entity => entity.Role);

        modelBuilder.Entity<Message>().HasIndex(entity => entity.ConversationId);
        modelBuilder.Entity<Message>().HasIndex(entity => entity.SenderUserId);
        modelBuilder.Entity<Message>().HasIndex(entity => entity.CreatedAt);

        modelBuilder.Entity<Review>().HasIndex(entity => entity.EventId);
        modelBuilder.Entity<Review>().HasIndex(entity => entity.ReviewerUserId);
        modelBuilder.Entity<Review>().HasIndex(entity => entity.ReviewedUserId);
        modelBuilder.Entity<Review>().HasIndex(entity => entity.Rating);

        modelBuilder.Entity<Friendship>().HasIndex(entity => entity.RequesterUserId);
        modelBuilder.Entity<Friendship>().HasIndex(entity => entity.AddresseeUserId);
        modelBuilder.Entity<Friendship>().HasIndex(entity => entity.Status);

        modelBuilder.Entity<Post>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<Post>().HasIndex(entity => entity.CreatedAt);
        modelBuilder.Entity<Post>()
            .HasIndex(entity => new { entity.UserId, entity.CreatedAt });

        modelBuilder.Entity<PostMedia>().HasIndex(entity => entity.PostId);

        modelBuilder.Entity<PostLike>().HasIndex(entity => entity.PostId);
        modelBuilder.Entity<PostLike>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<PostLike>()
            .HasIndex(entity => new { entity.UserId, entity.CreatedAt });

        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.PostId);
        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.ParentCommentId);
        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.CreatedAt);
        modelBuilder.Entity<PostComment>()
            .HasIndex(entity => new { entity.PostId, entity.CreatedAt });

        modelBuilder.Entity<Notification>().HasIndex(entity => entity.RecipientUserId);
        modelBuilder.Entity<Notification>().HasIndex(entity => entity.IsRead);
        modelBuilder.Entity<Notification>().HasIndex(entity => entity.CreatedAt);
        modelBuilder.Entity<Notification>().HasIndex(entity => entity.NotificationType);

        modelBuilder.Entity<NotificationSetting>().HasIndex(entity => entity.UserId);

        modelBuilder.Entity<Badge>().HasIndex(entity => entity.Category);
        modelBuilder.Entity<Badge>().HasIndex(entity => entity.Rarity);
        modelBuilder.Entity<Badge>().HasIndex(entity => entity.IsActive);

        modelBuilder.Entity<UserBadge>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserBadge>().HasIndex(entity => entity.BadgeId);
        modelBuilder.Entity<UserBadge>().HasIndex(entity => entity.EarnedAt);

        modelBuilder.Entity<Report>()
            .HasIndex(entity => new { entity.EntityType, entity.EntityId });
        modelBuilder.Entity<Report>().HasIndex(entity => entity.ReporterUserId);
        modelBuilder.Entity<Report>().HasIndex(entity => entity.Status);
        modelBuilder.Entity<Report>().HasIndex(entity => entity.CreatedAt);

        modelBuilder.Entity<ReportReason>().HasIndex(entity => entity.DisplayOrder);
        modelBuilder.Entity<ReportReason>().HasIndex(entity => entity.IsActive);
    }

    private static void ConfigurePropertyMappings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Property(entity => entity.PhoneNumber)
            .HasMaxLength(20);

        modelBuilder.Entity<UserProfile>()
            .Property(entity => entity.Username)
            .HasMaxLength(30);
        modelBuilder.Entity<UserProfile>()
            .Property(entity => entity.FirstName)
            .HasMaxLength(50);
        modelBuilder.Entity<UserProfile>()
            .Property(entity => entity.LastName)
            .HasMaxLength(50);
        modelBuilder.Entity<UserProfile>()
            .Property(entity => entity.Bio)
            .HasMaxLength(500);
        modelBuilder.Entity<UserProfile>()
            .Property(entity => entity.City)
            .HasMaxLength(100);
        modelBuilder.Entity<UserProfile>()
            .Property(entity => entity.AverageRating)
            .HasPrecision(3, 2);

        modelBuilder.Entity<Sport>()
            .Property(entity => entity.Name)
            .HasMaxLength(100);
        modelBuilder.Entity<Sport>()
            .Property(entity => entity.Slug)
            .HasMaxLength(100);

        modelBuilder.Entity<UserStatistics>()
            .Property(entity => entity.AttendanceRate)
            .HasPrecision(5, 2);
        modelBuilder.Entity<UserStatistics>()
            .Property(entity => entity.AverageRating)
            .HasPrecision(3, 2);

        modelBuilder.Entity<UserSession>()
            .Property(entity => entity.IpAddress)
            .HasMaxLength(45);

        modelBuilder.Entity<UserDevice>()
            .Property(entity => entity.DeviceName)
            .HasMaxLength(100);
        modelBuilder.Entity<UserDevice>()
            .Property(entity => entity.DeviceIdentifier)
            .HasMaxLength(255);
        modelBuilder.Entity<UserDevice>()
            .Property(entity => entity.AppVersion)
            .HasMaxLength(30);
        modelBuilder.Entity<UserDevice>()
            .Property(entity => entity.OsVersion)
            .HasMaxLength(30);

        modelBuilder.Entity<UserSavedLocation>()
            .Property(entity => entity.Title)
            .HasMaxLength(100);
        modelBuilder.Entity<UserSavedLocation>()
            .Property(entity => entity.City)
            .HasMaxLength(100);
        modelBuilder.Entity<UserSavedLocation>()
            .Property(entity => entity.District)
            .HasMaxLength(100);
        modelBuilder.Entity<UserSavedLocation>()
            .Property(entity => entity.Latitude)
            .HasPrecision(9, 6);
        modelBuilder.Entity<UserSavedLocation>()
            .Property(entity => entity.Longitude)
            .HasPrecision(9, 6);

        modelBuilder.Entity<Event>()
            .Property(entity => entity.Title)
            .HasMaxLength(150);
        modelBuilder.Entity<Event>()
            .Property(entity => entity.Latitude)
            .HasPrecision(9, 6);
        modelBuilder.Entity<Event>()
            .Property(entity => entity.Longitude)
            .HasPrecision(9, 6);

        modelBuilder.Entity<Conversation>()
            .Property(entity => entity.Title)
            .HasMaxLength(100);

        modelBuilder.Entity<Message>()
            .Property(entity => entity.MediaMimeType)
            .HasMaxLength(100);

        modelBuilder.Entity<Review>()
            .Property(entity => entity.Comment)
            .HasMaxLength(1000);

        modelBuilder.Entity<Post>()
            .Property(entity => entity.Content)
            .HasMaxLength(2200);

        modelBuilder.Entity<PostMedia>()
            .Property(entity => entity.FileName)
            .HasMaxLength(255);
        modelBuilder.Entity<PostMedia>()
            .Property(entity => entity.MimeType)
            .HasMaxLength(100);

        modelBuilder.Entity<PostComment>()
            .Property(entity => entity.Content)
            .HasMaxLength(1000);

        modelBuilder.Entity<Notification>()
            .Property(entity => entity.Title)
            .HasMaxLength(150);
        modelBuilder.Entity<Notification>()
            .Property(entity => entity.Body)
            .HasMaxLength(1000);

        modelBuilder.Entity<Badge>()
            .Property(entity => entity.Code)
            .HasMaxLength(100);
        modelBuilder.Entity<Badge>()
            .Property(entity => entity.Name)
            .HasMaxLength(100);
        modelBuilder.Entity<Badge>()
            .Property(entity => entity.Description)
            .HasMaxLength(1000);
        modelBuilder.Entity<Badge>()
            .Property(entity => entity.IconPath)
            .HasMaxLength(500);

        modelBuilder.Entity<Report>()
            .Property(entity => entity.Description)
            .HasMaxLength(2000);
        modelBuilder.Entity<Report>()
            .Property(entity => entity.ResolutionNote)
            .HasMaxLength(2000);

        modelBuilder.Entity<ReportReason>()
            .Property(entity => entity.Code)
            .HasMaxLength(100);
        modelBuilder.Entity<ReportReason>()
            .Property(entity => entity.Name)
            .HasMaxLength(100);
        modelBuilder.Entity<ReportReason>()
            .Property(entity => entity.Description)
            .HasMaxLength(1000);
    }

    private static void ConfigureDefaults(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>()
            .Property(entity => entity.LikeCount)
            .HasDefaultValue(0);
        modelBuilder.Entity<Post>()
            .Property(entity => entity.CommentCount)
            .HasDefaultValue(0);
        modelBuilder.Entity<Post>()
            .Property(entity => entity.MediaCount)
            .HasDefaultValue((short)0);

        modelBuilder.Entity<PostComment>()
            .Property(entity => entity.LikeCount)
            .HasDefaultValue(0);
        modelBuilder.Entity<PostComment>()
            .Property(entity => entity.ReplyCount)
            .HasDefaultValue(0);
    }

    private static void ConfigureSmallIntColumns(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(entity => entity.Status)
            .HasColumnType("smallint");
        modelBuilder.Entity<UserProfile>().Property(entity => entity.Gender)
            .HasColumnType("smallint");
        modelBuilder.Entity<UserSport>().Property(entity => entity.SkillLevel)
            .HasColumnType("smallint");
        modelBuilder.Entity<UserDevice>().Property(entity => entity.Platform)
            .HasColumnType("smallint");
        modelBuilder.Entity<Event>().Property(entity => entity.Status)
            .HasColumnType("smallint");
        modelBuilder.Entity<EventParticipant>().Property(entity => entity.Status)
            .HasColumnType("smallint");
        modelBuilder.Entity<Conversation>().Property(entity => entity.Type)
            .HasColumnType("smallint");
        modelBuilder.Entity<ConversationMember>().Property(entity => entity.Role)
            .HasColumnType("smallint");
        modelBuilder.Entity<Message>().Property(entity => entity.MessageType)
            .HasColumnType("smallint");
        modelBuilder.Entity<Review>().Property(entity => entity.Rating)
            .HasColumnType("smallint");
        modelBuilder.Entity<Friendship>().Property(entity => entity.Status)
            .HasColumnType("smallint");
        modelBuilder.Entity<Post>().Property(entity => entity.MediaCount)
            .HasColumnType("smallint");
        modelBuilder.Entity<PostMedia>().Property(entity => entity.MediaType)
            .HasColumnType("smallint");
        modelBuilder.Entity<PostMedia>().Property(entity => entity.DisplayOrder)
            .HasColumnType("smallint");
        modelBuilder.Entity<Notification>().Property(entity => entity.NotificationType)
            .HasColumnType("smallint");
        modelBuilder.Entity<Notification>().Property(entity => entity.EntityType)
            .HasColumnType("smallint");
        modelBuilder.Entity<NotificationSetting>()
            .Property(entity => entity.NotificationType)
            .HasColumnType("smallint");
        modelBuilder.Entity<Badge>().Property(entity => entity.Category)
            .HasColumnType("smallint");
        modelBuilder.Entity<Badge>().Property(entity => entity.Rarity)
            .HasColumnType("smallint");
        modelBuilder.Entity<Badge>().Property(entity => entity.DisplayOrder)
            .HasColumnType("smallint");
        modelBuilder.Entity<Report>().Property(entity => entity.EntityType)
            .HasColumnType("smallint");
        modelBuilder.Entity<Report>().Property(entity => entity.Status)
            .HasColumnType("smallint");
        modelBuilder.Entity<ReportReason>().Property(entity => entity.DisplayOrder)
            .HasColumnType("smallint");
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>()
            .HasOne<User>()
            .WithOne(user => user.UserProfile)
            .HasForeignKey<UserProfile>(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserStatistics>()
            .HasOne<User>()
            .WithOne(user => user.Statistics)
            .HasForeignKey<UserStatistics>(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSport>()
            .HasOne<User>()
            .WithMany(user => user.Sports)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSport>()
            .HasOne<Sport>()
            .WithMany()
            .HasForeignKey(entity => entity.SportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserSession>()
            .HasOne<User>()
            .WithMany(user => user.Sessions)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSession>()
            .HasOne<UserDevice>()
            .WithMany()
            .HasForeignKey(entity => entity.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserDevice>()
            .HasOne<User>()
            .WithMany(user => user.Devices)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSavedLocation>()
            .HasOne<User>()
            .WithMany(user => user.SavedLocations)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Event>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Event>()
            .HasOne<Sport>()
            .WithMany()
            .HasForeignKey(entity => entity.SportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventParticipant>()
            .HasOne<Event>()
            .WithMany(entity => entity.Participants)
            .HasForeignKey(entity => entity.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventParticipant>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventWaitlist>()
            .HasOne<Event>()
            .WithMany(entity => entity.Waitlist)
            .HasForeignKey(entity => entity.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventWaitlist>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventReminderDispatch>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => entity.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventReminderDispatch>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => entity.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConversationMember>()
            .HasOne<Conversation>()
            .WithMany(entity => entity.Members)
            .HasForeignKey(entity => entity.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConversationMember>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(entity => entity.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne<Message>()
            .WithMany()
            .HasForeignKey(entity => entity.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => entity.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.ReviewedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.AddresseeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.BlockedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Post>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PostMedia>()
            .HasOne<Post>()
            .WithMany(entity => entity.Media)
            .HasForeignKey(entity => entity.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PostLike>()
            .HasOne<Post>()
            .WithMany()
            .HasForeignKey(entity => entity.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PostLike>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PostComment>()
            .HasOne<Post>()
            .WithMany()
            .HasForeignKey(entity => entity.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PostComment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PostComment>()
            .HasOne<PostComment>()
            .WithMany()
            .HasForeignKey(entity => entity.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NotificationSetting>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBadge>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserBadge>()
            .HasOne<Badge>()
            .WithMany()
            .HasForeignKey(entity => entity.BadgeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.ReporterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne<ReportReason>()
            .WithMany()
            .HasForeignKey(entity => entity.ReportReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
