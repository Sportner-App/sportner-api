using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports.UpdateSportCoverImage;

internal sealed class UpdateSportCoverImageCommandHandler
    : ICommandHandler<UpdateSportCoverImageCommand, SportResponse>
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorage _fileStorage;
    private readonly TimeProvider _timeProvider;

    public UpdateSportCoverImageCommandHandler(
        IApplicationDbContext dbContext,
        IFileStorage fileStorage,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SportResponse>> Handle(
        UpdateSportCoverImageCommand request,
        CancellationToken cancellationToken)
    {
        var sport = await _dbContext.Sports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.SportId, cancellationToken);

        if (sport is null)
        {
            return Result<SportResponse>.Failure(SportErrors.NotFound);
        }

        string? storedPath = null;

        if (request.Content is not null)
        {
            if (request.ContentType is null || !AllowedContentTypes.Contains(request.ContentType))
            {
                return Result<SportResponse>.Failure(SportErrors.InvalidMedia);
            }

            var extension = Path.GetExtension(request.FileName);
            var objectPath = $"{sport.Id}/{Guid.NewGuid():N}{extension}";

            var uploadedPath = await _fileStorage.UploadAsync(
                StorageBuckets.SportCovers,
                objectPath,
                request.Content,
                request.ContentType,
                cancellationToken);

            storedPath = _fileStorage.GetPublicUrl(StorageBuckets.SportCovers, uploadedPath);
        }

        var previousPath = sport.CoverImageUrl;
        sport.ChangeCoverImage(storedPath, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!string.Equals(previousPath, storedPath, StringComparison.Ordinal))
        {
            await StorageCleanup.TryDeleteAsync(
                _fileStorage,
                StorageBuckets.SportCovers,
                previousPath,
                cancellationToken);
        }

        return Result<SportResponse>.Success(SportResponse.From(sport));
    }
}
