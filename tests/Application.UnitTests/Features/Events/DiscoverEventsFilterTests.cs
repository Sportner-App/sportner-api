using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events.DiscoverEvents;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class DiscoverEventsFilterTests
{
    [Fact]
    public async Task Handle_FiltersByOverlappingAgeRangeAndOrganizerGender()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero));
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var femaleOrganizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var maleOrganizer = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        femaleOrganizer.AttachUserProfile(CreateProfile(
            femaleOrganizer.Id, "female", 1, time.GetUtcNow()));
        maleOrganizer.AttachUserProfile(CreateProfile(
            maleOrganizer.Id, "male", 2, time.GetUtcNow()));

        var matching = CreateEvent(
            femaleOrganizer.Id, sport.Id, "18-30", 18, 30, time);
        var nonMatching = CreateEvent(
            maleOrganizer.Id, sport.Id, "40-60", 40, 60, time);

        db.Users.AddRange(femaleOrganizer, maleOrganizer);
        db.Sports.Add(sport);
        db.Events.AddRange(matching, nonMatching);
        await db.SaveChangesAsync();

        var handler = new DiscoverEventsQueryHandler(db, new TestCurrentUser(null), time);
        var result = await handler.Handle(
            new DiscoverEventsQuery(
                MinParticipantAge: 25,
                MaxParticipantAge: 35,
                OrganizerGender: 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(item => item.Id == matching.Id);
    }

    [Fact]
    public async Task Handle_FiltersByExactSkillLevelWhenProvided()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero));
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());

        var intermediate = CreateEvent(
            organizer.Id, sport.Id, "Orta", 18, 40, time, SkillLevel.Intermediate);
        var beginner = CreateEvent(
            organizer.Id, sport.Id, "Baslangic", 18, 40, time, SkillLevel.Beginner);
        var unlabeled = CreateEvent(
            organizer.Id, sport.Id, "Serbest", 18, 40, time);

        db.Users.Add(organizer);
        db.Sports.Add(sport);
        db.Events.AddRange(intermediate, beginner, unlabeled);
        await db.SaveChangesAsync();

        var handler = new DiscoverEventsQueryHandler(db, new TestCurrentUser(null), time);
        var result = await handler.Handle(
            new DiscoverEventsQuery(SkillLevel: (short)SkillLevel.Intermediate),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(item => item.Id == intermediate.Id);
        result.Value.Items[0].SkillLevel.Should().Be((short)SkillLevel.Intermediate);
    }

    [Fact]
    public async Task Handle_FriendsOnly_ReturnsEventsOrganizedByAcceptedFriends()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var viewer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var friend = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var stranger = TestUsers.CreateActive("+905553333333", time.GetUtcNow());

        var friendship = Friendship.CreateRequest(viewer.Id, friend.Id, time.GetUtcNow());
        friendship.Accept(time.GetUtcNow());

        var friendEvent = CreateEvent(friend.Id, sport.Id, "Arkadas", 18, 40, time);
        var strangerEvent = CreateEvent(stranger.Id, sport.Id, "Yabanci", 18, 40, time);

        db.Users.AddRange(viewer, friend, stranger);
        db.Sports.Add(sport);
        db.Friendships.Add(friendship);
        db.Events.AddRange(friendEvent, strangerEvent);
        await db.SaveChangesAsync();

        var handler = new DiscoverEventsQueryHandler(db, new TestCurrentUser(viewer.Id), time);
        var result = await handler.Handle(
            new DiscoverEventsQuery(FriendsOnly: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(item => item.Id == friendEvent.Id);
    }

    private static UserProfile CreateProfile(
        Guid userId,
        string username,
        short gender,
        DateTimeOffset utcNow)
    {
        var profile = UserProfile.Create(userId, username, "User", utcNow);
        profile.UpdatePersonalDetails(gender, new DateOnly(1995, 1, 1), utcNow);
        return profile;
    }

    private static DomainEvent CreateEvent(
        Guid organizerId,
        Guid sportId,
        string title,
        int minAge,
        int maxAge,
        FakeTimeProvider time,
        SkillLevel? skillLevel = null)
    {
        var @event = DomainEvent.Create(
            organizerId,
            sportId,
            title,
            time.GetUtcNow().AddDays(1),
            90,
            41m,
            29m,
            "İstanbul",
            time.GetUtcNow(),
            maxParticipants: 10,
            minParticipantAge: minAge,
            maxParticipantAge: maxAge,
            skillLevel: skillLevel);
        @event.Publish(time.GetUtcNow());
        return @event;
    }
}
