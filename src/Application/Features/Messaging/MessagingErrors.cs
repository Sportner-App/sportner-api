using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Messaging;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class MessagingErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Messaging.NotAuthenticated",
        ErrorMessagesResource.Messaging_NotAuthenticated);

    internal static Error ConversationNotFound => Error.NotFound(
        "Messaging.ConversationNotFound",
        ErrorMessagesResource.Messaging_ConversationNotFound);

    internal static Error MessageNotFound => Error.NotFound(
        "Messaging.MessageNotFound",
        ErrorMessagesResource.Messaging_MessageNotFound);

    internal static Error NotMember => Error.Forbidden(
        "Messaging.NotMember",
        ErrorMessagesResource.Messaging_NotMember);

    internal static Error CannotSend => Error.Forbidden(
        "Messaging.CannotSend",
        ErrorMessagesResource.Messaging_CannotSend);

    internal static Error ConversationClosed => Error.Conflict(
        "Messaging.ConversationClosed",
        ErrorMessagesResource.Messaging_ConversationClosed);

    internal static Error NotSender => Error.Forbidden(
        "Messaging.NotSender",
        ErrorMessagesResource.Messaging_NotSender);

    internal static Error InvalidMedia => Error.Validation(
        "Messaging.InvalidMedia",
        ErrorMessagesResource.Messaging_InvalidMedia);

    internal static Error InvalidCursor => Error.Validation(
        "Messaging.InvalidCursor",
        ErrorMessagesResource.Messaging_InvalidCursor);

    internal static Error ReplyNotFound => Error.NotFound(
        "Messaging.ReplyNotFound",
        ErrorMessagesResource.Messaging_ReplyNotFound);

    internal static Error PeerNotFound => Error.NotFound(
        "Messaging.PeerNotFound",
        ErrorMessagesResource.Messaging_PeerNotFound);

    internal static Error NotFriends => Error.Forbidden(
        "Messaging.NotFriends",
        ErrorMessagesResource.Messaging_NotFriends);

    internal static Error Blocked => Error.Forbidden(
        "Messaging.Blocked",
        ErrorMessagesResource.Messaging_Blocked);

    internal static Error CannotMessageSelf => Error.Validation(
        "Messaging.CannotMessageSelf",
        ErrorMessagesResource.Messaging_CannotMessageSelf);

    internal static Error CannotInvite => Error.Forbidden(
        "Messaging.CannotInvite",
        ErrorMessagesResource.Messaging_CannotInvite);

    internal static Error GroupFull => Error.Conflict(
        "Messaging.GroupFull",
        ErrorMessagesResource.Messaging_GroupFull);

    internal static Error InvalidOperation => Error.Conflict(
        "Messaging.InvalidOperation",
        ErrorMessagesResource.Messaging_InvalidOperation);

    internal static Error UserNotFound => Error.NotFound(
        "Messaging.UserNotFound",
        ErrorMessagesResource.Messaging_UserNotFound);
}
