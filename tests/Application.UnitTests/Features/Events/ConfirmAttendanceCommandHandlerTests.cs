using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Features.Events.ConfirmAttendance;
using Sportner.Application.Features.Quests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class ConfirmAttendanceCommandHandlerTests
{
    [Fact]
    public async Task Handle_SecondConfirm_DoesNotIncreaseEventsCompletedAgain()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var attendee = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Basketball", 1, time.GetUtcNow(), "basketball");
        var profile = UserProfile.Create(organizer.Id, "organizer", "Org", time.GetUtcNow());
        organizer.AttachUserProfile(profile);

        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Evening run",
            time.GetUtcNow().AddHours(1),
            durationMinutes: 60,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 10);

        @event.Publish(time.GetUtcNow());
        @event.Apply(attendee.Id, time.GetUtcNow());
        @event.ApproveParticipant(attendee.Id, time.GetUtcNow());
        attendee.Statistics!.IncreaseEventsJoined(time.GetUtcNow());

        db.Users.AddRange(organizer, attendee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        time.Advance(TimeSpan.FromHours(3));
        @event.Complete(time.GetUtcNow());
        await db.SaveChangesAsync();

        var badgeAwarder = new Mock<IBadgeAwarder>();
        badgeAwarder
            .Setup(awarder => awarder.TryAwardAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        badgeAwarder
            .Setup(awarder => awarder.EvaluateAfterAttendanceAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ConfirmAttendanceCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            badgeAwarder.Object,
            new Mock<IQuestProgressTracker>().Object);

        var first = await handler.Handle(
            new ConfirmAttendanceCommand(@event.Id, attendee.Id),
            CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        attendee.Statistics!.EventsCompleted.Should().Be(1);

        var second = await handler.Handle(
            new ConfirmAttendanceCommand(@event.Id, attendee.Id),
            CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        attendee.Statistics.EventsCompleted.Should().Be(1);

        var participant = await db.EventParticipants.FindAsync(
            @event.Participants.Single(p => p.UserId == attendee.Id).Id);

        participant!.Status.Should().Be(ParticipantStatus.Attended);
        badgeAwarder.Verify(
            awarder => awarder.TryAwardAsync(
                attendee.Id,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
