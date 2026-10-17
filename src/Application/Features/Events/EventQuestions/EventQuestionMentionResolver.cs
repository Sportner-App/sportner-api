using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.EventQuestions;

/// <summary>
/// Etkinlik sorusunda etiketlenecek kullanıcıları doğrular.
///
/// Ölçüt arkadaşlık değil etkinliğe dahil olmak: soru-cevap bölümünde
/// etiketlenmek istenen kişiler organizatör ve diğer katılımcılar, ve bunlar
/// soruyu soranın arkadaşı olmak zorunda değil. Zaten soruyu görebilen birini
/// etiketlemek yeni bir erişim açmıyor.
/// </summary>
internal static class EventQuestionMentionResolver
{
    internal static async Task<Result<IReadOnlyList<(Guid UserId, string Username)>>> ResolveAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        Guid organizerUserId,
        Guid authorUserId,
        IReadOnlyList<Guid>? mentionedUserIds,
        CancellationToken cancellationToken)
    {
        if (mentionedUserIds is null || mentionedUserIds.Count == 0)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Success([]);
        }

        var requested = mentionedUserIds
            .Where(id => id != Guid.Empty && id != authorUserId)
            .Distinct()
            .ToList();

        if (requested.Count == 0)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Success([]);
        }

        var allowed = await EventQuestionAccess.ListParticipantUserIdsAsync(
            dbContext, eventId, cancellationToken);
        allowed.Add(organizerUserId);

        if (requested.Any(id => !allowed.Contains(id)))
        {
            return Result<IReadOnlyList<(Guid, string)>>.Failure(
                EventErrors.MentionNotAllowed);
        }

        var usernames = await dbContext.UserProfiles.AsNoTracking()
            .Where(profile => requested.Contains(profile.UserId))
            .Select(profile => new { profile.UserId, profile.Username })
            .ToListAsync(cancellationToken);

        if (usernames.Count != requested.Count)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Failure(
                EventErrors.MentionNotAllowed);
        }

        return Result<IReadOnlyList<(Guid, string)>>.Success(
            usernames.Select(row => (row.UserId, row.Username)).ToList());
    }
}
