using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.EventQuestions;

internal static class EventQuestionAccess
{
    internal static async Task<bool> CanWriteAsync(
        IApplicationDbContext dbContext,
        Event @event,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (@event.HasEnded(utcNow)
            || @event.Status is EventStatus.Draft or EventStatus.Cancelled)
        {
            return false;
        }

        if (await EventAccess.CanManageAsync(dbContext, @event, userId, cancellationToken))
        {
            return true;
        }

        return !await BlockQueries.BlockedPairExistsAsync(
            dbContext,
            userId,
            @event.OrganizerUserId,
            cancellationToken);
    }

    internal static async Task<HashSet<Guid>> ListParticipantUserIdsAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                participant.EventId == eventId
                && participant.UserId != null
                && (participant.Status == ParticipantStatus.Approved
                    || participant.Status == ParticipantStatus.Attended))
            .Select(participant => participant.UserId!.Value)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    /// <summary>
    /// Soru-cevapta "etkinlik sahibi" tarafı: oluşturan kişi ve (kulüp etkinliğinde) kulüp yöneticileri.
    /// Kulüp silinmemişse bu kişilerin yazdığı yanıtlar kulüp adına gösterilir (<c>AnsweredByClub</c>).
    /// </summary>
    internal sealed record HostContext(
        Guid OrganizerUserId,
        string? ClubName,
        string? ClubImageUrl,
        IReadOnlySet<Guid> ManagerUserIds)
    {
        internal bool IsHost(Guid userId) =>
            userId == OrganizerUserId || ManagerUserIds.Contains(userId);

        internal bool IsAnsweredByClub(Guid userId) => ClubName is not null && IsHost(userId);
    }

    internal static async Task<HostContext> LoadHostContextAsync(
        IApplicationDbContext dbContext,
        Guid organizerUserId,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        if (organizationId is not { } orgId)
        {
            return new HostContext(organizerUserId, null, null, new HashSet<Guid>());
        }

        // Global query filter: silinmiş kulüp null döner; o zaman yönetici de yok, kişi adı görünür.
        var club = await dbContext.Organizations.AsNoTracking()
            .Where(candidate => candidate.Id == orgId)
            .Select(candidate => new { candidate.Name, candidate.ImageUrl })
            .FirstOrDefaultAsync(cancellationToken);

        if (club is null)
        {
            return new HostContext(organizerUserId, null, null, new HashSet<Guid>());
        }

        var managers = await dbContext.OrganizationMembers.AsNoTracking()
            .Where(member =>
                member.OrganizationId == orgId
                && member.Status == OrganizationMemberStatus.Approved
                && (member.Role == OrganizationRole.Founder || member.Role == OrganizationRole.Admin))
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);

        return new HostContext(organizerUserId, club.Name, club.ImageUrl, managers.ToHashSet());
    }

    internal static EventQnAAuthorRole ResolveRole(
        Guid authorUserId,
        HostContext host,
        IReadOnlySet<Guid> participantUserIds)
    {
        if (host.IsHost(authorUserId))
        {
            return EventQnAAuthorRole.Organizer;
        }

        return participantUserIds.Contains(authorUserId)
            ? EventQnAAuthorRole.Participant
            : EventQnAAuthorRole.Visitor;
    }

    /// <summary>
    /// Sorgu Include etmediyse koleksiyon boş gelir; liste ucu ediyor.
    /// </summary>
    internal static IReadOnlyList<EventQuestionMentionResponse> ToMentionResponses(
        Domain.Events.EventQuestion question) =>
        question.Mentions
            .Select(mention => new EventQuestionMentionResponse(
                mention.UserId,
                mention.Username))
            .ToList();

    internal static string Preview(string content) =>
        content.Length <= 120 ? content : content[..117] + "...";

    internal static async Task<EventQuestionResponse> ToResponseAsync(
        IApplicationDbContext dbContext,
        EventQuestion question,
        HostContext host,
        IReadOnlySet<Guid> participantUserIds,
        IReadOnlyList<EventQuestionResponse>? replies,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == question.AuthorUserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        string? replyToUsername = null;
        if (question.ReplyToUserId is { } replyToUserId)
        {
            replyToUsername = await dbContext.UserProfiles.AsNoTracking()
                .Where(candidate => candidate.UserId == replyToUserId)
                .Select(candidate => candidate.Username)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new EventQuestionResponse(
            question.Id,
            question.EventId,
            question.AuthorUserId,
            profile?.Username,
            profile?.FirstName,
            profile?.ProfileImageUrl,
            question.ParentId,
            question.ReplyToUserId,
            replyToUsername,
            question.Content,
            question.ReplyCount,
            (short)ResolveRole(question.AuthorUserId, host, participantUserIds),
            question.CreatedAt,
            replies ?? [],
            ToMentionResponses(question),
            host.IsAnsweredByClub(question.AuthorUserId),
            host.ClubName,
            host.ClubImageUrl);
    }
}
