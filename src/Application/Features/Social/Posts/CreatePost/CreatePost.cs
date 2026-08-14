using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social.Posts.CreatePost;

public sealed record CreatePostMediaInput(
    Stream Content,
    string ContentType,
    string FileName,
    long FileSize);

public sealed record CreatePostCommand(
    string? Content,
    IReadOnlyList<CreatePostMediaInput>? Media) : ICommand<PostResponse>;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(command => command.Content).MaximumLength(2200);
        RuleFor(command => command.Media).Must(media => media is null || media.Count <= 10)
            .WithMessage("A post may contain a maximum of 10 media items.");
    }
}

internal sealed class CreatePostCommandHandler : ICommandHandler<CreatePostCommand, PostResponse>
{
    private static readonly Dictionary<string, MediaType> AllowedContentTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = MediaType.Image,
        ["image/png"] = MediaType.Image,
        ["image/webp"] = MediaType.Image,
        ["video/mp4"] = MediaType.Video,
        ["video/quicktime"] = MediaType.Video,
        ["video/webm"] = MediaType.Video
    };

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;

    public CreatePostCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker)
    {
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
                if (!AllowedContentTypes.ContainsKey(item.ContentType) || item.FileSize <= 0)
                {
                    return Result<PostResponse>.Failure(PostErrors.InvalidMedia);
                }
            }
        }

        var utcNow = _timeProvider.GetUtcNow();
        var post = Post.Create(userId, request.Content, utcNow);

        if (request.Media is not null)
        {
            foreach (var item in request.Media)
            {
                var mediaType = AllowedContentTypes[item.ContentType];
                var extension = Path.GetExtension(item.FileName);
                var objectPath = $"{userId}/{post.Id}/{Guid.NewGuid():N}{extension}";

                var storedPath = await _fileStorage.UploadAsync(
                    StorageBuckets.PostMedia,
                    objectPath,
                    item.Content,
                    item.ContentType,
                    cancellationToken);

                post.AddMedia(
                    mediaType,
                    storedPath,
                    item.FileName,
                    item.ContentType,
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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<PostResponse>.Success(
            await SocialQueries.ToPostResponseAsync(_dbContext, post, userId, cancellationToken));
    }
}
