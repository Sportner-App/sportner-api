using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social.Posts;

internal static class PostEventLink
{
    /// <summary>
    /// Gönderiyi yalnızca etkinliğin organizatörü ya da onaylanmış/katılmış
    /// katılımcısı bağlayabilir; yoksa herkes başkasının etkinliğinin altına
    /// paylaşım düşebilirdi. İptal edilmiş etkinliğe paylaşım yapılmıyor.
    /// </summary>
    internal static async Task<bool> CanLinkAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var @event = await dbContext.Events.AsNoTracking()
            .Where(candidate => candidate.Id == eventId)
            .Select(candidate => new { candidate.OrganizerUserId, candidate.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (@event is null || @event.Status == EventStatus.Cancelled)
        {
            return false;
        }

        if (@event.OrganizerUserId == userId)
        {
            return true;
        }

        return await dbContext.EventParticipants.AsNoTracking().AnyAsync(
            participant =>
                participant.EventId == eventId
                && participant.UserId == userId
                && (participant.Status == ParticipantStatus.Approved
                    || participant.Status == ParticipantStatus.Attended),
            cancellationToken);
    }
}
