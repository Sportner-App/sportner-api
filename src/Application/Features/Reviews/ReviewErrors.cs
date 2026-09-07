using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Reviews;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class ReviewErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Review.NotAuthenticated",
        ErrorMessagesResource.Review_NotAuthenticated);

    internal static Error NotFound => Error.NotFound(
        "Review.NotFound",
        ErrorMessagesResource.Review_NotFound);

    internal static Error EventNotFound => Error.NotFound(
        "Review.EventNotFound",
        ErrorMessagesResource.Review_EventNotFound);

    internal static Error EventNotCompleted => Error.Conflict(
        "Review.EventNotCompleted",
        ErrorMessagesResource.Review_EventNotCompleted);

    internal static Error NotEligible => Error.Forbidden(
        "Review.NotEligible",
        ErrorMessagesResource.Review_NotEligible);

    internal static Error SelfReview => Error.Validation(
        "Review.SelfReview",
        ErrorMessagesResource.Review_SelfReview);

    internal static Error AlreadyExists => Error.Conflict(
        "Review.AlreadyExists",
        ErrorMessagesResource.Review_AlreadyExists);

    internal static Error RelationshipBlocked => Error.Forbidden(
        "Review.RelationshipBlocked",
        ErrorMessagesResource.Review_RelationshipBlocked);

    internal static Error NotReviewer => Error.Forbidden(
        "Review.NotReviewer",
        ErrorMessagesResource.Review_NotReviewer);
}
