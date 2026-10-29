namespace Sportner.Application.Features.Events.PendingJoinRequests;

public sealed record PendingJoinRequestApplicantResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl);

public sealed record PendingJoinRequestEventResponse(
    Guid EventId,
    string Title,
    DateTimeOffset EventDate,
    int PendingCount,
    IReadOnlyList<PendingJoinRequestApplicantResponse> Applicants);
