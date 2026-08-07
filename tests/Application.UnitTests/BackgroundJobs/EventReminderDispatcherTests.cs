using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.BackgroundJobs;

public sealed class EventReminderDispatcherTests
{
    [Fact]
    public async Task Dispatch_SendsOnce_PerUserAndWindow()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var attendee = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Run", 1, time.GetUtcNow(), "run");

        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Morning run",
            time.GetUtcNow().AddHours(2),
            durationMinutes: 60,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 10);

        @event.Publish(time.GetUtcNow());
        @event.Apply(attendee.Id, time.GetUtcNow());
        @event.ApproveParticipant(attendee.Id, time.GetUtcNow());

        db.Users.AddRange(organizer, attendee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        // Enter the 1h grace window (event in 2h; threshold at +60m; advance 65m).
        time.Advance(TimeSpan.FromMinutes(65));

        var publishCount = 0;
        var publisher = new Mock<INotificationPublisher>();
        publisher
            .Setup(p => p.PublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => publishCount++)
            .Returns(Task.CompletedTask);

        var dispatcher = new EventReminderDispatcher(
            db,
            publisher.Object,
            time,
            Options.Create(new BackgroundJobsOptions
            {
                EventReminderWindowsMinutes = [60]
            }),
            NullLogger<EventReminderDispatcher>.Instance);

        var first = await dispatcher.DispatchAsync();
        var second = await dispatcher.DispatchAsync();

        first.Should().Be(1);
        second.Should().Be(0);
        publishCount.Should().Be(1);
        db.EventReminderDispatches.Should().HaveCount(1);
    }
}
