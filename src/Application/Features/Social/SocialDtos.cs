namespace Sportner.Application.Features.Social;

public sealed record FriendshipResponse(
    Guid Id,
    Guid RequesterUserId,
    string? RequesterUsername,
    string? RequesterFirstName,
    Guid AddresseeUserId,
    string? AddresseeUsername,
    string? AddresseeFirstName,
    short Status,
    DateTimeOffset? RespondedAt,
    DateTimeOffset CreatedAt);

public sealed record FriendListItemResponse(
    Guid FriendshipId,
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    DateTimeOffset FriendsSince);

public sealed record PostMediaResponse(
    Guid Id,
    short MediaType,
    string StoragePath,
    string FileName,
    string MimeType,
    long FileSize,
    int? Width,
    int? Height,
    int? DurationSeconds,
    short DisplayOrder);

public sealed record PostResponse(
    Guid Id,
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    string? Content,
    int LikeCount,
    int CommentCount,
    short MediaCount,
    bool LikedByMe,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PostMediaResponse> Media);

public sealed record CommentResponse(
    Guid Id,
    Guid PostId,
    Guid UserId,
    string? Username,
    string? FirstName,
    Guid? ParentCommentId,
    string Content,
    int LikeCount,
    int ReplyCount,
    DateTimeOffset CreatedAt);
