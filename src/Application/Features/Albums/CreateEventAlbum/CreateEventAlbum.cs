using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Albums.CreateEventAlbum;

public sealed record CreateEventAlbumCommand(
    Guid EventId,
    string Title,
    string? Description,
    short? Visibility = null) : ICommand<AlbumResponse>;

public sealed class CreateEventAlbumCommandValidator : AbstractValidator<CreateEventAlbumCommand>
{
    public CreateEventAlbumCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

internal sealed class CreateEventAlbumCommandHandler
    : ICommandHandler<CreateEventAlbumCommand, AlbumResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateEventAlbumCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AlbumResponse>> Handle(
        CreateEventAlbumCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotAuthenticated);
        }

        var @event = await _dbContext.Events.AsNoTracking()
            .Where(candidate => candidate.Id == request.EventId)
            .Select(candidate => new { candidate.Id, candidate.OrganizerUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (@event is null)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.EventNotFound);
        }

        if (@event.OrganizerUserId != userId)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.NotOrganizer);
        }

        var count = await _dbContext.Albums.AsNoTracking()
            .CountAsync(
                album => album.Kind == AlbumKind.Event && album.EventId == request.EventId,
                cancellationToken);

        if (count >= Album.MaxAlbumsPerEvent)
        {
            return Result<AlbumResponse>.Failure(AlbumErrors.EventAlbumLimit);
        }

        AlbumVisibility? visibility = null;
        if (request.Visibility is { } raw)
        {
            if (!Enum.IsDefined((AlbumVisibility)raw))
            {
                return Result<AlbumResponse>.Failure(AlbumErrors.InvalidVisibility);
            }

            visibility = (AlbumVisibility)raw;
        }

        try
        {
            var album = Album.CreateEventAlbum(
                request.EventId,
                request.Title,
                request.Description,
                visibility,
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
