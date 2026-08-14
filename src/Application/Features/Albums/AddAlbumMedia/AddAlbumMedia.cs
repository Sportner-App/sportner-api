using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Albums.AddAlbumMedia;

public sealed record AddAlbumMediaCommand(
    Guid AlbumId,
    Stream Content,
    string ContentType,
    string FileName,
    long FileSize) : ICommand<AlbumResponse>;

internal sealed class AddAlbumMediaCommandHandler : ICommandHandler<AddAlbumMediaCommand, AlbumResponse>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;

    public AddAlbumMediaCommandHandler(
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
        AddAlbumMediaCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotAuthenticated);
        }

        if (!AllowedContentTypes.Contains(request.ContentType) || request.FileSize <= 0)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.InvalidMedia);
        }

        var album = await _dbContext.Albums
            .Include(candidate => candidate.Media)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.AlbumId, cancellationToken);

        if (album is null)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotFound);
        }

        if (!await AlbumQueries.CanUploadMediaAsync(_dbContext, album, userId, cancellationToken))
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.CannotUpload);
        }

        var extension = Path.GetExtension(request.FileName);
        var objectPath = $"{userId}/{album.Id}/{Guid.NewGuid():N}{extension}";
        var storedPath = await _fileStorage.UploadAsync(
            StorageBuckets.Albums,
            objectPath,
            request.Content,
            request.ContentType,
            cancellationToken);

        var media = album.AddMedia(
            storedPath,
            request.FileName,
            request.ContentType,
            request.FileSize,
            userId,
            _timeProvider.GetUtcNow());

        _dbContext.MarkAsAdded(media);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<AlbumResponse>.Success(AlbumQueries.ToResponse(album));
    }
}
