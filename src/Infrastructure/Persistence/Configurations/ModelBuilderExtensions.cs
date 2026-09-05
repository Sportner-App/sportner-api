using Microsoft.EntityFrameworkCore;
using Sportner.Domain.Badges;
using Sportner.Domain.Events;
using Sportner.Domain.Feedback;
using Sportner.Domain.Locations;
using Sportner.Domain.Messaging;
using Sportner.Domain.Moderation;
using Sportner.Domain.Notifications;
using Sportner.Domain.Organizations;
using Sportner.Domain.Quests;
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
        modelBuilder.Entity<City>().HasIndex(entity => entity.PlateCode).IsUnique();
        modelBuilder.Entity<City>().HasIndex(entity => entity.Name).IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(entity => entity.PhoneNumber)
            .IsUnique()
            .HasFilter("\"PhoneNumber\" IS NOT NULL");

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

        modelBuilder.Entity<SportCategory>()
            .HasIndex(entity => entity.Name)
            .IsUnique();

        modelBuilder.Entity<SportCategory>()
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

        modelBuilder.Entity<UserExternalLogin>()
            .HasIndex(entity => new { entity.Provider, entity.ProviderUserId })
            .IsUnique();

        modelBuilder.Entity<EventParticipant>()
            .HasIndex(entity => new { entity.EventId, entity.UserId })
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");

        modelBuilder.Entity<EventWaitlist>()
            .HasIndex(entity => new { entity.EventId, entity.UserId })
            .IsUnique();

        modelBuilder.Entity<EventWaitlist>()
            .HasIndex(entity => new { entity.EventId, entity.Position })
            .IsUnique();

        modelBuilder.Entity<EventReminderDispatch>()
            .HasIndex(entity => new { entity.EventId, entity.UserId, entity.WindowMinutes })
            .IsUnique();

        modelBuilder.Entity<EventParticipantRemoval>()
            .HasIndex(entity => entity.ParticipantId)
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

        modelBuilder.Entity<UserBlock>()
            .HasIndex(entity => new
            {
                entity.BlockerUserId,
                entity.BlockedUserId
            })
            .IsUnique();

        modelBuilder.Entity<PostMedia>()
            .HasIndex(entity => new { entity.PostId, entity.DisplayOrder })
            .IsUnique();

        modelBuilder.Entity<AlbumMedia>()
            .HasIndex(entity => new { entity.AlbumId, entity.DisplayOrder })
            .IsUnique();

        modelBuilder.Entity<PostLike>()
            .HasIndex(entity => new { entity.PostId, entity.UserId })
            .IsUnique();

        modelBuilder.Entity<NotificationSetting>()
            .HasIndex(entity => new { entity.UserId, entity.NotificationType })
            .IsUnique();

        modelBuilder.Entity<NotificationDeliveryOutbox>()
            .HasIndex(entity => new { entity.Status, entity.NextAttemptAt });

        modelBuilder.Entity<Badge>()
            .HasIndex(entity => entity.Code)
            .IsUnique();

        modelBuilder.Entity<UserBadge>()
            .HasIndex(entity => new { entity.UserId, entity.BadgeId })
            .IsUnique();

        modelBuilder.Entity<Quest>()
            .HasIndex(entity => entity.Code)
            .IsUnique();

        modelBuilder.Entity<UserQuest>()
            .HasIndex(entity => new { entity.UserId, entity.QuestId })
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

        modelBuilder.Entity<Organization>()
            .HasIndex(entity => entity.InviteCode)
            .IsUnique();

        modelBuilder.Entity<OrganizationMember>()
            .HasIndex(entity => new { entity.OrganizationId, entity.UserId })
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
        modelBuilder.Entity<Sport>().HasIndex(entity => entity.CategoryId);

        modelBuilder.Entity<SportCategory>().HasIndex(entity => entity.DisplayOrder);
        modelBuilder.Entity<SportCategory>().HasIndex(entity => entity.IsActive);

        modelBuilder.Entity<UserSport>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserSport>().HasIndex(entity => entity.SportId);
        modelBuilder.Entity<UserSport>().HasIndex(entity => entity.SkillLevel);

        modelBuilder.Entity<UserSession>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserSession>().HasIndex(entity => entity.DeviceId);
        modelBuilder.Entity<UserSession>().HasIndex(entity => entity.ExpiresAt);
        modelBuilder.Entity<UserSession>().HasIndex(entity => entity.RevokedAt);

        modelBuilder.Entity<UserDevice>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserDevice>().HasIndex(entity => entity.LastSeenAt);

        modelBuilder.Entity<UserExternalLogin>().HasIndex(entity => entity.UserId);

        modelBuilder.Entity<UserSavedLocation>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserSavedLocation>().HasIndex(entity => entity.City);

        modelBuilder.Entity<Event>().HasIndex(entity => entity.OrganizerUserId);
        modelBuilder.Entity<Event>().HasIndex(entity => entity.SportId);
        modelBuilder.Entity<Event>().HasIndex(entity => entity.EventDate);
        modelBuilder.Entity<Event>().HasIndex(entity => entity.Status);
        modelBuilder.Entity<Event>()
            .HasIndex(entity => new { entity.Status, entity.EventDate });
        modelBuilder.Entity<Event>()
            .HasIndex(entity => new { entity.Latitude, entity.Longitude });
        modelBuilder.Entity<Event>().HasIndex(entity => entity.SkillLevel);
        modelBuilder.Entity<Event>().HasIndex(entity => entity.IsPaid);
        modelBuilder.Entity<Event>().HasIndex(entity => entity.OrganizationId);

        modelBuilder.Entity<Organization>().HasIndex(entity => entity.FounderUserId);
        modelBuilder.Entity<Organization>().HasIndex(entity => entity.CityId);

        modelBuilder.Entity<OrganizationMember>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<OrganizationMember>()
            .HasIndex(entity => new { entity.OrganizationId, entity.Status });

        modelBuilder.Entity<EventQuestion>()
            .HasIndex(entity => new { entity.EventId, entity.CreatedAt });
        modelBuilder.Entity<EventQuestion>()
            .HasIndex(entity => new { entity.EventId, entity.ParentId });
        modelBuilder.Entity<EventQuestion>().HasIndex(entity => entity.ParentId);
        modelBuilder.Entity<EventQuestion>().HasIndex(entity => entity.AuthorUserId);

        modelBuilder.Entity<EventParticipant>().HasIndex(entity => entity.EventId);
        modelBuilder.Entity<EventParticipant>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<EventParticipant>().HasIndex(entity => entity.Status);
        modelBuilder.Entity<EventParticipant>().HasIndex(entity => entity.Kind);

        modelBuilder.Entity<EventWaitlist>().HasIndex(entity => entity.EventId);
        modelBuilder.Entity<EventWaitlist>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<EventWaitlist>().HasIndex(entity => entity.Position);

        modelBuilder.Entity<EventParticipantRemoval>().HasIndex(entity => entity.EventId);
        modelBuilder.Entity<EventParticipantRemoval>().HasIndex(entity => entity.OrganizerUserId);
        modelBuilder.Entity<EventParticipantRemoval>().HasIndex(entity => entity.RemovedUserId);
        modelBuilder.Entity<EventParticipantRemoval>().HasIndex(entity => entity.ReportReasonId);
        modelBuilder.Entity<EventParticipantRemoval>().HasIndex(entity => entity.CreatedAt);

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

        modelBuilder.Entity<UserBlock>()
            .HasIndex(entity => new { entity.BlockerUserId, entity.CreatedAt });
        modelBuilder.Entity<UserBlock>().HasIndex(entity => entity.BlockedUserId);

        modelBuilder.Entity<Post>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<Post>().HasIndex(entity => entity.CreatedAt);
        modelBuilder.Entity<Post>()
            .HasIndex(entity => new { entity.UserId, entity.CreatedAt });

        modelBuilder.Entity<PostMedia>().HasIndex(entity => entity.PostId);

        modelBuilder.Entity<Album>().HasIndex(entity => entity.OwnerUserId);
        modelBuilder.Entity<Album>().HasIndex(entity => entity.EventId);
        modelBuilder.Entity<Album>().HasIndex(entity => entity.Kind);
        modelBuilder.Entity<Album>().HasIndex(entity => entity.Visibility);
        modelBuilder.Entity<AlbumMedia>().HasIndex(entity => entity.AlbumId);
        modelBuilder.Entity<AlbumMedia>().HasIndex(entity => entity.UploadedByUserId);

        modelBuilder.Entity<PostLike>().HasIndex(entity => entity.PostId);
        modelBuilder.Entity<PostLike>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<PostLike>()
            .HasIndex(entity => new { entity.UserId, entity.CreatedAt });

        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.PostId);
        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.ParentCommentId);
        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.ReplyToUserId);
        modelBuilder.Entity<PostComment>().HasIndex(entity => entity.CreatedAt);
        modelBuilder.Entity<PostComment>()
            .HasIndex(entity => new { entity.PostId, entity.CreatedAt });

        modelBuilder.Entity<Notification>().HasIndex(entity => entity.RecipientUserId);
        modelBuilder.Entity<Notification>().HasIndex(entity => entity.IsRead);
        modelBuilder.Entity<Notification>().HasIndex(entity => entity.CreatedAt);
        modelBuilder.Entity<Notification>().HasIndex(entity => entity.NotificationType);

        modelBuilder.Entity<NotificationSetting>().HasIndex(entity => entity.UserId);

        modelBuilder.Entity<NotificationDeliveryOutbox>().HasIndex(entity => entity.RecipientUserId);
        modelBuilder.Entity<NotificationDeliveryOutbox>().HasIndex(entity => entity.NotificationId);
        modelBuilder.Entity<NotificationDeliveryOutbox>().HasIndex(entity => entity.CreatedAt);

        modelBuilder.Entity<Badge>().HasIndex(entity => entity.Category);
        modelBuilder.Entity<Badge>().HasIndex(entity => entity.Rarity);
        modelBuilder.Entity<Badge>().HasIndex(entity => entity.IsActive);

        modelBuilder.Entity<UserBadge>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserBadge>().HasIndex(entity => entity.BadgeId);
        modelBuilder.Entity<UserBadge>().HasIndex(entity => entity.EarnedAt);

        modelBuilder.Entity<Quest>().HasIndex(entity => entity.IsActive);
        modelBuilder.Entity<Quest>().HasIndex(entity => entity.MetricCode);
        modelBuilder.Entity<Quest>().HasIndex(entity => entity.SortOrder);
        modelBuilder.Entity<Quest>().HasIndex(entity => entity.RewardBadgeId);

        modelBuilder.Entity<UserQuest>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<UserQuest>().HasIndex(entity => entity.QuestId);
        modelBuilder.Entity<UserQuest>().HasIndex(entity => entity.Status);
        modelBuilder.Entity<UserQuest>().HasIndex(entity => new { entity.UserId, entity.Status });

        modelBuilder.Entity<Report>()
            .HasIndex(entity => new { entity.EntityType, entity.EntityId });
        modelBuilder.Entity<Report>().HasIndex(entity => entity.ReporterUserId);
        modelBuilder.Entity<Report>().HasIndex(entity => entity.Status);
        modelBuilder.Entity<Report>().HasIndex(entity => entity.CreatedAt);

        modelBuilder.Entity<ReportReason>().HasIndex(entity => entity.DisplayOrder);
        modelBuilder.Entity<ReportReason>().HasIndex(entity => entity.IsActive);

        modelBuilder.Entity<AppFeedback>().HasIndex(entity => entity.UserId);
        modelBuilder.Entity<AppFeedback>().HasIndex(entity => entity.CreatedAt);
        modelBuilder.Entity<AppFeedback>()
            .HasIndex(entity => new { entity.UserId, entity.CreatedAt });
    }

    private static void ConfigurePropertyMappings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>().Property(entity => entity.PlateCode).HasColumnType("smallint");
        modelBuilder.Entity<City>().Property(entity => entity.Name).HasMaxLength(100);
        modelBuilder.Entity<User>()
            .Property(entity => entity.PhoneNumber)
            .HasMaxLength(20);

        modelBuilder.Entity<User>()
            .Property(entity => entity.PasswordHash)
            .HasMaxLength(500);

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

        modelBuilder.Entity<SportCategory>()
            .Property(entity => entity.Name)
            .HasMaxLength(100);
        modelBuilder.Entity<SportCategory>()
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
        modelBuilder.Entity<Event>()
            .Property(entity => entity.FeeAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<EventQuestion>()
            .Property(entity => entity.Content)
            .HasMaxLength(EventQuestion.MaxContentLength);

        modelBuilder.Entity<EventParticipant>()
            .Property(entity => entity.GuestFirstName)
            .HasMaxLength(50);
        modelBuilder.Entity<EventParticipant>()
            .Property(entity => entity.GuestLastName)
            .HasMaxLength(50);

        modelBuilder.Entity<EventParticipantRemoval>()
            .Property(entity => entity.Note)
            .HasMaxLength(EventParticipantRemoval.NoteMaxLength);

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

        modelBuilder.Entity<Album>()
            .Property(entity => entity.Title)
            .HasMaxLength(150);
        modelBuilder.Entity<Album>()
            .Property(entity => entity.Description)
            .HasMaxLength(1000);
        modelBuilder.Entity<AlbumMedia>()
            .Property(entity => entity.StoragePath)
            .HasMaxLength(500);
        modelBuilder.Entity<AlbumMedia>()
            .Property(entity => entity.FileName)
            .HasMaxLength(255);
        modelBuilder.Entity<AlbumMedia>()
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

        modelBuilder.Entity<Quest>()
            .Property(entity => entity.Code)
            .HasMaxLength(100);
        modelBuilder.Entity<Quest>()
            .Property(entity => entity.Title)
            .HasMaxLength(150);
        modelBuilder.Entity<Quest>()
            .Property(entity => entity.Description)
            .HasMaxLength(1000);
        modelBuilder.Entity<Quest>()
            .Property(entity => entity.MetricCode)
            .HasMaxLength(100);

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

        modelBuilder.Entity<AppFeedback>()
            .Property(entity => entity.Content)
            .HasMaxLength(AppFeedback.MaxContentLength);

        modelBuilder.Entity<Organization>()
            .Property(entity => entity.Name)
            .HasMaxLength(Organization.NameMaxLength);
        modelBuilder.Entity<Organization>()
            .Property(entity => entity.Description)
            .HasMaxLength(Organization.DescriptionMaxLength);
        modelBuilder.Entity<Organization>()
            .Property(entity => entity.InviteCode)
            .HasMaxLength(Organization.InviteCodeLength);
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
        modelBuilder.Entity<Post>()
            .Property(entity => entity.IsHidden)
            .HasDefaultValue(false);

        modelBuilder.Entity<PostComment>()
            .Property(entity => entity.LikeCount)
            .HasDefaultValue(0);
        modelBuilder.Entity<PostComment>()
            .Property(entity => entity.ReplyCount)
            .HasDefaultValue(0);
        modelBuilder.Entity<PostComment>()
            .Property(entity => entity.IsHidden)
            .HasDefaultValue(false);

        modelBuilder.Entity<EventQuestion>()
            .Property(entity => entity.ReplyCount)
            .HasDefaultValue(0);

        modelBuilder.Entity<Event>()
            .Property(entity => entity.IsPaid)
            .HasDefaultValue(false);
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
        modelBuilder.Entity<UserExternalLogin>().Property(entity => entity.Provider)
            .HasColumnType("smallint");
        modelBuilder.Entity<Event>().Property(entity => entity.Status)
            .HasColumnType("smallint");
        modelBuilder.Entity<Event>().Property(entity => entity.SkillLevel)
            .HasColumnType("smallint");
        modelBuilder.Entity<EventParticipant>().Property(entity => entity.Status)
            .HasColumnType("smallint");
        modelBuilder.Entity<EventParticipant>().Property(entity => entity.Kind)
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
        modelBuilder.Entity<NotificationDeliveryOutbox>()
            .Property(entity => entity.Channel)
            .HasColumnType("smallint");
        modelBuilder.Entity<NotificationDeliveryOutbox>()
            .Property(entity => entity.Status)
            .HasColumnType("smallint");
        modelBuilder.Entity<NotificationDeliveryOutbox>()
            .Property(entity => entity.NotificationType)
            .HasColumnType("smallint");
        modelBuilder.Entity<NotificationDeliveryOutbox>()
            .Property(entity => entity.EntityType)
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
        modelBuilder.Entity<OrganizationMember>().Property(entity => entity.Role)
            .HasColumnType("smallint");
        modelBuilder.Entity<OrganizationMember>().Property(entity => entity.Status)
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

        modelBuilder.Entity<Sport>()
            .HasOne<SportCategory>()
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
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

        modelBuilder.Entity<UserExternalLogin>()
            .HasOne<User>()
            .WithMany(user => user.ExternalLogins)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Event>()
            .ToTable(table =>
                table.HasCheckConstraint(
                    "CK_Events_Fee",
                    "(\"IsPaid\" = FALSE AND \"FeeAmount\" IS NULL) OR (\"IsPaid\" = TRUE AND \"FeeAmount\" IS NOT NULL AND \"FeeAmount\" > 0)"));

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

        modelBuilder.Entity<Event>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Organization>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.FounderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Organization>()
            .HasOne<City>()
            .WithMany()
            .HasForeignKey(entity => entity.CityId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<OrganizationMember>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrganizationMember>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventQuestion>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => entity.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventQuestion>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventQuestion>()
            .HasOne<EventQuestion>()
            .WithMany()
            .HasForeignKey(entity => entity.ParentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<EventQuestion>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.ReplyToUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

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

        modelBuilder.Entity<EventParticipantRemoval>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => entity.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventParticipantRemoval>()
            .HasOne<EventParticipant>()
            .WithMany()
            .HasForeignKey(entity => entity.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventParticipantRemoval>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.OrganizerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventParticipantRemoval>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.RemovedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventParticipantRemoval>()
            .HasOne<ReportReason>()
            .WithMany()
            .HasForeignKey(entity => entity.ReportReasonId)
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

        modelBuilder.Entity<ConversationMember>()
            .HasOne<Message>()
            .WithMany()
            .HasForeignKey(entity => entity.LastReadMessageId)
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<UserBlock>()
            .ToTable(table =>
                table.HasCheckConstraint(
                    "CK_UserBlocks_NotSelf",
                    "\"BlockerUserId\" <> \"BlockedUserId\""));

        modelBuilder.Entity<UserBlock>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.BlockerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserBlock>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.BlockedUserId)
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

        modelBuilder.Entity<Album>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Album>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(entity => entity.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AlbumMedia>()
            .HasOne<Album>()
            .WithMany(entity => entity.Media)
            .HasForeignKey(entity => entity.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AlbumMedia>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<PostComment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.ReplyToUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

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

        modelBuilder.Entity<NotificationDeliveryOutbox>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotificationDeliveryOutbox>()
            .HasOne<Notification>()
            .WithMany()
            .HasForeignKey(entity => entity.NotificationId)
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<Quest>()
            .HasOne<Badge>()
            .WithMany()
            .HasForeignKey(entity => entity.RewardBadgeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserQuest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserQuest>()
            .HasOne<Quest>()
            .WithMany()
            .HasForeignKey(entity => entity.QuestId)
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

        modelBuilder.Entity<AppFeedback>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
