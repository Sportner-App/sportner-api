using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Organizations;

namespace Sportner.Application.Features.Organizations.JoinOrganization;

public sealed record JoinOrganizationCommand(string InviteCode) : ICommand<OrganizationDetailResponse>;

public sealed class JoinOrganizationCommandValidator : AbstractValidator<JoinOrganizationCommand>
{
    public JoinOrganizationCommandValidator()
    {
        RuleFor(command => command.InviteCode).NotEmpty();
    }
}

internal sealed class JoinOrganizationCommandHandler
    : ICommandHandler<JoinOrganizationCommand, OrganizationDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public JoinOrganizationCommandHandler(
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

    public async Task<Result<OrganizationDetailResponse>> Handle(
        JoinOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.UserNotFound);
        }

        if (!user.CanCreateContent())
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CannotCreateContent);
        }

        string inviteCode;
        try
        {
            inviteCode = Organization.NormalizeInviteCode(request.InviteCode);
        }
        catch (DomainException)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.InvalidInviteCode);
        }

        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(candidate => candidate.InviteCode == inviteCode, cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.InvalidInviteCode);
        }

        var existing = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            organization.Id,
            userId,
            cancellationToken);

        var utcNow = _timeProvider.GetUtcNow();

        if (existing is null)
        {
            var pending = OrganizationMember.CreatePending(organization.Id, userId, utcNow);
            _dbContext.OrganizationMembers.Add(pending);
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
                existing.Reapply(utcNow);
            }
            catch (DomainException)
            {
                return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.AlreadyPending);
            }
        }

        var requestCopy = await NotificationActor.TitleAsync(
            _dbContext,
            userId,
            "organizasyona katılmak istiyor",
            cancellationToken);

        var managerIds = await _dbContext.OrganizationMembers.AsNoTracking()
            .Where(member =>
                member.OrganizationId == organization.Id
                && member.Status == OrganizationMemberStatus.Approved
                && (member.Role == OrganizationRole.Founder || member.Role == OrganizationRole.Admin)
                && member.UserId != userId)
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);

        foreach (var managerId in managerIds)
        {
            await _notificationPublisher.PublishAsync(
                managerId,
                NotificationType.OrganizationJoinRequested,
                requestCopy,
                requestCopy,
                NotificationEntityType.Organization,
                organization.Id,
                userId,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await OrganizationQueries.GetDetailAsync(
            _dbContext,
            organization.Id,
            userId,
            cancellationToken);

        return Result<OrganizationDetailResponse>.Success(response!);
    }
}
