using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.EventQuestions;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class EventQuestionErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "EventQuestion.NotAuthenticated",
        ErrorMessagesResource.EventQuestion_NotAuthenticated);

    internal static Error CannotCreateContent => Error.Forbidden(
        "EventQuestion.CannotCreateContent",
        ErrorMessagesResource.EventQuestion_CannotCreateContent);

    internal static Error EventNotFound => Error.NotFound(
        "EventQuestion.EventNotFound",
        ErrorMessagesResource.EventQuestion_EventNotFound);

    internal static Error QuestionNotFound => Error.NotFound(
        "EventQuestion.NotFound",
        ErrorMessagesResource.EventQuestion_NotFound);

    internal static Error Closed => Error.Conflict(
        "EventQuestion.Closed",
        ErrorMessagesResource.EventQuestion_Closed);

    internal static Error OrganizerCannotAsk => Error.Validation(
        "EventQuestion.OrganizerCannotAsk",
        ErrorMessagesResource.EventQuestion_OrganizerCannotAsk);

    internal static Error Blocked => Error.Forbidden(
        "EventQuestion.Blocked",
        ErrorMessagesResource.EventQuestion_Blocked);

    internal static Error TooFrequent => Error.TooManyRequests(
        "EventQuestion.TooFrequent",
        ErrorMessagesResource.EventQuestion_TooFrequent);

    internal static Error InvalidContent => Error.Validation(
        "EventQuestion.InvalidContent",
        ErrorMessagesResource.EventQuestion_InvalidContent);
}

public sealed record EventQuestionResponse(
    Guid Id,
    Guid EventId,
    Guid AuthorUserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    Guid? ParentId,
    Guid? ReplyToUserId,
    string? ReplyToUsername,
    string Content,
    int ReplyCount,
    short AuthorRole,
    DateTimeOffset CreatedAt,
    IReadOnlyList<EventQuestionResponse> Replies);
