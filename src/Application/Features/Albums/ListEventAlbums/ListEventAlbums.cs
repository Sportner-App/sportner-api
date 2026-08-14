using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Albums.ListEventAlbums;

public sealed record ListEventAlbumsQuery(Guid EventId) : IQuery<IReadOnlyList<AlbumListItemResponse>>;

internal sealed class ListEventAlbumsQueryHandler
    : IQueryHandler<ListEventAlbumsQuery, IReadOnlyList<AlbumListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListEventAlbumsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<AlbumListItemResponse>>> Handle(
        ListEventAlbumsQuery request,
        CancellationToken cancellationToken)
    {
        var eventExists = await _dbContext.Events.AsNoTracking()
            .AnyAsync(@event => @event.Id == request.EventId, cancellationToken);

        if (!eventExists)
        {
            return Result<IReadOnlyList<AlbumListItemResponse>>.Failure(AlbumErrors.EventNotFound);
        }

        var albums = await _dbContext.Albums.AsNoTracking()
            .Where(album => album.Kind == AlbumKind.Event && album.EventId == request.EventId)
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
