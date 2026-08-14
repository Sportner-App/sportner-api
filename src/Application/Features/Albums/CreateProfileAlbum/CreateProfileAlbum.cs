using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Albums.CreateProfileAlbum;

public sealed record CreateProfileAlbumCommand(
    string Title,
    string? Description,
    short Visibility = (short)AlbumVisibility.Private) : ICommand<AlbumResponse>;

public sealed class CreateProfileAlbumCommandValidator : AbstractValidator<CreateProfileAlbumCommand>
{
    public CreateProfileAlbumCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

internal sealed class CreateProfileAlbumCommandHandler
    : ICommandHandler<CreateProfileAlbumCommand, AlbumResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateProfileAlbumCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AlbumResponse>> Handle(
        CreateProfileAlbumCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotAuthenticated);
        }

        if (!Enum.IsDefined((AlbumVisibility)request.Visibility))
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.InvalidVisibility);
        }

        var count = await _dbContext.Albums.AsNoTracking()
            .CountAsync(
                album => album.Kind == AlbumKind.Profile && album.OwnerUserId == userId,
                cancellationToken);

        if (count >= Album.MaxAlbumsPerProfile)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.ProfileAlbumLimit);
        }

        try
        {
            var album = Album.CreateProfileAlbum(
                userId,
                request.Title,
                request.Description,
                (AlbumVisibility)request.Visibility,
                _timeProvider.GetUtcNow());

            _dbContext.Albums.Add(album);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<AlbumResponse>.Success(AlbumQueries.ToResponse(album));
        }
        catch (Domain.Common.Exceptions.DomainException)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.InvalidVisibility);
        }
    }
}
