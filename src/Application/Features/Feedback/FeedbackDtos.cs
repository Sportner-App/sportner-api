using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Feedback;

internal static class FeedbackErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "AppFeedback.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error TooFrequent = Error.TooManyRequests(
        "AppFeedback.TooFrequent",
        "Please wait a moment before sending another suggestion.");

    internal static readonly Error InvalidContent = Error.Validation(
        "AppFeedback.InvalidContent",
        "Feedback content is invalid.");
}

public sealed record AppFeedbackResponse(
    Guid Id,
    DateTimeOffset CreatedAt);
