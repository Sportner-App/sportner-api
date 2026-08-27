using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserProfiles.DiscoverUsers;

public sealed record DiscoverUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? SportId = null,
    string? City = null) : IQuery<PagedResult<DiscoverUserItemResponse>>;

public sealed record DiscoverUserItemResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl,
    string? City);

public sealed class DiscoverUsersQueryValidator : AbstractValidator<DiscoverUsersQuery>
{
    public DiscoverUsersQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PaginationRequest.MaxPageSize);
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.City).MaximumLength(100);
    }
}

internal sealed class DiscoverUsersQueryHandler
    : IQueryHandler<DiscoverUsersQuery, PagedResult<DiscoverUserItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DiscoverUsersQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<DiscoverUserItemResponse>>> Handle(
        DiscoverUsersQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } viewerId)
        {
            return Result<PagedResult<DiscoverUserItemResponse>>.Failure(
                ProfileErrors.NotAuthenticated);
        }

        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var search = request.Search?.Trim().ToLowerInvariant();
        var city = request.City?.Trim().ToLowerInvariant();

        var query =
            from profile in _dbContext.UserProfiles.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking() on profile.UserId equals user.Id
            where profile.UserId != viewerId
                && profile.IsProfilePublic
                && user.Status == UserStatus.Active
                && !_dbContext.Friendships.AsNoTracking().Any(friendship =>
                    friendship.Status == FriendshipStatus.Blocked
                    && ((friendship.RequesterUserId == viewerId
                            && friendship.AddresseeUserId == profile.UserId)
                        || (friendship.AddresseeUserId == viewerId
                            && friendship.RequesterUserId == profile.UserId)))
            select profile;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(profile =>
                (profile.Username != null && profile.Username.ToLower().Contains(search))
                || (profile.FirstName != null && profile.FirstName.ToLower().Contains(search))
                || (profile.LastName != null && profile.LastName.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(profile =>
                profile.City != null && profile.City.ToLower() == city);
        }

        if (request.SportId is { } sportId)
        {
            query = query.Where(profile => _dbContext.UserSports.AsNoTracking().Any(userSport =>
                userSport.UserId == profile.UserId && userSport.SportId == sportId));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(profile => profile.FirstName ?? profile.Username)
            .ThenBy(profile => profile.Username)
            .ThenBy(profile => profile.UserId)
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .Select(profile => new DiscoverUserItemResponse(
                profile.UserId,
                profile.Username,
                profile.FirstName,
                profile.LastName,
                profile.ProfileImageUrl,
                profile.City))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<DiscoverUserItemResponse>>.Success(
            PagedResult<DiscoverUserItemResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                totalCount));
    }
}
