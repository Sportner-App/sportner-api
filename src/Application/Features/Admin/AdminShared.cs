using Sportner.Application.Common.Results;
using Sportner.Domain.Admin;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Admin;

internal static class AdminErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Admin.NotAuthenticated", ErrorMessagesResource.AppFeedback_NotAuthenticated);

    internal static Error InvalidCode => Error.Unauthorized(
        "Admin.InvalidCode", ErrorMessagesResource.Admin_InvalidCode);

    internal static Error CodeTooFrequent => Error.TooManyRequests(
        "Admin.CodeTooFrequent", ErrorMessagesResource.Admin_CodeTooFrequent);

    internal static Error UserNotFound => Error.NotFound(
        "Admin.UserNotFound", ErrorMessagesResource.Admin_UserNotFound);

    internal static Error InvalidUserState => Error.Conflict(
        "Admin.InvalidUserState", ErrorMessagesResource.Admin_InvalidUserState);

    internal static Error CannotModerateAdmin => Error.Forbidden(
        "Admin.CannotModerateAdmin", ErrorMessagesResource.Admin_CannotModerateAdmin);

    internal static Error FeedbackNotFound => Error.NotFound(
        "Admin.FeedbackNotFound", ErrorMessagesResource.Admin_FeedbackNotFound);

    internal static Error NoEmail => Error.Conflict(
        "Admin.NoEmail", ErrorMessagesResource.Admin_NoEmail);

    internal static Error InvalidReply => Error.Validation(
        "Admin.InvalidReply", ErrorMessagesResource.Admin_InvalidReply);

    internal static Error EmailFailed => Error.Failure(
        "Admin.EmailFailed", ErrorMessagesResource.Admin_EmailFailed);

    internal static Error ReportNotFound => Error.NotFound(
        "Admin.ReportNotFound", ErrorMessagesResource.Admin_ReportNotFound);

    internal static Error InvalidReportState => Error.Conflict(
        "Admin.InvalidReportState", ErrorMessagesResource.Admin_InvalidReportState);

    internal static Error PostNotFound => Error.NotFound(
        "Admin.PostNotFound", ErrorMessagesResource.Admin_PostNotFound);

    internal static Error CommentNotFound => Error.NotFound(
        "Admin.CommentNotFound", ErrorMessagesResource.Admin_CommentNotFound);

    internal static Error EventNotFound => Error.NotFound(
        "Admin.EventNotFound", ErrorMessagesResource.Admin_EventNotFound);

    internal static Error EventNotCancellable => Error.Conflict(
        "Admin.EventNotCancellable", ErrorMessagesResource.Admin_EventNotCancellable);

    internal static Error InvalidContentType => Error.Validation(
        "Admin.InvalidContentType", ErrorMessagesResource.Admin_InvalidContentType);

    internal static Error InvalidNote => Error.Validation(
        "Admin.InvalidNote", ErrorMessagesResource.Admin_InvalidNote);
}

/// <summary>İşlem kaydındaki sabit adlar; panel bunları Türkçe etikete çevirir.</summary>
internal static class AdminActions
{
    public const string BanUser = "user.ban";
    public const string UnbanUser = "user.unban";
    public const string RemoveAvatar = "user.remove_avatar";
    public const string FeedbackStatus = "feedback.status";
    public const string FeedbackReply = "feedback.reply";
    public const string ReportResolve = "report.resolve";
    public const string ReportReject = "report.reject";
    public const string PostHide = "post.hide";
    public const string PostUnhide = "post.unhide";
    public const string CommentHide = "comment.hide";
    public const string CommentUnhide = "comment.unhide";
    public const string EventCancel = "event.cancel";
    public const string UserTestAccount = "user.test_account";

    public const string UserTarget = "user";
    public const string FeedbackTarget = "feedback";
    public const string ReportTarget = "report";
    public const string PostTarget = "post";
    public const string CommentTarget = "comment";
    public const string EventTarget = "event";

    public static AdminAuditEntry Entry(
        Guid actor, string action, string targetType, Guid targetId, string? details, DateTimeOffset utcNow) =>
        AdminAuditEntry.Create(actor, action, targetType, targetId, details, utcNow);
}

public static class AdminPaging
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize) =>
        (Math.Max(page ?? 1, 1), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}
