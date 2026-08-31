using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.EventQuestions;

internal static class EventQuestionErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "EventQuestion.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error CannotCreateContent = Error.Forbidden(
        "EventQuestion.CannotCreateContent",
        "This account cannot create content.");

    internal static readonly Error EventNotFound = Error.NotFound(
        "EventQuestion.EventNotFound",
        "The event was not found.");

    internal static readonly Error QuestionNotFound = Error.NotFound(
        "EventQuestion.NotFound",
        "The question was not found.");

    internal static readonly Error Closed = Error.Conflict(
        "EventQuestion.Closed",
        "Questions are closed because the event has ended.");

    internal static readonly Error OrganizerCannotAsk = Error.Validation(
        "EventQuestion.OrganizerCannotAsk",
        "The organizer cannot ask a question on their own event.");

    internal static readonly Error Blocked = Error.Forbidden(
        "EventQuestion.Blocked",
        "This relationship is blocked.");

    internal static readonly Error TooFrequent = Error.TooManyRequests(
        "EventQuestion.TooFrequent",
        "Please wait a moment before sending another question.");

    internal static readonly Error InvalidContent = Error.Validation(
        "EventQuestion.InvalidContent",
        "The question content is invalid.");
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
