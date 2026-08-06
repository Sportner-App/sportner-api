using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Badges;
using Sportner.Domain.Events;
using Sportner.Domain.Messaging;
using Sportner.Domain.Moderation;
using Sportner.Domain.Notifications;
using Sportner.Domain.Reviews;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence.Configurations;

namespace Sportner.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<Sport> Sports => Set<Sport>();

    public DbSet<UserSport> UserSports => Set<UserSport>();

    public DbSet<UserStatistics> UserStatistics => Set<UserStatistics>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    public DbSet<UserSavedLocation> UserSavedLocations => Set<UserSavedLocation>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();

    public DbSet<EventWaitlist> EventWaitlists => Set<EventWaitlist>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Friendship> Friendships => Set<Friendship>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<PostMedia> PostMedia => Set<PostMedia>();

    public DbSet<PostLike> PostLikes => Set<PostLike>();

    public DbSet<PostComment> PostComments => Set<PostComment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();

    public DbSet<Badge> Badges => Set<Badge>();

    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<ReportReason> ReportReasons => Set<ReportReason>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyDocumentedConstraints();
        base.OnModelCreating(modelBuilder);
    }
}
