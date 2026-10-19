using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.BackgroundJobs;

public sealed class EventSeriesDispatcherTests
{
    /// <summary>
    /// Seri halkası yayına alınmış doğuyor ama sohbetini PublishEvent komutu
    /// kuruyordu ve bu yol oradan geçmiyor. Sonuç: tekrarlayan etkinliklerin
    /// sohbeti hiç oluşmuyor, organizatör dahil kimse açamıyordu.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_OpensChat_ForTheNextOccurrence()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var sport = Sport.Create("Run", 1, time.GetUtcNow(), "run");

        var first = DomainEvent.Create(
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

        first.Publish(time.GetUtcNow());
        first.StartSeries(intervalWeeks: 1, totalOccurrences: 3, time.GetUtcNow());

        db.Users.Add(organizer);
        db.Sports.Add(sport);
        db.Events.Add(first);
        await db.SaveChangesAsync();

        // İlk halkanın bitişini geç: sıradaki halka ancak o zaman açılır.
        time.Advance(TimeSpan.FromHours(3));

        var dispatcher = new EventSeriesDispatcher(
            db,
            time,
            Options.Create(new BackgroundJobsOptions()),
            NullLogger<EventSeriesDispatcher>.Instance);

        var created = await dispatcher.DispatchAsync();

        created.Should().Be(1);

        var next = await db.Events
            .SingleAsync(candidate => candidate.Id != first.Id);

        next.Status.Should().Be(EventStatus.Published);

        var conversation = await db.Conversations
            .Include(candidate => candidate.Members)
            .SingleOrDefaultAsync(candidate => candidate.EventId == next.Id);

        conversation.Should().NotBeNull();
        conversation!.Type.Should().Be(ConversationType.Event);
        conversation.Members.Should().ContainSingle(member =>
            member.UserId == organizer.Id
            && member.Role == ConversationMemberRole.Owner);
    }
}
