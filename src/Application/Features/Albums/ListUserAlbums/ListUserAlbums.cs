using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Albums.ListUserAlbums;

public sealed record ListUserAlbumsQuery(Guid UserId) : IQuery<IReadOnlyList<AlbumListItemResponse>>;

internal sealed class ListUserAlbumsQueryHandler
    : IQueryHandler<ListUserAlbumsQuery, IReadOnlyList<AlbumListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListUserAlbumsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<AlbumListItemResponse>>> Handle(
        ListUserAlbumsQuery request,
        CancellationToken cancellationToken)
    {
        var albums = await _dbContext.Albums.AsNoTracking()
            .Where(album => album.Kind == AlbumKind.Profile && album.OwnerUserId == request.UserId)
            .OrderByDescending(album => album.CreatedAt)
            .ToListAsync(cancellationToken);

        var visible = new List<AlbumListItemResponse>();
        foreach (var album in albums)
        {
            if (await AlbumQueries.CanViewAsync(
                    _dbContext,
                    album,
                    _currentUser.UserId,
                    cancellationToken))
            {
                visible.Add(AlbumQueries.ToListItem(album));
            }
        }

        return Result<IReadOnlyList<AlbumListItemResponse>>.Success(visible);
    }
}
