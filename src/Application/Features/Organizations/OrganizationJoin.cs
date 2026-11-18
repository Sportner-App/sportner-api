using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Organizations;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Organizations;

/// <summary>
/// Davet koduyla ve herkese açık kulübe doğrudan katılımın ortak akışı.
/// </summary>
internal static class OrganizationJoin
{
    /// <summary>Kullanıcının var olduğunu ve içerik oluşturabildiğini doğrular; hata varsa döner.</summary>
    internal static async Task<Error?> ValidateUserAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return OrganizationErrors.UserNotFound;
        }

        return user.CanCreateContent() ? null : OrganizationErrors.CannotCreateContent;
    }

    internal static async Task<Result<OrganizationDetailResponse>> JoinAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider,
        Organization organization,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var existing = await OrganizationQueries.FindMembershipAsync(
            dbContext,
            organization.Id,
            userId,
            cancellationToken);

        var utcNow = timeProvider.GetUtcNow();

        if (existing is null)
        {
            var approved = OrganizationMember.CreateApproved(organization.Id, userId, utcNow);
            dbContext.OrganizationMembers.Add(approved);
        }
        else if (existing.Status is OrganizationMemberStatus.Approved)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.AlreadyMember);
        }
        else if (existing.Status is OrganizationMemberStatus.Pending)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.AlreadyPending);
        }
        else if (existing.Status is OrganizationMemberStatus.Blocked)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.MemberBlocked);
        }
        else
        {
            try
            {
                existing.Rejoin(utcNow);
            }
            catch (DomainException)
            {
                return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.AlreadyPending);
            }
        }

        var actorUsername = await NotificationActor.ResolveUsernameAsync(dbContext, userId, cancellationToken);

        var managerIds = await dbContext.OrganizationMembers.AsNoTracking()
            .Where(member =>
                member.OrganizationId == organization.Id
                && member.Status == OrganizationMemberStatus.Approved
                && (member.Role == OrganizationRole.Founder || member.Role == OrganizationRole.Admin)
                && member.UserId != userId)
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);

        var languagesByManager = await NotificationActor.ResolveRecipientLanguagesAsync(
            dbContext, managerIds, cancellationToken);

        foreach (var managerId in managerIds)
        {
            var language = languagesByManager.GetValueOrDefault(managerId);
            var text = NotificationActor.Format(
                language,
                nameof(NotificationsResource.OrganizationMemberJoined_Text),
                NotificationActor.FormatPrefix(actorUsername, language));

            await notificationPublisher.PublishAsync(
                managerId,
                NotificationType.OrganizationMemberJoined,
                text,
                text,
                NotificationEntityType.Organization,
                organization.Id,
                userId,
                cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Aynı kullanıcıdan eşzamanlı iki katılım isteği: ikisi de üyelik bulamaz, ikisi de
            // satır ekler, ikincisi (OrganizationId, UserId) unique index'ine takılır. 500 değil
            // "zaten üye" dönmeli; ilk istek üyeliği zaten yazdı.
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.AlreadyMember);
        }

        var response = await OrganizationQueries.GetDetailAsync(
            dbContext,
            organization.Id,
            userId,
            cancellationToken);

        return response is null
            ? Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFound)
            : Result<OrganizationDetailResponse>.Success(response);
    }
}
