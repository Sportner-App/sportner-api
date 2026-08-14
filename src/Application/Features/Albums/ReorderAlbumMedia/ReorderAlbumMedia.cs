using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Albums.ReorderAlbumMedia;

public sealed record ReorderAlbumMediaCommand(Guid AlbumId, IReadOnlyList<Guid> OrderedMediaIds)
    : ICommand<AlbumResponse>;

public sealed class ReorderAlbumMediaCommandValidator : AbstractValidator<ReorderAlbumMediaCommand>
{
    public ReorderAlbumMediaCommandValidator()
    {
        RuleFor(command => command.AlbumId).NotEmpty();
        RuleFor(command => command.OrderedMediaIds).NotNull();
    }
}

internal sealed class ReorderAlbumMediaCommandHandler
    : ICommandHandler<ReorderAlbumMediaCommand, AlbumResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ReorderAlbumMediaCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AlbumResponse>> Handle(
        ReorderAlbumMediaCommand request,
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
            album.ReorderMedia(request.OrderedMediaIds, _timeProvider.GetUtcNow());
        }
        catch (Domain.Common.Exceptions.DomainException)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.MediaNotFound);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<AlbumResponse>.Success(AlbumQueries.ToResponse(album));
    }
}
