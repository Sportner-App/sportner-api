using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Organizations;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class OrganizationErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Organization.NotAuthenticated",
        ErrorMessagesResource.Organization_NotAuthenticated);

    internal static Error UserNotFound => Error.NotFound(
        "Organization.UserNotFound",
        ErrorMessagesResource.Organization_UserNotFound);

    internal static Error CannotCreateContent => Error.Forbidden(
        "Organization.CannotCreateContent",
        ErrorMessagesResource.Organization_CannotCreateContent);

    internal static Error NotFound => Error.NotFound(
        "Organization.NotFound",
        ErrorMessagesResource.Organization_NotFound);

    internal static Error CityNotFound => Error.NotFound(
        "Organization.CityNotFound",
        ErrorMessagesResource.Organization_CityNotFound);

    internal static Error InvalidInviteCode => Error.NotFound(
        "Organization.InvalidInviteCode",
        ErrorMessagesResource.Organization_InvalidInviteCode);

    internal static Error AlreadyMember => Error.Conflict(
        "Organization.AlreadyMember",
        ErrorMessagesResource.Organization_AlreadyMember);

    internal static Error AlreadyPending => Error.Conflict(
        "Organization.AlreadyPending",
        ErrorMessagesResource.Organization_AlreadyPending);

    internal static Error NotApprovedMember => Error.Forbidden(
        "Organization.NotApprovedMember",
        ErrorMessagesResource.Organization_NotApprovedMember);

    internal static Error CannotManageMembers => Error.Forbidden(
        "Organization.CannotManageMembers",
        ErrorMessagesResource.Organization_CannotManageMembers);

    internal static Error CannotCreateEvents => Error.Forbidden(
        "Organization.CannotCreateEvents",
        ErrorMessagesResource.Organization_CannotCreateEvents);

    internal static Error CannotModerateMember => Error.Forbidden(
        "Organization.CannotModerateMember",
        ErrorMessagesResource.Organization_CannotModerateMember);

    internal static Error MemberBlocked => Error.Forbidden(
        "Organization.MemberBlocked",
        ErrorMessagesResource.Organization_MemberBlocked);

    internal static Error NotFounder => Error.Forbidden(
        "Organization.NotFounder",
        ErrorMessagesResource.Organization_NotFounder);

    internal static Error FounderCannotLeave => Error.Conflict(
        "Organization.FounderCannotLeave",
        ErrorMessagesResource.Organization_FounderCannotLeave);

    internal static Error MemberNotFound => Error.NotFound(
        "Organization.MemberNotFound",
        ErrorMessagesResource.Organization_MemberNotFound);

    internal static Error InviteCodeUnavailable => Error.Conflict(
        "Organization.InviteCodeUnavailable",
        ErrorMessagesResource.Organization_InviteCodeUnavailable);
}
