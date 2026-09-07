using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Feedback;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class FeedbackErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "AppFeedback.NotAuthenticated",
        ErrorMessagesResource.AppFeedback_NotAuthenticated);

    internal static Error TooFrequent => Error.TooManyRequests(
        "AppFeedback.TooFrequent",
        ErrorMessagesResource.AppFeedback_TooFrequent);

    internal static Error InvalidContent => Error.Validation(
        "AppFeedback.InvalidContent",
        ErrorMessagesResource.AppFeedback_InvalidContent);
}

public sealed record AppFeedbackResponse(
    Guid Id,
    DateTimeOffset CreatedAt);
