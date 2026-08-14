using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Albums.UpdateAlbum;

public sealed record UpdateAlbumCommand(
    Guid AlbumId,
    string Title,
    string? Description,
    short Visibility) : ICommand<AlbumResponse>;

public sealed class UpdateAlbumCommandValidator : AbstractValidator<UpdateAlbumCommand>
{
    public UpdateAlbumCommandValidator()
    {
        RuleFor(command => command.AlbumId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

internal sealed class UpdateAlbumCommandHandler : ICommandHandler<UpdateAlbumCommand, AlbumResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateAlbumCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AlbumResponse>> Handle(
        UpdateAlbumCommand request,
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

        if (!Enum.IsDefined((AlbumVisibility)request.Visibility))
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.InvalidVisibility);
        }

        try
        {
            album.UpdateDetails(
                request.Title,
                request.Description,
                (AlbumVisibility)request.Visibility,
                _timeProvider.GetUtcNow());
        }
        catch (Domain.Common.Exceptions.DomainException)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.InvalidVisibility);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<AlbumResponse>.Success(AlbumQueries.ToResponse(album));
    }
}
