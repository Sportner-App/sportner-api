using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Moderation;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class ReportErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Report.NotAuthenticated",
        ErrorMessagesResource.Report_NotAuthenticated);

    internal static Error NotFound => Error.NotFound(
        "Report.NotFound",
        ErrorMessagesResource.Report_NotFound);

    internal static Error ReasonNotFound => Error.NotFound(
        "Report.ReasonNotFound",
        ErrorMessagesResource.Report_ReasonNotFound);

    internal static Error TargetNotFound => Error.NotFound(
        "Report.TargetNotFound",
        ErrorMessagesResource.Report_TargetNotFound);

    internal static Error AlreadyExists => Error.Conflict(
        "Report.AlreadyExists",
        ErrorMessagesResource.Report_AlreadyExists);

    internal static Error CannotReportSelf => Error.Validation(
        "Report.CannotReportSelf",
        ErrorMessagesResource.Report_CannotReportSelf);

    internal static Error InvalidEntityType => Error.Validation(
        "Report.InvalidEntityType",
        ErrorMessagesResource.Report_InvalidEntityType);

    internal static Error NotOwner => Error.Forbidden(
        "Report.NotOwner",
        ErrorMessagesResource.Report_NotOwner);

    internal static Error InvalidOperation => Error.Conflict(
        "Report.InvalidOperation",
        ErrorMessagesResource.Report_InvalidOperation);
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
