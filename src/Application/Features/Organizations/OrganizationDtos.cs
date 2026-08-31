namespace Sportner.Application.Features.Organizations;

public sealed record OrganizationListItemResponse(
    Guid Id,
    string Name,
    string? CityName,
    short Role,
    short Status,
    int ApprovedMemberCount);

public sealed record OrganizationDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? CityId,
    string? CityName,
    Guid FounderUserId,
    short MyRole,
    short MyStatus,
    bool CanManageMembers,
    bool CanCreateEvents,
    bool CanRotateInviteCode,
    bool CanUpdateDetails,
    bool CanLeave,
    string? InviteCode,
    int ApprovedMemberCount,
    int PendingMemberCount,
    int BlockedMemberCount = 0);

public sealed record OrganizationMemberResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl,
    short Role,
    short Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt);
