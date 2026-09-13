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
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Organizations.UpdateOrganizationMemberRole;

public sealed record UpdateOrganizationMemberRoleCommand(
    Guid OrganizationId,
    Guid UserId,
    short Role) : ICommand<OrganizationMemberResponse>;

public sealed class UpdateOrganizationMemberRoleCommandValidator
    : AbstractValidator<UpdateOrganizationMemberRoleCommand>
{
    public UpdateOrganizationMemberRoleCommandValidator()
    {
        RuleFor(command => command.Role)
            .Must(role => role is (short)OrganizationRole.Admin or (short)OrganizationRole.Member)
            .WithMessage("Only admin or member roles can be assigned.");
    }
}

internal sealed class UpdateOrganizationMemberRoleCommandHandler
    : ICommandHandler<UpdateOrganizationMemberRoleCommand, OrganizationMemberResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public UpdateOrganizationMemberRoleCommandHandler(
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
        UpdateOrganizationMemberRoleCommand request,
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

        if (actor is null || !actor.IsApproved || actor.Role is not OrganizationRole.Founder)
        {
            return Result<OrganizationMemberResponse>.Failure(OrganizationErrors.NotFounder);
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
            membership.SetRole((OrganizationRole)request.Role, _timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return Result<OrganizationMemberResponse>.Failure(OrganizationErrors.MemberNotFound);
        }

        var resourceKey = membership.Role is OrganizationRole.Admin
            ? nameof(NotificationsResource.OrganizationRoleChangedToAdmin_Text)
            : nameof(NotificationsResource.OrganizationRoleChangedFromAdmin_Text);

        var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
            _dbContext, request.UserId, cancellationToken);
        var title = NotificationActor.Format(
            recipientLanguage,
            resourceKey,
            await NotificationActor.PrefixAsync(_dbContext, actorId, recipientLanguage, cancellationToken));

        await _notificationPublisher.PublishAsync(
            request.UserId,
            NotificationType.OrganizationRoleChanged,
            title,
            title,
            NotificationEntityType.Organization,
            request.OrganizationId,
            actorId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var profile = await _dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == membership.UserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.LastName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Result<OrganizationMemberResponse>.Success(new OrganizationMemberResponse(
            membership.UserId,
            profile?.Username,
            profile?.FirstName,
            profile?.LastName,
            profile?.ProfileImageUrl,
            (short)membership.Role,
            (short)membership.Status,
            membership.CreatedAt,
            membership.RespondedAt));
    }
}
