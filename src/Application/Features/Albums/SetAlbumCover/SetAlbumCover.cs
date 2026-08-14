using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Albums.SetAlbumCover;

public sealed record SetAlbumCoverCommand(Guid AlbumId, Guid MediaId) : ICommand<AlbumResponse>;

internal sealed class SetAlbumCoverCommandHandler : ICommandHandler<SetAlbumCoverCommand, AlbumResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SetAlbumCoverCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AlbumResponse>> Handle(
        SetAlbumCoverCommand request,
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

        try
        {
            album.SetCover(request.MediaId, _timeProvider.GetUtcNow());
        }
        catch (Domain.Common.Exceptions.DomainException)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.MediaNotFound);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<AlbumResponse>.Success(AlbumQueries.ToResponse(album));
    }
}
