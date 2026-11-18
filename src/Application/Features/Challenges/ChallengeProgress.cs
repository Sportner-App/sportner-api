using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Challenges;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Challenges;

/// <summary>
/// Challenge ilerlemesi katılım kayıtlarından türetiliyor (karne ve seriyle aynı
/// kural): yoklamada "geldi" işaretlenen ya da organizatörü olunan tamamlanmış
/// etkinlikler, challenge'ın başlangıç–bitiş aralığında. Katılma tarihi önemli
/// değil; ay içinde sonradan katılan da o ayki maçlarını sayar.
/// </summary>
internal static class ChallengeProgress
{
    internal sealed record PlayedEvent(
        Guid UserId,
        Guid EventId,
        Guid SportId,
        DateTimeOffset EventDate,
        bool Organized,
        // false: kulüp etkinliğini oynamadan yöneten oluşturan; yalnızca "organize edilen" ölçütüne sayılır.
        bool Played = true);

    internal static async Task<ILookup<Guid, PlayedEvent>> LoadPlayedAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> userIds,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return Array.Empty<PlayedEvent>().ToLookup(item => item.UserId);
        }

        var attended = await (
                from participant in dbContext.EventParticipants.AsNoTracking()
                join @event in dbContext.Events.AsNoTracking()
                    on participant.EventId equals @event.Id
                where participant.UserId != null
                    && userIds.Contains(participant.UserId.Value)
                    && participant.Status == ParticipantStatus.Attended
                    && @event.Status != EventStatus.Cancelled
                    && @event.EventDate >= windowStart
                    && @event.EventDate < windowEnd
                select new PlayedEvent(
                    participant.UserId!.Value, @event.Id, @event.SportId, @event.EventDate, false))
            .ToListAsync(cancellationToken);

        var organized = await dbContext.Events.AsNoTracking()
            .Where(@event =>
                userIds.Contains(@event.OrganizerUserId)
                && @event.Status == EventStatus.Completed
                && @event.EventDate >= windowStart
                && @event.EventDate < windowEnd)
            .Select(@event => new PlayedEvent(
                @event.OrganizerUserId,
                @event.Id,
                @event.SportId,
                @event.EventDate,
                true,
                @event.IsOrganizerParticipant))
            .ToListAsync(cancellationToken);

        return attended.Concat(organized).ToLookup(item => item.UserId);
    }

    internal static int Compute(Challenge challenge, IEnumerable<PlayedEvent> played)
    {
        var inWindow = played
            .Where(item => item.EventDate >= challenge.StartsAt && item.EventDate < challenge.EndsAt)
            .ToList();

        return challenge.Metric switch
        {
            ChallengeMetric.EventsPlayed => inWindow
                .Where(item => item.Played)
                .Select(item => item.EventId)
                .Distinct()
                .Count(),
            ChallengeMetric.DistinctSports => inWindow
                .Where(item => item.Played)
                .Select(item => item.SportId)
                .Distinct()
                .Count(),
            ChallengeMetric.EventsOrganized => inWindow
                .Where(item => item.Organized)
                .Select(item => item.EventId)
                .Distinct()
                .Count(),
            ChallengeMetric.SportEventsPlayed => inWindow
                .Where(item => item.Played && item.SportId == challenge.SportId)
                .Select(item => item.EventId)
                .Distinct()
                .Count(),
            _ => 0
        };
    }
}
