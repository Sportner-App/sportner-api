using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Application.Features.Quests;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social.Posts.CreatePost;

public sealed record CreatePostMediaInput(
    Stream Content,
    string ContentType,
    string FileName,
    long FileSize);

public sealed record CreatePostCommand(
    string? Content,
    IReadOnlyList<CreatePostMediaInput>? Media,
    IReadOnlyList<Guid>? MentionedUserIds = null) : ICommand<PostResponse>;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(command => command.Content).MaximumLength(2200);
        RuleFor(command => command.Media).Must(media => media is null || media.Count <= 10)
            .WithMessage("A post may contain a maximum of 10 media items.");
        RuleFor(command => command.MentionedUserIds)
            .Must(ids => ids is null || ids.Count <= 20)
            .WithMessage("A post may mention a maximum of 20 people.");
    }
}

internal sealed class CreatePostCommandHandler : ICommandHandler<CreatePostCommand, PostResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;
    private readonly INotificationPublisher _notificationPublisher;

    public CreatePostCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
        INotificationPublisher notificationPublisher)
    {
        _notificationPublisher = notificationPublisher;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _fileStorage = fileStorage;
        _badgeAwarder = badgeAwarder;
        _questProgressTracker = questProgressTracker;
    }

    public async Task<Result<PostResponse>> Handle(
        CreatePostCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<PostResponse>.Failure(PostErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<PostResponse>.Failure(PostErrors.UserNotFound);
        }

        if (!user.CanCreateContent())
        {
            return Result<PostResponse>.Failure(PostErrors.CannotCreateContent);
        }

        if (request.Media is not null)
        {
            foreach (var item in request.Media)
            {
                if (!PostMediaContentTypes.TryResolve(
                        item.ContentType,
                        item.FileName,
                        out _,
                        out _)
                    || item.FileSize <= 0)
                {
                    return Result<PostResponse>.Failure(PostErrors.InvalidMedia);
                }
            }
        }

        var mentions = await PostMentionResolver.ResolveAsync(
            _dbContext, userId, request.MentionedUserIds, cancellationToken);

        if (mentions.IsFailure)
        {
            return Result<PostResponse>.Failure(mentions.Errors);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var post = Post.Create(userId, request.Content, utcNow);
        post.SetMentions(mentions.Value!, utcNow);

        if (request.Media is not null)
        {
            foreach (var item in request.Media)
            {
                PostMediaContentTypes.TryResolve(
                    item.ContentType,
                    item.FileName,
                    out var contentType,
                    out var mediaType);

                var extension = PostMediaContentTypes.ResolveExtension(item.FileName, contentType);
                var objectPath = $"{userId}/{post.Id}/{Guid.NewGuid():N}{extension}";

                var storedPath = await _fileStorage.UploadAsync(
                    StorageBuckets.PostMedia,
                    objectPath,
                    item.Content,
                    contentType,
                    cancellationToken);

                post.AddMedia(
                    mediaType,
                    storedPath,
                    item.FileName,
                    contentType,
                    item.FileSize,
                    utcNow);
            }
        }

        post.ValidatePublishable();

        _dbContext.Posts.Add(post);

        var statistics = await _dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        statistics?.IncreasePostsCount(utcNow);

        var priorCount = await _dbContext.Posts.AsNoTracking()
            .CountAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (priorCount == 0)
        {
            await _badgeAwarder.TryAwardAsync(userId, BadgeCodes.FirstPost, cancellationToken);
        }

        await _questProgressTracker.ReportAsync(
            userId,
            QuestMetrics.PostsCreated,
            1,
            cancellationToken);

        await NotifyMentionedAsync(userId, post, mentions.Value!, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<PostResponse>.Success(
            await SocialQueries.ToPostResponseAsync(
                _dbContext,
                _fileStorage,
                post,
                userId,
                cancellationToken));
    }

    private async Task NotifyMentionedAsync(
        Guid authorUserId,
        Post post,
        IReadOnlyList<(Guid UserId, string Username)> mentions,
        CancellationToken cancellationToken)
    {
        if (mentions.Count == 0)
        {
            return;
        }

        var authorUsername = await NotificationActor.ResolveUsernameAsync(
            _dbContext, authorUserId, cancellationToken);

        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext,
            mentions.Select(mention => mention.UserId).ToList(),
            cancellationToken);

        foreach (var mention in mentions)
        {
            var language = languages.GetValueOrDefault(mention.UserId);
            var text = NotificationActor.Format(
                language,
                nameof(NotificationsResource.PostMentioned_Body),
                NotificationActor.FormatPrefix(authorUsername, language));

            await _notificationPublisher.PublishAsync(
                mention.UserId,
                NotificationType.PostMentioned,
                NotificationActor.Format(
                    language, nameof(NotificationsResource.PostMentioned_Title)),
                text,
                NotificationEntityType.Post,
                post.Id,
                authorUserId,
                cancellationToken);
        }
    }
}
