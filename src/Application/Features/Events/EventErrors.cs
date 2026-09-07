using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür ve
/// istek anındaki <c>CurrentUICulture</c>'a göre dile göre değişir (bkz.
/// LocalizationExtension.cs). Bu yüzden değerler <c>static readonly</c> alan
/// değil, her erişimde yeniden hesaplanan property olmalı — aksi halde mesaj
/// yalnızca ilk kullanıldığı andaki dile göre sabitlenip önbelleğe alınır.
/// </summary>
internal static class EventErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Event.NotAuthenticated",
        ErrorMessagesResource.Event_NotAuthenticated);

    internal static Error NotFound => Error.NotFound(
        "Event.NotFound",
        ErrorMessagesResource.Event_NotFound);

    internal static Error SportNotFound => Error.NotFound(
        "Event.SportNotFound",
        ErrorMessagesResource.Event_SportNotFound);

    internal static Error SportInactive => Error.Validation(
        "Event.SportInactive",
        ErrorMessagesResource.Event_SportInactive);

    internal static Error UserNotFound => Error.NotFound(
        "Event.UserNotFound",
        ErrorMessagesResource.Event_UserNotFound);

    internal static Error CannotCreateContent => Error.Forbidden(
        "Event.CannotCreateContent",
        ErrorMessagesResource.Event_CannotCreateContent);

    internal static Error NotOrganizer => Error.Forbidden(
        "Event.NotOrganizer",
        ErrorMessagesResource.Event_NotOrganizer);

    internal static Error AlreadyApplied => Error.Conflict(
        "Event.AlreadyApplied",
        ErrorMessagesResource.Event_AlreadyApplied);

    internal static Error OrganizerCannotApply => Error.Validation(
        "Event.OrganizerCannotApply",
        ErrorMessagesResource.Event_OrganizerCannotApply);

    internal static Error ParticipantNotFound => Error.NotFound(
        "Event.ParticipantNotFound",
        ErrorMessagesResource.Event_ParticipantNotFound);

    internal static Error RemovalReasonNotFound => Error.Validation(
        "Event.RemovalReasonNotFound",
        ErrorMessagesResource.Event_RemovalReasonNotFound);

    internal static Error WaitlistEntryNotFound => Error.NotFound(
        "Event.WaitlistEntryNotFound",
        ErrorMessagesResource.Event_WaitlistEntryNotFound);

    internal static Error NotAcceptingApplications => Error.Conflict(
        "Event.NotAcceptingApplications",
        ErrorMessagesResource.Event_NotAcceptingApplications);

    internal static Error CapacityFull => Error.Conflict(
        "Event.CapacityFull",
        ErrorMessagesResource.Event_CapacityFull);

    internal static Error ParticipantAgeNotEligible => Error.Forbidden(
        "Event.ParticipantAgeNotEligible",
        ErrorMessagesResource.Event_ParticipantAgeNotEligible);

    internal static Error ParticipantBirthDateMissing => Error.Forbidden(
        "Event.ParticipantBirthDateMissing",
        ErrorMessagesResource.Event_ParticipantBirthDateMissing);

    internal static Error InvitationNotFound => Error.NotFound(
        "Event.InvitationNotFound",
        ErrorMessagesResource.Event_InvitationNotFound);

    internal static Error ParticipationLocked => Error.Conflict(
        "Event.ParticipationLocked",
        ErrorMessagesResource.Event_ParticipationLocked);

    internal static Error AssignmentEmpty => Error.Validation(
        "Event.AssignmentEmpty",
        ErrorMessagesResource.Event_AssignmentEmpty);

    internal static Error NotFriends => Error.Forbidden(
        "Event.NotFriends",
        ErrorMessagesResource.Event_NotFriends);

    internal static Error RelationshipBlocked => Error.Forbidden(
        "Event.RelationshipBlocked",
        ErrorMessagesResource.Event_RelationshipBlocked);

    internal static Error NotOrganizationMember => Error.Forbidden(
        "Event.NotOrganizationMember",
        ErrorMessagesResource.Event_NotOrganizationMember);

    internal static Error FriendAlreadyAssociated => Error.Conflict(
        "Event.FriendAlreadyAssociated",
        ErrorMessagesResource.Event_FriendAlreadyAssociated);
}
