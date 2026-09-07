using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Social;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class FriendshipErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Friendship.NotAuthenticated",
        ErrorMessagesResource.Friendship_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "Friendship.UserNotFound",
        ErrorMessagesResource.Friendship_UserNotFound);

    internal static Error NotFound => Error.NotFound(
        "Friendship.NotFound",
        ErrorMessagesResource.Friendship_NotFound);

    internal static Error SelfRequest => Error.Validation(
        "Friendship.SelfRequest",
        ErrorMessagesResource.Friendship_SelfRequest);

    internal static Error AlreadyExists => Error.Conflict(
        "Friendship.AlreadyExists",
        ErrorMessagesResource.Friendship_AlreadyExists);

    internal static Error Blocked => Error.Forbidden(
        "Friendship.Blocked",
        ErrorMessagesResource.Friendship_Blocked);

    internal static Error NotAddressee => Error.Forbidden(
        "Friendship.NotAddressee",
        ErrorMessagesResource.Friendship_NotAddressee);

    internal static Error NotParticipant => Error.Forbidden(
        "Friendship.NotParticipant",
        ErrorMessagesResource.Friendship_NotParticipant);

    internal static Error NotAccepted => Error.Conflict(
        "Friendship.NotAccepted",
        ErrorMessagesResource.Friendship_NotAccepted);

    internal static Error NotVisible => Error.Forbidden(
        "Friendship.NotVisible",
        ErrorMessagesResource.Friendship_NotVisible);
}

internal static class PostErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Post.NotAuthenticated",
        ErrorMessagesResource.Post_NotAuthenticated);

    internal static Error NotFound => Error.NotFound(
        "Post.NotFound",
        ErrorMessagesResource.Post_NotFound);

    internal static Error UserNotFound => Error.NotFound(
        "Post.UserNotFound",
        ErrorMessagesResource.Post_UserNotFound);

    internal static Error CannotCreateContent => Error.Forbidden(
        "Post.CannotCreateContent",
        ErrorMessagesResource.Post_CannotCreateContent);

    internal static Error NotOwner => Error.Forbidden(
        "Post.NotOwner",
        ErrorMessagesResource.Post_NotOwner);

    internal static Error Forbidden => Error.Forbidden(
        "Post.Forbidden",
        ErrorMessagesResource.Post_Forbidden);

    internal static Error AlreadyLiked => Error.Conflict(
        "Post.AlreadyLiked",
        ErrorMessagesResource.Post_AlreadyLiked);

    internal static Error NotLiked => Error.NotFound(
        "Post.NotLiked",
        ErrorMessagesResource.Post_NotLiked);

    internal static Error SelfLike => Error.Validation(
        "Post.SelfLike",
        ErrorMessagesResource.Post_SelfLike);

    internal static Error InvalidMedia => Error.Validation(
        "Post.InvalidMedia",
        ErrorMessagesResource.Post_InvalidMedia);

    internal static Error MediaNotFound => Error.NotFound(
        "Post.MediaNotFound",
        ErrorMessagesResource.Post_MediaNotFound);

    internal static Error CommentNotFound => Error.NotFound(
        "Post.CommentNotFound",
        ErrorMessagesResource.Post_CommentNotFound);

    internal static Error InvalidCursor => Error.Validation(
        "Post.InvalidCursor",
        ErrorMessagesResource.Post_InvalidCursor);
}
