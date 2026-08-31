using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Organizations.ApproveOrganizationMember;

public sealed record ApproveOrganizationMemberCommand(Guid OrganizationId, Guid UserId)
    : ICommand<OrganizationMemberResponse>;

internal sealed class ApproveOrganizationMemberCommandHandler
    : ICommandHandler<ApproveOrganizationMemberCommand, OrganizationMemberResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public ApproveOrganizationMemberCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<OrganizationMemberResponse>> Handle(
        ApproveOrganizationMemberCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } actorId)
        {
            return Result<OrganizationMemberResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        var actor = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            actorId,
            cancellationToken);

        if (actor is null || !actor.CanManageMembers)
        {
            return Result<OrganizationMemberResponse>.Failure(OrganizationErrors.CannotManageMembers);
        }

        var membership = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return Result<OrganizationMemberResponse>.Failure(OrganizationErrors.MemberNotFound);
        }

        try
        {
            membership.Approve(_timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return Result<OrganizationMemberResponse>.Failure(OrganizationErrors.MemberNotFound);
        }

        var title = await NotificationActor.TitleAsync(
            _dbContext,
            actorId,
            "organizasyon katılımını onayladı",
            cancellationToken);

        await _notificationPublisher.PublishAsync(
            request.UserId,
            NotificationType.OrganizationJoinApproved,
            title,
            title,
            NotificationEntityType.Organization,
            request.OrganizationId,
            actorId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<OrganizationMemberResponse>.Success(
            await MapMemberAsync(_dbContext, membership, cancellationToken));
    }

    private static async Task<OrganizationMemberResponse> MapMemberAsync(
        IApplicationDbContext dbContext,
        Domain.Organizations.OrganizationMember membership,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == membership.UserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.LastName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new OrganizationMemberResponse(
            membership.UserId,
            profile?.Username,
            profile?.FirstName,
            profile?.LastName,
            profile?.ProfileImageUrl,
            (short)membership.Role,
            (short)membership.Status,
            membership.CreatedAt,
            membership.RespondedAt);
    }
}
