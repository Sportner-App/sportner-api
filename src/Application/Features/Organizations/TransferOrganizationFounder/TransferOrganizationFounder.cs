using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Organizations.TransferOrganizationFounder;

public sealed record TransferOrganizationFounderCommand(Guid OrganizationId, Guid UserId)
    : ICommand<OrganizationDetailResponse>;

public sealed class TransferOrganizationFounderCommandValidator
    : AbstractValidator<TransferOrganizationFounderCommand>
{
    public TransferOrganizationFounderCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}

internal sealed class TransferOrganizationFounderCommandHandler
    : ICommandHandler<TransferOrganizationFounderCommand, OrganizationDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public TransferOrganizationFounderCommandHandler(
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
        TransferOrganizationFounderCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } actorId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        var actor = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            actorId,
            cancellationToken);

        if (actor is null || !actor.IsApproved || actor.Role is not OrganizationRole.Founder)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFounder);
        }

        if (request.UserId == actorId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CannotTransferToSelf);
        }

        var target = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            request.UserId,
            cancellationToken);

        if (target is null || !target.IsApproved)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.TransferTargetNotApproved);
        }

        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(candidate => candidate.Id == request.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        target.PromoteToFounder(utcNow);
        actor.DemoteFounderToAdmin(utcNow);
        organization.TransferFounder(target.UserId, utcNow);

        var language = await NotificationActor.ResolveRecipientLanguageAsync(
            _dbContext, target.UserId, cancellationToken);
        var actorPrefix = await NotificationActor.PrefixAsync(_dbContext, actorId, language, cancellationToken);

        await _notificationPublisher.PublishAsync(
            target.UserId,
            NotificationType.OrganizationRoleChanged,
            NotificationActor.Format(
                language,
                nameof(NotificationsResource.OrganizationFounderTransferred_Title)),
            NotificationActor.Format(
                language,
                nameof(NotificationsResource.OrganizationFounderTransferred_Body),
                actorPrefix,
                organization.Name),
            NotificationEntityType.Organization,
            organization.Id,
            actorId,
            cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Organization / OrganizationMember satırlarında xmin iyimser kilidi var: aynı anda gelen
            // başka bir devir, block/leave ya da silme bu istekten önce kazandı. Kurucu artık çağıran
            // değil (ya da hedef artık onaylı üye değil); tek kurucu kuralı korunur.
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFounder);
        }

        var response = await OrganizationQueries.GetDetailAsync(
            _dbContext,
            organization.Id,
            actorId,
            cancellationToken);

        return response is null
            ? Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFound)
            : Result<OrganizationDetailResponse>.Success(response);
    }
}
