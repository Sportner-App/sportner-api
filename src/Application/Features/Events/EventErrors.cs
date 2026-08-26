using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events;

internal static class EventErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Event.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error NotFound = Error.NotFound(
        "Event.NotFound",
        "The event was not found.");

    internal static readonly Error SportNotFound = Error.NotFound(
        "Event.SportNotFound",
        "The sport was not found.");

    internal static readonly Error SportInactive = Error.Validation(
        "Event.SportInactive",
        "The sport is not currently available.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Event.UserNotFound",
        "The user was not found.");

    internal static readonly Error CannotCreateContent = Error.Forbidden(
        "Event.CannotCreateContent",
        "This account cannot create content.");

    internal static readonly Error NotOrganizer = Error.Forbidden(
        "Event.NotOrganizer",
        "Only the organizer can perform this action.");

    internal static readonly Error AlreadyApplied = Error.Conflict(
        "Event.AlreadyApplied",
        "You are already associated with this event.");

    internal static readonly Error OrganizerCannotApply = Error.Validation(
        "Event.OrganizerCannotApply",
        "The organizer cannot apply to their own event.");

    internal static readonly Error ParticipantNotFound = Error.NotFound(
        "Event.ParticipantNotFound",
        "The participant was not found.");

    internal static readonly Error WaitlistEntryNotFound = Error.NotFound(
        "Event.WaitlistEntryNotFound",
        "The waitlist entry was not found.");

    internal static readonly Error NotAcceptingApplications = Error.Conflict(
        "Event.NotAcceptingApplications",
        "The event does not accept applications in its current status.");

    internal static readonly Error CapacityFull = Error.Conflict(
        "Event.CapacityFull",
        "The event capacity is full.");

    internal static readonly Error ParticipationLocked = Error.Conflict(
        "Event.ParticipationLocked",
        "Biten etkinlikten ayrılamazsın.");

    internal static readonly Error AssignmentEmpty = Error.Validation(
        "Event.AssignmentEmpty",
        "At least one guest or friend must be assigned.");

    internal static readonly Error NotFriends = Error.Forbidden(
        "Event.NotFriends",
        "Only accepted friends can be added to the event.");

    internal static readonly Error FriendAlreadyAssociated = Error.Conflict(
        "Event.FriendAlreadyAssociated",
        "One of the selected friends is already associated with this event.");
}
