using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
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

        if (await OrganizationJoin.ValidateUserAsync(_dbContext, userId, cancellationToken) is { } userError)
        {
            return Result<OrganizationDetailResponse>.Failure(userError);
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

        return await OrganizationJoin.JoinAsync(
            _dbContext,
            _notificationPublisher,
            _timeProvider,
            organization,
            userId,
            cancellationToken);
    }
}
