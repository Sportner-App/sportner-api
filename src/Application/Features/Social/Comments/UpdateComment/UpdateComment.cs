using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social.Comments.CreateComment;

namespace Sportner.Application.Features.Social.Comments.UpdateComment;

public sealed record UpdateCommentCommand(Guid CommentId, string Content) : ICommand<CommentResponse>;

public sealed class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(command => command.CommentId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty().MaximumLength(1000);
    }
}

internal sealed class UpdateCommentCommandHandler
    : ICommandHandler<UpdateCommentCommand, CommentResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateCommentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CommentResponse>> Handle(
        UpdateCommentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<CommentResponse>.Failure(PostErrors.NotAuthenticated);
        }

        var comment = await _dbContext.PostComments
            .FirstOrDefaultAsync(candidate => candidate.Id == request.CommentId, cancellationToken);

        if (comment is null)
        {
            return Result<CommentResponse>.Failure(PostErrors.CommentNotFound);
        }

        if (comment.UserId != userId)
        {
            return Result<CommentResponse>.Failure(PostErrors.NotOwner);
        }

        comment.UpdateContent(request.Content, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CommentResponse>.Success(
            await CreateCommentCommandHandler.ToResponseAsync(_dbContext, comment, cancellationToken));
    }
}
