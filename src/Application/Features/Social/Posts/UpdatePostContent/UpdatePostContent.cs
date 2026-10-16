using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social.Posts;

namespace Sportner.Application.Features.Social.Posts.UpdatePostContent;

public sealed record UpdatePostContentCommand(
    Guid PostId,
    string? Content,
    IReadOnlyList<Guid>? MentionedUserIds = null) : ICommand<PostResponse>;

public sealed class UpdatePostContentCommandValidator : AbstractValidator<UpdatePostContentCommand>
{
    public UpdatePostContentCommandValidator()
    {
        RuleFor(command => command.PostId).NotEmpty();
        RuleFor(command => command.Content).MaximumLength(2200);
    }
}

internal sealed class UpdatePostContentCommandHandler
    : ICommandHandler<UpdatePostContentCommand, PostResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;

    public UpdatePostContentCommandHandler(
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

    public async Task<Result<PostResponse>> Handle(
        UpdatePostContentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<PostResponse>.Failure(PostErrors.NotAuthenticated);
        }

        var post = await _dbContext.Posts
            .Include(candidate => candidate.Media)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.PostId, cancellationToken);

        if (post is null)
        {
            return Result<PostResponse>.Failure(PostErrors.NotFound);
        }

        if (post.UserId != userId)
        {
            return Result<PostResponse>.Failure(PostErrors.NotOwner);
        }

        var mentions = await PostMentionResolver.ResolveAsync(
            _dbContext, userId, request.MentionedUserIds, cancellationToken);

        if (mentions.IsFailure)
        {
            return Result<PostResponse>.Failure(mentions.Errors);
        }

        var utcNow = _timeProvider.GetUtcNow();
        post.UpdateContent(request.Content, utcNow);
        post.SetMentions(mentions.Value!, utcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<PostResponse>.Success(
            await SocialQueries.ToPostResponseAsync(
                _dbContext,
                _fileStorage,
                post,
                userId,
                cancellationToken));
    }
}
