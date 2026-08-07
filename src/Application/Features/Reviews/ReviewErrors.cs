using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Reviews;

internal static class ReviewErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Review.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error NotFound = Error.NotFound(
        "Review.NotFound",
        "The review was not found.");

    internal static readonly Error EventNotFound = Error.NotFound(
        "Review.EventNotFound",
        "The event was not found.");

    internal static readonly Error EventNotCompleted = Error.Conflict(
        "Review.EventNotCompleted",
        "Reviews can only be created for completed events.");

    internal static readonly Error NotEligible = Error.Forbidden(
        "Review.NotEligible",
        "Both participants must have attended the event to exchange reviews.");

    internal static readonly Error SelfReview = Error.Validation(
        "Review.SelfReview",
        "Users cannot review themselves.");

    internal static readonly Error AlreadyExists = Error.Conflict(
        "Review.AlreadyExists",
        "A review for this participant on this event already exists.");

    internal static readonly Error NotReviewer = Error.Forbidden(
        "Review.NotReviewer",
        "Only the reviewer can update this review.");
}
