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
}

/// <summary>İşlem kaydındaki sabit adlar; panel bunları Türkçe etikete çevirir.</summary>
internal static class AdminActions
{
    public const string BanUser = "user.ban";
    public const string UnbanUser = "user.unban";
    public const string RemoveAvatar = "user.remove_avatar";
    public const string FeedbackStatus = "feedback.status";
    public const string FeedbackReply = "feedback.reply";

    public const string UserTarget = "user";
    public const string FeedbackTarget = "feedback";

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
