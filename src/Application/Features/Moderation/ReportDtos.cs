using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Moderation;

internal static class ReportErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Report.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error NotFound = Error.NotFound(
        "Report.NotFound",
        "The report was not found.");

    internal static readonly Error ReasonNotFound = Error.NotFound(
        "Report.ReasonNotFound",
        "The report reason was not found or is not selectable.");

    internal static readonly Error TargetNotFound = Error.NotFound(
        "Report.TargetNotFound",
        "The reported entity was not found.");

    internal static readonly Error AlreadyExists = Error.Conflict(
        "Report.AlreadyExists",
        "You have already reported this entity.");

    internal static readonly Error CannotReportSelf = Error.Validation(
        "Report.CannotReportSelf",
        "You cannot report your own content or profile.");

    internal static readonly Error InvalidEntityType = Error.Validation(
        "Report.InvalidEntityType",
        "The report entity type is invalid.");

    internal static readonly Error NotOwner = Error.Forbidden(
        "Report.NotOwner",
        "Only the reporter can update this report.");

    internal static readonly Error InvalidOperation = Error.Conflict(
        "Report.InvalidOperation",
        "The report cannot be updated in its current status.");
}

public sealed record ReportReasonResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    short DisplayOrder);

public sealed record ReportResponse(
    Guid Id,
    Guid ReporterUserId,
    short EntityType,
    Guid EntityId,
    Guid ReportReasonId,
    string? ReportReasonCode,
    string? ReportReasonName,
    string? Description,
    short Status,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? ResolutionNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
