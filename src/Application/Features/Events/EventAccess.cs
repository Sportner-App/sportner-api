using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Organizations;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Domain.Messaging;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Events;

internal static class EventAccess
{
    /// <summary>
    /// Etkinliği yönetme yetkisi: oluşturan kişi ya da etkinlik bir kulübe bağlıysa kulübün (silinmemiş)
    /// onaylı Kurucu/Yönetici üyeleri. Kişisel işler (kendi etkinliğine başvuramama, yoklama/paylaşım
    /// daveti, EventsOrganized gibi istatistikler) bu kontrolü değil <c>OrganizerUserId</c>'yi kullanır.
    /// </summary>
    internal static async Task<bool> CanManageAsync(
        IApplicationDbContext dbContext,
        Domain.Events.Event @event,
        Guid? userId,
        CancellationToken cancellationToken) =>
        await CanManageAsync(dbContext, @event.OrganizerUserId, @event.OrganizationId, userId, cancellationToken);

    internal static async Task<bool> CanManageAsync(
        IApplicationDbContext dbContext,
        Guid organizerUserId,
        Guid? organizationId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (userId is not { } id)
        {
            return false;
        }

        if (organizerUserId == id)
        {
            return true;
        }

        if (organizationId is not { } orgId)
        {
            return false;
        }

        return await IsClubManagerAsync(dbContext, orgId, id, cancellationToken);
    }

    /// <summary>Kulüp silinmemiş ve kullanıcı onaylı Kurucu/Yönetici ise true (silinmiş kulüpte hep false).</summary>
    internal static Task<bool> IsClubManagerAsync(
        IApplicationDbContext dbContext,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationMembers.AsNoTracking()
            .AnyAsync(
                member =>
                    member.OrganizationId == organizationId
                    && member.UserId == userId
                    && member.Status == OrganizationMemberStatus.Approved
                    && (member.Role == OrganizationRole.Founder || member.Role == OrganizationRole.Admin)
                    && dbContext.Organizations.Any(organization => organization.Id == member.OrganizationId),
                cancellationToken);

    /// <summary>Kullanıcının onaylı Kurucu/Yönetici olduğu (silinmemiş) kulüplerin kimlikleri.</summary>
    internal static Task<List<Guid>> ListManagedOrganizationIdsAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationMembers.AsNoTracking()
            .Where(member =>
                member.UserId == userId
                && member.Status == OrganizationMemberStatus.Approved
                && (member.Role == OrganizationRole.Founder || member.Role == OrganizationRole.Admin)
                && dbContext.Organizations.Any(organization => organization.Id == member.OrganizationId))
            .Select(member => member.OrganizationId)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Etkinlik yönetim bildirimlerinin (başvuru vb.) alıcıları: oluşturan + kulüp etkinliğinde kulübün
    /// onaylı Kurucu/Yönetici üyeleri; tekilleştirilmiş, <paramref name="excludeUserId"/> hariç.
    /// </summary>
    internal static async Task<IReadOnlyList<Guid>> ResolveManagerRecipientsAsync(
        IApplicationDbContext dbContext,
        Domain.Events.Event @event,
        Guid excludeUserId,
        CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid> { @event.OrganizerUserId };

        if (@event.OrganizationId is { } organizationId)
        {
            var managerIds = await dbContext.OrganizationMembers.AsNoTracking()
                .Where(member =>
                    member.OrganizationId == organizationId
                    && member.Status == OrganizationMemberStatus.Approved
                    && (member.Role == OrganizationRole.Founder || member.Role == OrganizationRole.Admin)
                    && dbContext.Organizations.Any(organization => organization.Id == member.OrganizationId))
                .Select(member => member.UserId)
                .ToListAsync(cancellationToken);

            recipients.UnionWith(managerIds);
        }

        recipients.Remove(excludeUserId);

        return recipients.ToList();
    }

    internal static async Task<Result<(User User, Domain.Events.Event Event)>> LoadOrganizerEventAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.UserNotFound);
        }

        var @event = await LoadAggregateAsync(dbContext, eventId, cancellationToken);

        if (@event is null)
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.NotFound);
        }

        if (!await CanManageAsync(dbContext, @event, userId, cancellationToken))
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.NotOrganizer);
        }

        return Result<(User, Domain.Events.Event)>.Success((user, @event));
    }

    /// <summary>
    /// Yalnızca oluşturan kişinin yapabildiği kişisel işler (ör. kendi oyuncu katılımını değiştirme).
    /// Kulüp yöneticileri geçemez.
    /// </summary>
    internal static async Task<Result<(User User, Domain.Events.Event Event)>> LoadCreatorEventAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.UserNotFound);
        }

        var @event = await LoadAggregateAsync(dbContext, eventId, cancellationToken);

        if (@event is null)
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.NotFound);
        }

        if (@event.OrganizerUserId != userId)
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.NotOrganizer);
        }

        return Result<(User, Domain.Events.Event)>.Success((user, @event));
    }

    internal static Task<Domain.Events.Event?> LoadAggregateAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken) =>
        dbContext.Events
            .Include(candidate => candidate.Participants)
            .Include(candidate => candidate.Waitlist)
            .FirstOrDefaultAsync(candidate => candidate.Id == eventId, cancellationToken);

    internal static Task<Conversation?> FindEventConversationAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken) =>
        dbContext.Conversations
            .Include(conversation => conversation.Members)
            .FirstOrDefaultAsync(
                conversation =>
                    conversation.EventId == eventId
                    && conversation.Type == ConversationType.Event,
                cancellationToken);

    internal static async Task EnsureEventConversationAsync(
        IApplicationDbContext dbContext,
        Domain.Events.Event @event,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await FindEventConversationAsync(dbContext, @event.Id, cancellationToken);

        if (existing is not null)
        {
            return;
        }

        dbContext.Conversations.Add(
            Conversation.CreateEventConversation(@event.Id, @event.OrganizerUserId, utcNow, @event.Title));
    }

    internal static async Task AddConversationMemberIfPresentAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var conversation = await FindEventConversationAsync(dbContext, eventId, cancellationToken);

        if (conversation is null || conversation.IsClosed)
        {
            return;
        }

        if (conversation.ContainsActiveMember(userId))
        {
            return;
        }

        var alreadyTracked = conversation.Members.Any(member => member.UserId == userId);
        var member = conversation.AddMember(userId, utcNow);

        if (!alreadyTracked)
        {
            dbContext.MarkAsAdded(member);
        }
    }

    internal static async Task RemoveConversationMemberIfPresentAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var conversation = await FindEventConversationAsync(dbContext, eventId, cancellationToken);

        if (conversation is null || conversation.IsClosed)
        {
            return;
        }

        if (!conversation.ContainsActiveMember(userId))
        {
            return;
        }

        conversation.RemoveMember(userId, utcNow);
    }

    internal static async Task CloseEventConversationAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var conversation = await FindEventConversationAsync(dbContext, eventId, cancellationToken);

        conversation?.Close(utcNow);
    }
}
