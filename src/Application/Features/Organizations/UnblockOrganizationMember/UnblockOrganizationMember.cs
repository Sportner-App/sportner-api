using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Organizations.UnblockOrganizationMember;

public sealed record UnblockOrganizationMemberCommand(Guid OrganizationId, Guid UserId) : ICommand;

internal sealed class UnblockOrganizationMemberCommandHandler
    : ICommandHandler<UnblockOrganizationMemberCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UnblockOrganizationMemberCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        UnblockOrganizationMemberCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } actorId)
        {
            return Result.Failure(OrganizationErrors.NotAuthenticated);
        }

        var actor = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            actorId,
            cancellationToken);

        var target = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            request.UserId,
            cancellationToken);

        if (actor is null || !actor.CanManageMembers)
        {
            return Result.Failure(OrganizationErrors.CannotManageMembers);
        }

        if (target is null)
        {
            return Result.Failure(OrganizationErrors.MemberNotFound);
        }

        if (actor.Role is Domain.Common.Enums.OrganizationRole.Admin
            && target.Role is Domain.Common.Enums.OrganizationRole.Admin)
        {
            return Result.Failure(OrganizationErrors.CannotModerateMember);
        }

        try
        {
            target.Unblock(_timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return Result.Failure(OrganizationErrors.MemberNotFound);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
