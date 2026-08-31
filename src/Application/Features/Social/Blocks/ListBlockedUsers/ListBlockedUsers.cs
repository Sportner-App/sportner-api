using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Blocks.ListBlockedUsers;

public sealed record ListBlockedUsersQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<BlockedUserResponse>>;

internal sealed class ListBlockedUsersQueryHandler
    : IQueryHandler<ListBlockedUsersQuery, PagedResult<BlockedUserResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListBlockedUsersQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<BlockedUserResponse>>> Handle(
        ListBlockedUsersQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } blockerId)
        {
            return Result<PagedResult<BlockedUserResponse>>.Failure(BlockErrors.NotAuthenticated);
        }

        var pagination = new PaginationRequest(request.Page, request.PageSize);

        var query =
            from block in _dbContext.UserBlocks.AsNoTracking()
            where block.BlockerUserId == blockerId
            join profile in _dbContext.UserProfiles.AsNoTracking()
                on block.BlockedUserId equals profile.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            orderby block.CreatedAt descending
            select new BlockedUserResponse(
                block.BlockedUserId,
                profile != null ? profile.Username : null,
                profile != null ? profile.FirstName : null,
                profile != null ? profile.ProfileImageUrl : null,
                block.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<BlockedUserResponse>>.Success(
            PagedResult<BlockedUserResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
