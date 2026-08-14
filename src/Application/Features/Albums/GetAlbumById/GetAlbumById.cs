using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Albums.GetAlbumById;

public sealed record GetAlbumByIdQuery(Guid AlbumId) : IQuery<AlbumResponse>;

internal sealed class GetAlbumByIdQueryHandler : IQueryHandler<GetAlbumByIdQuery, AlbumResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetAlbumByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<AlbumResponse>> Handle(
        GetAlbumByIdQuery request,
        CancellationToken cancellationToken)
    {
        var album = await _dbContext.Albums.AsNoTracking()
            .Include(candidate => candidate.Media)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.AlbumId, cancellationToken);

        if (album is null)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotFound);
        }

        if (!await AlbumQueries.CanViewAsync(
                _dbContext,
                album,
                _currentUser.UserId,
                cancellationToken))
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.Forbidden);
        }

        return Result<AlbumResponse>.Success(AlbumQueries.ToResponse(album));
    }
}
