using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Features.Quests;
using Sportner.Application.Features.Social.Posts.CreatePost;
using Sportner.Application.Features.Social.Posts.ListEventPosts;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Sports;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Social;

public sealed class PostEventLinkTests
{
    [Fact]
    public async Task Organizer_CanShareToEvent_AndPostCarriesEventSnippet()
    {
        var (db, time, organizerId, _, eventId) = await SeedAsync();
        await using var _db = db;

        var result = await CreateHandler(db, time, organizerId).Handle(
            new CreatePostCommand("Harika maçtı", Media: null, EventId: eventId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Event.Should().NotBeNull();
        result.Value.Event!.Id.Should().Be(eventId);
        result.Value.Event.SportSlug.Should().Be("football");

        var listed = await new ListEventPostsQueryHandler(
                db, new TestCurrentUser(organizerId), new Mock<IFileStorage>().Object)
            .Handle(new ListEventPostsQuery(eventId), CancellationToken.None);

        listed.Value!.Items.Should().ContainSingle(post => post.Id == result.Value.Id);
    }

    [Fact]
    public async Task NonParticipant_CannotShareToEvent()
    {
        var (db, time, _, strangerId, eventId) = await SeedAsync();
        await using var _db = db;

        var result = await CreateHandler(db, time, strangerId).Handle(
            new CreatePostCommand("Ben de oradaydım", Media: null, EventId: eventId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Post.EventNotLinkable");
    }

    [Fact]
    public async Task PostWithoutEvent_StillWorks()
    {
        var (db, time, _, strangerId, _) = await SeedAsync();
        await using var _db = db;

        var result = await CreateHandler(db, time, strangerId).Handle(
            new CreatePostCommand("Bağımsız gönderi", Media: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Event.Should().BeNull();
    }

    private static async Task<(AppDbContext Db, FakeTimeProvider Time, Guid OrganizerId, Guid StrangerId, Guid EventId)> SeedAsync()
    {
        var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var stranger = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Football", 1, time.GetUtcNow(), "football");
        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Match",
            time.GetUtcNow().AddHours(2),
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 8);
        @event.Publish(time.GetUtcNow());

        db.Users.AddRange(organizer, stranger);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        return (db, time, organizer.Id, stranger.Id, @event.Id);
    }

    private static CreatePostCommandHandler CreateHandler(
        AppDbContext db,
        TimeProvider time,
        Guid userId) =>
        new(
            db,
            new TestCurrentUser(userId),
            time,
            new Mock<IFileStorage>().Object,
            new Mock<IBadgeAwarder>().Object,
            new Mock<IQuestProgressTracker>().Object,
            new Mock<INotificationPublisher>().Object);
}
