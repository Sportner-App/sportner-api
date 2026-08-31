using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social;

internal static class BlockErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Block.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Block.UserNotFound",
        "The user was not found.");

    internal static readonly Error SelfBlock = Error.Validation(
        "Block.SelfBlock",
        "Users cannot block themselves.");
}

public sealed record BlockedUserResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    DateTimeOffset CreatedAt);
