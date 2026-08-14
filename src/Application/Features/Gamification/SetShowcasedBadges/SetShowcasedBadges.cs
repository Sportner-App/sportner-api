using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Badges;

namespace Sportner.Application.Features.Gamification.SetShowcasedBadges;

public sealed record SetShowcasedBadgesCommand(IReadOnlyList<Guid> BadgeIds)
    : ICommand<IReadOnlyList<UserBadgeResponse>>;

public sealed class SetShowcasedBadgesCommandValidator : AbstractValidator<SetShowcasedBadgesCommand>
{
    public SetShowcasedBadgesCommandValidator()
    {
        RuleFor(command => command.BadgeIds)
            .NotNull()
            .Must(ids => ids.Count <= UserBadge.MaxShowcaseSlots)
            .WithMessage($"At most {UserBadge.MaxShowcaseSlots} badges can be showcased.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Showcase badge ids must be unique.");
    }
}

internal sealed class SetShowcasedBadgesCommandHandler
    : ICommandHandler<SetShowcasedBadgesCommand, IReadOnlyList<UserBadgeResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SetShowcasedBadgesCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<UserBadgeResponse>>> Handle(
        SetShowcasedBadgesCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<UserBadgeResponse>>.Failure(BadgeErrors.NotAuthenticated);
        }

        var badgeIds = request.BadgeIds ?? [];
        if (badgeIds.Count > UserBadge.MaxShowcaseSlots)
        {
            return Result<IReadOnlyList<UserBadgeResponse>>.Failure(BadgeErrors.ShowcaseTooMany);
        }

        if (badgeIds.Distinct().Count() != badgeIds.Count)
        {
            return Result<IReadOnlyList<UserBadgeResponse>>.Failure(BadgeErrors.ShowcaseDuplicate);
        }

        var owned = await _dbContext.UserBadges
            .Where(userBadge => userBadge.UserId == userId)
            .ToListAsync(cancellationToken);

        if (badgeIds.Count > 0)
        {
            var ownedBadgeIds = owned.Select(userBadge => userBadge.BadgeId).ToHashSet();
            if (badgeIds.Any(id => !ownedBadgeIds.Contains(id)))
            {
                return Result<IReadOnlyList<UserBadgeResponse>>.Failure(BadgeErrors.ShowcaseNotOwned);
            }
        }

        var utcNow = _timeProvider.GetUtcNow();
        var selected = badgeIds
            .Select((badgeId, index) => (BadgeId: badgeId, Order: (short)(index + 1)))
            .ToDictionary(item => item.BadgeId, item => item.Order);

        foreach (var userBadge in owned)
        {
            if (selected.TryGetValue(userBadge.BadgeId, out var order))
            {
                userBadge.SetShowcased(order, utcNow);
            }
            else
            {
                userBadge.ClearShowcase(utcNow);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await BadgeQueries.ListForUserAsync(_dbContext, userId, cancellationToken);
        return Result<IReadOnlyList<UserBadgeResponse>>.Success(response);
    }
}
