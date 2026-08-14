using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Albums.RemoveAlbumMedia;

public sealed record RemoveAlbumMediaCommand(Guid AlbumId, Guid MediaId) : ICommand<AlbumResponse>;

internal sealed class RemoveAlbumMediaCommandHandler
    : ICommandHandler<RemoveAlbumMediaCommand, AlbumResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;

    public RemoveAlbumMediaCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _fileStorage = fileStorage;
    }

    public async Task<Result<AlbumResponse>> Handle(
        RemoveAlbumMediaCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotAuthenticated);
        }

        var album = await _dbContext.Albums
            .Include(candidate => candidate.Media)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.AlbumId, cancellationToken);

        if (album is null)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotFound);
        }

        if (!await AlbumQueries.CanManageAsync(_dbContext, album, userId, cancellationToken))
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotOwner);
        }

        if (album.Media.All(item => item.Id != request.MediaId))
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.MediaNotFound);
        }

        var path = album.RemoveMedia(request.MediaId, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        await StorageCleanup.TryDeleteAsync(
            _fileStorage,
            StorageBuckets.Albums,
            path,
            cancellationToken);

        return Result<AlbumResponse>.Success(AlbumQueries.ToResponse(album));
    }
}
