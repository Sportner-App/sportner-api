using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.Features.Quests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.BackgroundJobs;

public sealed class EventCompletionDispatcherTests
{
    [Fact]
    public async Task Dispatch_CompletesEvent_AfterScheduledEnd_OnlyOnce()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var sport = Sport.Create("Run", 1, time.GetUtcNow(), "run");
        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Morning run",
            time.GetUtcNow().AddHours(1),
            durationMinutes: 60,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 10);

        @event.Publish(time.GetUtcNow());

        db.Users.Add(organizer);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var badges = new Mock<IBadgeAwarder>();
        badges
            .Setup(awarder => awarder.EvaluateAfterEventCompletedAsync(
                organizer.Id,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var quests = new Mock<IQuestProgressTracker>();
        quests
            .Setup(tracker => tracker.ReportAsync(
                organizer.Id,
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dispatcher = new EventCompletionDispatcher(
            db,
            badges.Object,
            quests.Object,
            time,
            Options.Create(new BackgroundJobsOptions()),
            NullLogger<EventCompletionDispatcher>.Instance);

        var beforeEnd = await dispatcher.DispatchAsync();
        beforeEnd.Should().Be(0);
        (await db.Events.FindAsync(@event.Id))!.Status.Should().Be(EventStatus.Published);

        time.Advance(TimeSpan.FromHours(2));

        var first = await dispatcher.DispatchAsync();
        var second = await dispatcher.DispatchAsync();

        first.Should().Be(1);
        second.Should().Be(0);
        (await db.Events.FindAsync(@event.Id))!.Status.Should().Be(EventStatus.Completed);
        badges.Verify(
            awarder => awarder.EvaluateAfterEventCompletedAsync(organizer.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
