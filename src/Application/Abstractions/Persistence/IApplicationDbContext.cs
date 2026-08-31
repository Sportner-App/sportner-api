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

namespace Sportner.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<City> Cities { get; }
    DbSet<Sport> Sports { get; }
    DbSet<UserSport> UserSports { get; }
    DbSet<UserStatistics> UserStatistics { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<UserDevice> UserDevices { get; }
    DbSet<UserSavedLocation> UserSavedLocations { get; }
    DbSet<Event> Events { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationMember> OrganizationMembers { get; }
    DbSet<EventQuestion> EventQuestions { get; }
    DbSet<EventParticipant> EventParticipants { get; }
    DbSet<EventWaitlist> EventWaitlists { get; }
    DbSet<EventReminderDispatch> EventReminderDispatches { get; }
    DbSet<EventParticipantRemoval> EventParticipantRemovals { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationMember> ConversationMembers { get; }
    DbSet<Message> Messages { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Friendship> Friendships { get; }
    DbSet<UserBlock> UserBlocks { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostMedia> PostMedia { get; }
    DbSet<PostLike> PostLikes { get; }
    DbSet<PostComment> PostComments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationSetting> NotificationSettings { get; }
    DbSet<NotificationDeliveryOutbox> NotificationDeliveryOutbox { get; }
    DbSet<Badge> Badges { get; }
    DbSet<UserBadge> UserBadges { get; }
    DbSet<Quest> Quests { get; }
    DbSet<UserQuest> UserQuests { get; }
    DbSet<Album> Albums { get; }
    DbSet<AlbumMedia> AlbumMedia { get; }
    DbSet<Report> Reports { get; }
    DbSet<ReportReason> ReportReasons { get; }
    DbSet<AppFeedback> AppFeedbacks { get; }

    /// <summary>
    /// Forces EF to insert an entity that already has a client-generated key
    /// (otherwise some providers track it as <c>Modified</c>).
    /// </summary>
    void MarkAsAdded<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
