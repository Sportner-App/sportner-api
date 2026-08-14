using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Albums.DeleteAlbum;

public sealed record DeleteAlbumCommand(Guid AlbumId) : ICommand;

internal sealed class DeleteAlbumCommandHandler : ICommandHandler<DeleteAlbumCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    public DeleteAlbumCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<Result> Handle(DeleteAlbumCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(AlbumErrors.NotAuthenticated);
        }

        var album = await _dbContext.Albums
            .Include(candidate => candidate.Media)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.AlbumId, cancellationToken);

        if (album is null)
        {
            return Result.Failure(AlbumErrors.NotFound);
        }

        if (!await AlbumQueries.CanManageAsync(_dbContext, album, userId, cancellationToken))
        {
            return Result.Failure(AlbumErrors.NotOwner);
        }

        var paths = album.CollectStoragePaths();
        _dbContext.Albums.Remove(album);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var path in paths)
        {
            await StorageCleanup.TryDeleteAsync(
                _fileStorage,
                StorageBuckets.Albums,
                path,
                cancellationToken);
        }

        return Result.Success();
    }
}
