using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging;

internal static class MessagingErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Messaging.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error ConversationNotFound = Error.NotFound(
        "Messaging.ConversationNotFound",
        "The conversation was not found.");

    internal static readonly Error MessageNotFound = Error.NotFound(
        "Messaging.MessageNotFound",
        "The message was not found.");

    internal static readonly Error NotMember = Error.Forbidden(
        "Messaging.NotMember",
        "You are not an active member of this conversation.");

    internal static readonly Error CannotSend = Error.Forbidden(
        "Messaging.CannotSend",
        "You cannot send messages in this conversation.");

    internal static readonly Error ConversationClosed = Error.Conflict(
        "Messaging.ConversationClosed",
        "This conversation is closed.");

    internal static readonly Error NotSender = Error.Forbidden(
        "Messaging.NotSender",
        "Only the sender can modify this message.");

    internal static readonly Error InvalidMedia = Error.Validation(
        "Messaging.InvalidMedia",
        "The uploaded file is missing or has an unsupported content type.");

    internal static readonly Error InvalidCursor = Error.Validation(
        "Messaging.InvalidCursor",
        "The pagination cursor is invalid.");

    internal static readonly Error ReplyNotFound = Error.NotFound(
        "Messaging.ReplyNotFound",
        "The message being replied to was not found in this conversation.");
}
