using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Media;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Organizations.UpdateOrganizationImage;

public sealed record UpdateOrganizationImageCommand(
    Guid OrganizationId,
    Stream? Content,
    string? ContentType,
    string? FileName) : ICommand<OrganizationDetailResponse>;

internal sealed class UpdateOrganizationImageCommandHandler
    : ICommandHandler<UpdateOrganizationImageCommand, OrganizationDetailResponse>
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly TimeProvider _timeProvider;

    public UpdateOrganizationImageCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IFileStorage fileStorage,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrganizationDetailResponse>> Handle(
        UpdateOrganizationImageCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        var membership = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            userId,
            cancellationToken);

        if (membership is null || !membership.CanManageMembers)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CannotManageMembers);
        }

        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(candidate => candidate.Id == request.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFound);
        }

        string? storedPath = null;

        if (request.Content is not null)
        {
            if (request.ContentType is null || !AllowedContentTypes.Contains(request.ContentType))
            {
                return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.InvalidMedia);
            }

            var extension = MediaFileExtension.FromContentType(request.ContentType);
            var objectPath = $"{organization.Id}/{Guid.NewGuid():N}{extension}";

            var uploadedPath = await _fileStorage.UploadAsync(
                StorageBuckets.ClubImages,
                objectPath,
                request.Content,
                request.ContentType,
                cancellationToken);

            storedPath = _fileStorage.GetPublicUrl(StorageBuckets.ClubImages, uploadedPath);
        }

        var previousPath = organization.ImageUrl;
        organization.ChangeImage(storedPath, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!string.Equals(previousPath, storedPath, StringComparison.Ordinal))
        {
            await StorageCleanup.TryDeleteAsync(
                _fileStorage,
                StorageBuckets.ClubImages,
                previousPath,
                cancellationToken);
        }

        var response = await OrganizationQueries.GetDetailAsync(
            _dbContext,
            organization.Id,
            userId,
            cancellationToken);

        return Result<OrganizationDetailResponse>.Success(response!);
    }
}
