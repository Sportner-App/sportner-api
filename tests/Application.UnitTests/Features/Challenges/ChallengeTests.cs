using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.Features.Challenges;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Challenges;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Challenges;

public sealed class ChallengeTests
{
    // 10 Kasım 2026 12:00 İstanbul.
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IntroMonth_UsesShorterTargets_AndIstanbulMonthBounds()
    {
        var november = ChallengeDispatcher.BuildMonthlyChallenges(Now);
        var december = ChallengeDispatcher.BuildMonthlyChallenges(Now.AddMonths(1));

        november.Select(challenge => challenge.TargetValue).Should().Equal(3, 2, 1);
        december.Select(challenge => challenge.TargetValue).Should().Equal(4, 3, 1);
        november[0].Code.Should().Be("2026-11-play");
        november[0].Title.Should().Be("Kasım Oyuncusu");
        november[0].StartsAt.Should().Be(new DateTimeOffset(2026, 10, 31, 21, 0, 0, TimeSpan.Zero));
        november[0].EndsAt.Should().Be(new DateTimeOffset(2026, 11, 30, 21, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Progress_CountsOnlyEventsInsideTheChallengeWindow()
    {
        var challenges = ChallengeDispatcher.BuildMonthlyChallenges(Now);
        var football = Guid.NewGuid();
        var tennis = Guid.NewGuid();
        var userId = Guid.NewGuid();
        ChallengeProgress.PlayedEvent Played(Guid sport, DateTimeOffset date, bool organized = false) =>
            new(userId, Guid.NewGuid(), sport, date, organized);

        var played = new[]
        {
            Played(football, Now.AddDays(-2)),
            Played(tennis, Now.AddDays(-1), organized: true),
            Played(football, Now.AddDays(-30)) // Ekim: sayılmaz.
        };

        ChallengeProgress.Compute(challenges[0], played).Should().Be(2);
        ChallengeProgress.Compute(challenges[1], played).Should().Be(2);
        ChallengeProgress.Compute(challenges[2], played).Should().Be(1);
    }

    [Fact]
    public async Task Join_IsIdempotent_AndCompletionIsMarkedOnceWithNotification()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(Now);
        var user = TestUsers.CreateActive("+905551111111", Now);
        user.AttachUserProfile(UserProfile.Create(user.Id, "player", "Player", Now));
        var sport = Sport.Create("Football", 1, Now, "football");
        var challenge = Challenge.Create(
            "test-organize", "Kaptan", "Bir etkinlik düzenle", "flag",
            ChallengeMetric.EventsOrganized, 1,
            Now.AddDays(-9), Now.AddDays(20), 0, Now);
        db.Users.Add(user);
        db.Sports.Add(sport);
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var handler = new JoinChallengeCommandHandler(db, new TestCurrentUser(user.Id), time);
        await handler.Handle(new JoinChallengeCommand(challenge.Id), CancellationToken.None);
        var joined = await handler.Handle(new JoinChallengeCommand(challenge.Id), CancellationToken.None);

        joined.Value!.IsJoined.Should().BeTrue();
        joined.Value.ParticipantCount.Should().Be(1);
        joined.Value.IsCompleted.Should().BeFalse();

        var @event = DomainEvent.Create(
            user.Id, sport.Id, "Match", Now.AddDays(-2), 60,
            latitude: 41m, longitude: 29m, address: "Kadıköy/İstanbul",
            Now.AddDays(-3), maxParticipants: 8);
        @event.Publish(Now.AddDays(-3));
        @event.CompleteIfDue(Now);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var publisher = new RecordingPublisher();
        var dispatcher = new ChallengeDispatcher(
            db, publisher, time, NullLogger<ChallengeDispatcher>.Instance);
        await dispatcher.DispatchAsync();
        await dispatcher.DispatchAsync();

        publisher.Sent.Count(type => type == NotificationType.ChallengeCompleted).Should().Be(1);
        db.ChallengeParticipants.Single().CompletedAt.Should().NotBeNull();

        var leave = await new LeaveChallengeCommandHandler(db, new TestCurrentUser(user.Id))
            .Handle(new LeaveChallengeCommand(challenge.Id), CancellationToken.None);
        leave.IsFailure.Should().BeTrue();
    }

    private sealed class RecordingPublisher : INotificationPublisher
    {
        public List<NotificationType> Sent { get; } = [];

        public Task PublishAsync(
            Guid recipientUserId,
            NotificationType type,
            string title,
            string body,
            NotificationEntityType entityType,
            Guid? entityId,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
        {
            Sent.Add(type);
            return Task.CompletedTask;
        }
    }
}
