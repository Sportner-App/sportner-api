using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Social;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class BlockErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Block.NotAuthenticated",
        ErrorMessagesResource.Block_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "Block.UserNotFound",
        ErrorMessagesResource.Block_UserNotFound);

    internal static Error SelfBlock => Error.Validation(
        "Block.SelfBlock",
        ErrorMessagesResource.Block_SelfBlock);
}

public sealed record BlockedUserResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    DateTimeOffset CreatedAt);
