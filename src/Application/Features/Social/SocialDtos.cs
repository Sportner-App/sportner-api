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
    DateTimeOffset CreatedAt,
    string? RequesterProfileImageUrl = null,
    string? AddresseeProfileImageUrl = null,
    int? MutualFriendsCount = null,
    IReadOnlyList<string>? SharedSportNames = null);

public sealed record FriendListItemResponse(
    Guid FriendshipId,
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    DateTimeOffset FriendsSince);

public sealed record FriendSuggestionItemResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    string? City,
    int MutualFriendsCount,
    int SharedSportsCount,
    bool SameCity,
    IReadOnlyList<string> SharedSportNames);

public sealed record MutualFriendItemResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl);

public sealed record MutualFriendsResponse(
    Guid UserId,
    int TotalCount,
    IReadOnlyList<MutualFriendItemResponse> Items);

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
    IReadOnlyList<PostMediaResponse> Media,
    IReadOnlyList<PostMentionResponse> Mentions);

/// <summary>
/// Gönderide etiketlenen kullanıcı. İstemci içerikteki "@kullanıcıadı"
/// yazısını buradaki <paramref name="Username"/> ile eşleştirip
/// <paramref name="UserId"/>'ye bağlıyor; etiket metne değil kimliğe
/// bağlandığı için kişi adını değiştirse de bağlantı doğru kalıyor.
/// </summary>
public sealed record PostMentionResponse(
    Guid UserId,
    string Username);

public sealed record CommentResponse(
    Guid Id,
    Guid PostId,
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    Guid? ParentCommentId,
    string Content,
    int LikeCount,
    int ReplyCount,
    DateTimeOffset CreatedAt,
    Guid? ReplyToUserId,
    string? ReplyToUsername,
    IReadOnlyList<PostMentionResponse> Mentions);
