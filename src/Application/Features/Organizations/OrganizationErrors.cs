using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Organizations;

internal static class OrganizationErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Organization.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Organization.UserNotFound",
        "The user was not found.");

    internal static readonly Error CannotCreateContent = Error.Forbidden(
        "Organization.CannotCreateContent",
        "This account cannot create content.");

    internal static readonly Error NotFound = Error.NotFound(
        "Organization.NotFound",
        "The organization was not found.");

    internal static readonly Error CityNotFound = Error.NotFound(
        "Organization.CityNotFound",
        "The city was not found.");

    internal static readonly Error InvalidInviteCode = Error.NotFound(
        "Organization.InvalidInviteCode",
        "The invite code was not found.");

    internal static readonly Error AlreadyMember = Error.Conflict(
        "Organization.AlreadyMember",
        "You are already a member of this organization.");

    internal static readonly Error AlreadyPending = Error.Conflict(
        "Organization.AlreadyPending",
        "A membership request is already pending.");

    internal static readonly Error NotApprovedMember = Error.Forbidden(
        "Organization.NotApprovedMember",
        "Only approved members can perform this action.");

    internal static readonly Error CannotManageMembers = Error.Forbidden(
        "Organization.CannotManageMembers",
        "Only the founder or an admin can manage members.");

    internal static readonly Error CannotCreateEvents = Error.Forbidden(
        "Organization.CannotCreateEvents",
        "Only approved organization members can create organization events.");

    internal static readonly Error CannotModerateMember = Error.Forbidden(
        "Organization.CannotModerateMember",
        "You cannot manage this member.");

    internal static readonly Error MemberBlocked = Error.Forbidden(
        "Organization.MemberBlocked",
        "This user is blocked from the organization.");

    internal static readonly Error NotFounder = Error.Forbidden(
        "Organization.NotFounder",
        "Only the founder can perform this action.");

    internal static readonly Error FounderCannotLeave = Error.Conflict(
        "Organization.FounderCannotLeave",
        "The founder cannot leave the organization.");

    internal static readonly Error MemberNotFound = Error.NotFound(
        "Organization.MemberNotFound",
        "The membership was not found.");

    internal static readonly Error InviteCodeUnavailable = Error.Conflict(
        "Organization.InviteCodeUnavailable",
        "A unique invite code could not be allocated. Try again.");
}
