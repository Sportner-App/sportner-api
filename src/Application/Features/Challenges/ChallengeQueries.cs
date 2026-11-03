using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Challenges;

namespace Sportner.Application.Features.Challenges;

internal static class ChallengeQueries
{
    /// <summary>Verilen challenge'ları katılımcı sayıları ve isteyenin ilerlemesiyle birlikte döner.</summary>
    internal static async Task<List<ChallengeResponse>> BuildResponsesAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<Challenge> challenges,
        Guid? requesterId,
        CancellationToken cancellationToken)
    {
        if (challenges.Count == 0)
        {
            return [];
        }

        var challengeIds = challenges.Select(challenge => challenge.Id).ToList();

        var counts = await dbContext.ChallengeParticipants.AsNoTracking()
            .Where(participant => challengeIds.Contains(participant.ChallengeId))
            .GroupBy(participant => participant.ChallengeId)
            .Select(group => new
            {
                ChallengeId = group.Key,
                Joined = group.Count(),
                Completed = group.Count(participant => participant.CompletedAt != null)
            })
            .ToDictionaryAsync(row => row.ChallengeId, cancellationToken);

        var sportIds = challenges
            .Where(challenge => challenge.SportId is not null)
            .Select(challenge => challenge.SportId!.Value)
            .Distinct()
            .ToList();
        var sportSlugs = await dbContext.Sports.AsNoTracking()
            .Where(sport => sportIds.Contains(sport.Id))
            .ToDictionaryAsync(sport => sport.Id, sport => sport.Slug, cancellationToken);

        var participations = new Dictionary<Guid, ChallengeParticipant>();
        ILookup<Guid, ChallengeProgress.PlayedEvent> played =
            Array.Empty<ChallengeProgress.PlayedEvent>().ToLookup(item => item.UserId);

        if (requesterId is { } userId)
        {
            participations = await dbContext.ChallengeParticipants.AsNoTracking()
                .Where(participant =>
                    participant.UserId == userId && challengeIds.Contains(participant.ChallengeId))
                .ToDictionaryAsync(participant => participant.ChallengeId, cancellationToken);

            played = await ChallengeProgress.LoadPlayedAsync(
                dbContext,
                [userId],
                challenges.Min(challenge => challenge.StartsAt),
                challenges.Max(challenge => challenge.EndsAt),
                cancellationToken);
        }

        return challenges
            .Select(challenge =>
            {
                var count = counts.GetValueOrDefault(challenge.Id);
                var participation = participations.GetValueOrDefault(challenge.Id);
                return ChallengeMapping.ToResponse(
                    challenge,
                    challenge.SportId is { } sportId ? sportSlugs.GetValueOrDefault(sportId) : null,
                    count?.Joined ?? 0,
                    count?.Completed ?? 0,
                    participation,
                    requesterId is { } id ? ChallengeProgress.Compute(challenge, played[id]) : 0);
            })
            .ToList();
    }
}
