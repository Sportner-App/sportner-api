using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Gamification.ListBadges;

public sealed record ListBadgesQuery(short? Category = null, bool? Earned = null)
    : IQuery<IReadOnlyList<BadgeResponse>>;

public sealed class ListBadgesQueryValidator : AbstractValidator<ListBadgesQuery>
{
    public ListBadgesQueryValidator()
    {
        RuleFor(query => query.Category)
            .Must(value => value is null || Enum.IsDefined(typeof(BadgeCategory), (BadgeCategory)value.Value))
            .WithMessage("Badge category is invalid.");
    }
}

internal sealed class ListBadgesQueryHandler
    : IQueryHandler<ListBadgesQuery, IReadOnlyList<BadgeResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListBadgesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<BadgeResponse>>> Handle(
        ListBadgesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Earned is not null && _currentUser.UserId is null)
        {
            return Result<IReadOnlyList<BadgeResponse>>.Failure(BadgeErrors.NotAuthenticated);
        }

        var query = _dbContext.Badges.AsNoTracking().Where(badge => badge.IsActive);

        if (request.Category is { } category)
        {
            var typed = (BadgeCategory)category;
            query = query.Where(badge => badge.Category == typed);
        }

        var badges = await query
            .OrderBy(badge => badge.DisplayOrder)
            .ThenBy(badge => badge.Code)
            .ToListAsync(cancellationToken);

        HashSet<Guid>? earnedIds = null;
        if (_currentUser.UserId is { } userId)
        {
            var ids = await _dbContext.UserBadges.AsNoTracking()
                .Where(userBadge => userBadge.UserId == userId)
                .Select(userBadge => userBadge.BadgeId)
                .ToListAsync(cancellationToken);
            earnedIds = ids.ToHashSet();
        }

        IEnumerable<Badge> filtered = badges;
        if (request.Earned is { } earnedFilter && earnedIds is not null)
        {
            filtered = earnedFilter
                ? badges.Where(badge => earnedIds.Contains(badge.Id))
                : badges.Where(badge => !earnedIds.Contains(badge.Id));
        }

        var items = filtered
            .Select(badge => new BadgeResponse(
                badge.Id,
                badge.Code,
                badge.Name,
                badge.Description,
                badge.IconPath,
                (short)badge.Category,
                (short)badge.Rarity,
                badge.ExperiencePoints,
                badge.DisplayOrder,
                earnedIds is null ? null : earnedIds.Contains(badge.Id)))
            .ToList();

        return Result<IReadOnlyList<BadgeResponse>>.Success(items);
    }
}
