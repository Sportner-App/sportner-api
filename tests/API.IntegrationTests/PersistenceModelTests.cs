using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Base;
using Sportner.Domain.Events;
using Sportner.Domain.Messaging;
using Sportner.Domain.Moderation;
using Sportner.Domain.Notifications;
using Sportner.Domain.Reviews;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.API.IntegrationTests;

public class PersistenceModelTests
{
    [Fact]
    public void AppDbContext_RegistersEveryConcreteDomainEntity()
    {
        var expectedEntityTypes = typeof(BaseEntity).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract
                && typeof(BaseEntity).IsAssignableFrom(type))
            .ToHashSet();

        var registeredDbSetTypes = typeof(AppDbContext)
            .GetProperties()
            .Where(property =>
                property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => property.PropertyType.GenericTypeArguments[0])
            .ToHashSet();

        registeredDbSetTypes.Should().BeEquivalentTo(expectedEntityTypes);
    }

    [Fact]
    public void AppDbContext_BuildsConventionModelForEveryRegisteredEntity()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=sportner_model_test;"
                + "Username=postgres;Password=postgres")
            .Options;

        using var context = new AppDbContext(options);

        var registeredDbSetTypes = typeof(AppDbContext)
            .GetProperties()
            .Where(property =>
                property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => property.PropertyType.GenericTypeArguments[0])
            .ToHashSet();

        var modelEntityTypes = context.Model
            .GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .ToHashSet();

        modelEntityTypes.Should().Contain(registeredDbSetTypes);

        var usernameChangedAt = context.Model
            .FindEntityType(typeof(Profile))!
            .FindProperty(nameof(Profile.UsernameChangedAt));

        usernameChangedAt.Should().NotBeNull();
        usernameChangedAt!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void AppDbContext_ContainsEveryDocumentedUniqueConstraint()
    {
        var expectedIndexes = new (Type EntityType, string[] Properties)[]
        {
            (typeof(User), [nameof(User.PhoneNumber)]),
            (typeof(Profile), [nameof(Profile.UserId)]),
            (typeof(Profile), [nameof(Profile.Username)]),
            (typeof(Sport), [nameof(Sport.Name)]),
            (typeof(Sport), [nameof(Sport.Slug)]),
            (typeof(UserSport), [nameof(UserSport.UserId), nameof(UserSport.SportId)]),
            (typeof(UserStatistics), [nameof(UserStatistics.UserId)]),
            (typeof(UserDevice), [nameof(UserDevice.DeviceIdentifier)]),
            (typeof(EventParticipant),
                [nameof(EventParticipant.EventId), nameof(EventParticipant.UserId)]),
            (typeof(EventWaitlist),
                [nameof(EventWaitlist.EventId), nameof(EventWaitlist.UserId)]),
            (typeof(EventWaitlist),
                [nameof(EventWaitlist.EventId), nameof(EventWaitlist.Position)]),
            (typeof(Conversation), [nameof(Conversation.EventId)]),
            (typeof(ConversationMember),
                [nameof(ConversationMember.ConversationId), nameof(ConversationMember.UserId)]),
            (typeof(Review),
                [nameof(Review.EventId), nameof(Review.ReviewerUserId), nameof(Review.ReviewedUserId)]),
            (typeof(Friendship),
                [nameof(Friendship.RequesterUserId), nameof(Friendship.AddresseeUserId)]),
            (typeof(PostMedia), [nameof(PostMedia.PostId), nameof(PostMedia.DisplayOrder)]),
            (typeof(PostLike), [nameof(PostLike.PostId), nameof(PostLike.UserId)]),
            (typeof(NotificationSetting),
                [nameof(NotificationSetting.UserId), nameof(NotificationSetting.NotificationType)]),
            (typeof(Badge), [nameof(Badge.Code)]),
            (typeof(UserBadge), [nameof(UserBadge.UserId), nameof(UserBadge.BadgeId)]),
            (typeof(Report),
                [nameof(Report.ReporterUserId), nameof(Report.EntityType), nameof(Report.EntityId)]),
            (typeof(ReportReason), [nameof(ReportReason.Code)])
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=sportner_model_test;"
                + "Username=postgres;Password=postgres")
            .Options;

        using var context = new AppDbContext(options);

        foreach (var (entityType, properties) in expectedIndexes)
        {
            var modelEntity = context.Model.FindEntityType(entityType);

            modelEntity.Should().NotBeNull();
            modelEntity!.GetIndexes().Should().Contain(
                index =>
                    index.IsUnique
                    && index.Properties
                        .Select(property => property.Name)
                        .SequenceEqual(properties),
                $"the documented unique constraint on {entityType.Name}"
                + $"({string.Join(", ", properties)}) must be in the EF model");
        }
    }

    [Fact]
    public void AppDbContext_AppliesDocumentedPropertyFacets()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=sportner_model_test;"
                + "Username=postgres;Password=postgres")
            .Options;

        using var context = new AppDbContext(options);

        var maxLengths = new (Type EntityType, string Property, int Length)[]
        {
            (typeof(User), nameof(User.PhoneNumber), 20),
            (typeof(Profile), nameof(Profile.Username), 30),
            (typeof(Profile), nameof(Profile.FirstName), 50),
            (typeof(Profile), nameof(Profile.LastName), 50),
            (typeof(Profile), nameof(Profile.Bio), 500),
            (typeof(Profile), nameof(Profile.City), 100),
            (typeof(Sport), nameof(Sport.Name), 100),
            (typeof(Sport), nameof(Sport.Slug), 100),
            (typeof(UserSession), nameof(UserSession.IpAddress), 45),
            (typeof(UserDevice), nameof(UserDevice.DeviceName), 100),
            (typeof(UserDevice), nameof(UserDevice.DeviceIdentifier), 255),
            (typeof(UserDevice), nameof(UserDevice.AppVersion), 30),
            (typeof(UserDevice), nameof(UserDevice.OsVersion), 30),
            (typeof(UserSavedLocation), nameof(UserSavedLocation.Title), 100),
            (typeof(UserSavedLocation), nameof(UserSavedLocation.City), 100),
            (typeof(UserSavedLocation), nameof(UserSavedLocation.District), 100),
            (typeof(Event), nameof(Event.Title), 150),
            (typeof(Conversation), nameof(Conversation.Title), 100),
            (typeof(Message), nameof(Message.MediaMimeType), 100),
            (typeof(Review), nameof(Review.Comment), 1000),
            (typeof(Post), nameof(Post.Content), 2200),
            (typeof(PostMedia), nameof(PostMedia.FileName), 255),
            (typeof(PostMedia), nameof(PostMedia.MimeType), 100),
            (typeof(PostComment), nameof(PostComment.Content), 1000),
            (typeof(Notification), nameof(Notification.Title), 150),
            (typeof(Notification), nameof(Notification.Body), 1000),
            (typeof(Badge), nameof(Badge.Code), 100),
            (typeof(Badge), nameof(Badge.Name), 100),
            (typeof(Badge), nameof(Badge.Description), 1000),
            (typeof(Badge), nameof(Badge.IconPath), 500),
            (typeof(Report), nameof(Report.Description), 2000),
            (typeof(Report), nameof(Report.ResolutionNote), 2000),
            (typeof(ReportReason), nameof(ReportReason.Code), 100),
            (typeof(ReportReason), nameof(ReportReason.Name), 100),
            (typeof(ReportReason), nameof(ReportReason.Description), 1000)
        };

        foreach (var (entityType, propertyName, length) in maxLengths)
        {
            context.Model.FindEntityType(entityType)!
                .FindProperty(propertyName)!
                .GetMaxLength()
                .Should()
                .Be(length);
        }

        var precisions = new (Type EntityType, string Property, int Precision, int Scale)[]
        {
            (typeof(Profile), nameof(Profile.AverageRating), 3, 2),
            (typeof(UserStatistics), nameof(UserStatistics.AttendanceRate), 5, 2),
            (typeof(UserStatistics), nameof(UserStatistics.AverageRating), 3, 2),
            (typeof(UserSavedLocation), nameof(UserSavedLocation.Latitude), 9, 6),
            (typeof(UserSavedLocation), nameof(UserSavedLocation.Longitude), 9, 6),
            (typeof(Event), nameof(Event.Latitude), 9, 6),
            (typeof(Event), nameof(Event.Longitude), 9, 6)
        };

        foreach (var (entityType, propertyName, precision, scale) in precisions)
        {
            var property = context.Model.FindEntityType(entityType)!
                .FindProperty(propertyName)!;

            property.GetPrecision().Should().Be(precision);
            property.GetScale().Should().Be(scale);
        }

        var smallInts = new (Type EntityType, string Property)[]
        {
            (typeof(User), nameof(User.Status)),
            (typeof(Profile), nameof(Profile.Gender)),
            (typeof(UserSport), nameof(UserSport.SkillLevel)),
            (typeof(UserDevice), nameof(UserDevice.Platform)),
            (typeof(Event), nameof(Event.Status)),
            (typeof(EventParticipant), nameof(EventParticipant.Status)),
            (typeof(Conversation), nameof(Conversation.Type)),
            (typeof(ConversationMember), nameof(ConversationMember.Role)),
            (typeof(Message), nameof(Message.MessageType)),
            (typeof(Review), nameof(Review.Rating)),
            (typeof(Friendship), nameof(Friendship.Status)),
            (typeof(Post), nameof(Post.MediaCount)),
            (typeof(PostMedia), nameof(PostMedia.MediaType)),
            (typeof(PostMedia), nameof(PostMedia.DisplayOrder)),
            (typeof(Notification), nameof(Notification.NotificationType)),
            (typeof(Notification), nameof(Notification.EntityType)),
            (typeof(NotificationSetting), nameof(NotificationSetting.NotificationType)),
            (typeof(Badge), nameof(Badge.Category)),
            (typeof(Badge), nameof(Badge.Rarity)),
            (typeof(Badge), nameof(Badge.DisplayOrder)),
            (typeof(Report), nameof(Report.EntityType)),
            (typeof(Report), nameof(Report.Status)),
            (typeof(ReportReason), nameof(ReportReason.DisplayOrder))
        };

        foreach (var (entityType, propertyName) in smallInts)
        {
            context.Model.FindEntityType(entityType)!
                .FindProperty(propertyName)!
                .GetColumnType()
                .Should()
                .Be("smallint");
        }

        context.Model.FindEntityType(typeof(Post))!
            .FindProperty(nameof(Post.LikeCount))!
            .GetDefaultValue()
            .Should()
            .Be(0);
        context.Model.FindEntityType(typeof(Post))!
            .FindProperty(nameof(Post.CommentCount))!
            .GetDefaultValue()
            .Should()
            .Be(0);
        context.Model.FindEntityType(typeof(Post))!
            .FindProperty(nameof(Post.MediaCount))!
            .GetDefaultValue()
            .Should()
            .Be((short)0);
        context.Model.FindEntityType(typeof(PostComment))!
            .FindProperty(nameof(PostComment.LikeCount))!
            .GetDefaultValue()
            .Should()
            .Be(0);
        context.Model.FindEntityType(typeof(PostComment))!
            .FindProperty(nameof(PostComment.ReplyCount))!
            .GetDefaultValue()
            .Should()
            .Be(0);
    }
}
