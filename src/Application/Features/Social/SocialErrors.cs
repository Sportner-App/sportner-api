using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social;

internal static class FriendshipErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Friendship.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Friendship.UserNotFound",
        "The user was not found.");

    internal static readonly Error NotFound = Error.NotFound(
        "Friendship.NotFound",
        "The friendship was not found.");

    internal static readonly Error SelfRequest = Error.Validation(
        "Friendship.SelfRequest",
        "Users cannot send a friend request to themselves.");

    internal static readonly Error AlreadyExists = Error.Conflict(
        "Friendship.AlreadyExists",
        "A friendship or request already exists between these users.");

    internal static readonly Error Blocked = Error.Forbidden(
        "Friendship.Blocked",
        "This relationship is blocked.");

    internal static readonly Error NotAddressee = Error.Forbidden(
        "Friendship.NotAddressee",
        "Only the addressee can respond to this request.");

    internal static readonly Error NotParticipant = Error.Forbidden(
        "Friendship.NotParticipant",
        "You are not a participant in this friendship.");

    internal static readonly Error NotAccepted = Error.Conflict(
        "Friendship.NotAccepted",
        "Only an accepted friendship can be removed this way.");

    internal static readonly Error NotVisible = Error.Forbidden(
        "Friendship.NotVisible",
        "Mutual friends are not available for this user.");
}

internal static class PostErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Post.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error NotFound = Error.NotFound(
        "Post.NotFound",
        "The post was not found.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Post.UserNotFound",
        "The user was not found.");

    internal static readonly Error CannotCreateContent = Error.Forbidden(
        "Post.CannotCreateContent",
        "This account cannot create content.");

    internal static readonly Error NotOwner = Error.Forbidden(
        "Post.NotOwner",
        "Only the post owner can perform this action.");

    internal static readonly Error Forbidden = Error.Forbidden(
        "Post.Forbidden",
        "You cannot view this post.");

    internal static readonly Error AlreadyLiked = Error.Conflict(
        "Post.AlreadyLiked",
        "You have already liked this post.");

    internal static readonly Error NotLiked = Error.NotFound(
        "Post.NotLiked",
        "You have not liked this post.");

    internal static readonly Error SelfLike = Error.Validation(
        "Post.SelfLike",
        "Users cannot like their own posts.");

    internal static readonly Error InvalidMedia = Error.Validation(
        "Post.InvalidMedia",
        "The uploaded file is missing or has an unsupported content type.");

    internal static readonly Error MediaNotFound = Error.NotFound(
        "Post.MediaNotFound",
        "The media item was not found.");

    internal static readonly Error CommentNotFound = Error.NotFound(
        "Post.CommentNotFound",
        "The comment was not found.");

    internal static readonly Error InvalidCursor = Error.Validation(
        "Post.InvalidCursor",
        "The pagination cursor is invalid.");
}
