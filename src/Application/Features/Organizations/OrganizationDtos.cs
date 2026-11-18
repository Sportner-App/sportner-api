namespace Sportner.Application.Features.Organizations;

public sealed record OrganizationListItemResponse(
    Guid Id,
    string Name,
    string? CityName,
    short Role,
    short Status,
    int ApprovedMemberCount,
    string? ImageUrl = null,
    bool IsPublic = false,
    bool CanCreateEvents = false);

public sealed record OrganizationDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? CityId,
    string? CityName,
    Guid FounderUserId,
    short? MyRole,
    short? MyStatus,
    bool CanManageMembers,
    bool CanCreateEvents,
    bool CanRotateInviteCode,
    bool CanUpdateDetails,
    bool CanLeave,
    string? InviteCode,
    int ApprovedMemberCount,
    int PendingMemberCount,
    int BlockedMemberCount = 0,
    bool IsPublic = false,
    string? ImageUrl = null,
    bool CanJoin = false,
    short EventCreationPolicy = 0,
    bool CanDelete = false,
    bool CanTransferFounder = false,
    int CompletedEventCount = 0);

/// <summary>Herkese açık kulüp keşif listesi satırı.</summary>
public sealed record OrganizationSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    string? CityName,
    int ApprovedMemberCount,
    bool IsPublic,
    short? MyStatus);

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
