using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Albums.ListMyAlbums;

public sealed record ListMyAlbumsQuery : IQuery<IReadOnlyList<AlbumListItemResponse>>;

internal sealed class ListMyAlbumsQueryHandler
    : IQueryHandler<ListMyAlbumsQuery, IReadOnlyList<AlbumListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyAlbumsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<AlbumListItemResponse>>> Handle(
        ListMyAlbumsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<AlbumListItemResponse>>.Failure(AlbumErrors.NotAuthenticated);
        }

        var albums = await _dbContext.Albums.AsNoTracking()
            .Where(album => album.Kind == AlbumKind.Profile && album.OwnerUserId == userId)
            .OrderByDescending(album => album.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AlbumListItemResponse>>.Success(
            albums.Select(AlbumQueries.ToListItem).ToList());
    }
}
