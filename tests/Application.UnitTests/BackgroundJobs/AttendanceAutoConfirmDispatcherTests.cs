using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.Features.Quests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.BackgroundJobs;

public sealed class AttendanceAutoConfirmDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_DoesNothing_BeforeGracePeriodElapses()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var (organizer, attendee, sport, @event) = SeedCompletedEventWithPendingAttendance(now);
        db.Users.AddRange(organizer, attendee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var dispatcher = new AttendanceAutoConfirmDispatcher(
            db,
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            Mock.Of<INotificationPublisher>(),
            time,
            Options.Create(new BackgroundJobsOptions { AttendanceAutoConfirmGraceDays = 3 }),
            NullLogger<AttendanceAutoConfirmDispatcher>.Instance);

        // Event ended ~2h ago; grace period (3 days) hasn't elapsed yet.
        var confirmed = await dispatcher.DispatchAsync();

        confirmed.Should().Be(0);
        var reloaded = await db.Events
            .Include(candidate => candidate.Participants)
            .FirstAsync(candidate => candidate.Id == @event.Id);
        reloaded.Participants.Single(p => p.UserId == attendee.Id).Status
            .Should().Be(ParticipantStatus.Approved);
    }

    [Fact]
    public async Task DispatchAsync_AutoConfirmsAttendance_AfterGracePeriodElapses()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var (organizer, attendee, sport, @event) = SeedCompletedEventWithPendingAttendance(now);
        db.Users.AddRange(organizer, attendee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var dispatcher = new AttendanceAutoConfirmDispatcher(
            db,
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            Mock.Of<INotificationPublisher>(),
            time,
            Options.Create(new BackgroundJobsOptions { AttendanceAutoConfirmGraceDays = 3 }),
            NullLogger<AttendanceAutoConfirmDispatcher>.Instance);

        time.Advance(TimeSpan.FromDays(4));

        var confirmed = await dispatcher.DispatchAsync();

        // The organizer is auto-enrolled as an Approved participant of their own event
        // (Event.Create), so the sweep confirms both the organizer and the attendee.
        confirmed.Should().Be(2);
        var reloaded = await db.Events.FindAsync(@event.Id);
        reloaded!.Participants.Should().OnlyContain(p => p.Status == ParticipantStatus.Attended);
        reloaded.Participants.Should().Contain(p => p.UserId == attendee.Id);
        reloaded.Participants.Should().Contain(p => p.UserId == organizer.Id);
    }

    private static (Sportner.Domain.Users.User Organizer, Sportner.Domain.Users.User Attendee, Sport Sport, DomainEvent Event)
        SeedCompletedEventWithPendingAttendance(DateTimeOffset now)
    {
        var organizer = TestUsers.CreateActive("+905551110100", now);
        var attendee = TestUsers.CreateActive("+905551110101", now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(-2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now.AddHours(-3), maxParticipants: 10);
        @event.Publish(now.AddHours(-3));
        @event.Apply(attendee.Id, now.AddHours(-2));
        @event.ApproveParticipant(attendee.Id, now.AddHours(-2));
        @event.Complete(now);

        return (organizer, attendee, sport, @event);
    }
}
