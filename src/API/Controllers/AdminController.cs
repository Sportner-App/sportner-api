using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.API.Extensions.RateLimiting;
using Sportner.Application.Features.Admin.Audit;
using Sportner.Application.Features.Admin.Auth;
using Sportner.Application.Features.Admin.Content;
using Sportner.Application.Features.Admin.Feedback;
using Sportner.Application.Features.Admin.Reports;
using Sportner.Application.Features.Admin.Users;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Feedback;

namespace Sportner.API.Controllers;

/// <summary>
/// Web sitesindeki yönetim paneli. Giriş uçları dışında her uç yönetici izin listesini
/// ister (<see cref="AuthorizationPolicies.Admin"/>); panel kodunun herkese açık olması
/// bir şey değiştirmez.
/// </summary>
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminController : ApiControllerBase
{
    [HttpPost("auth/code")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> RequestCode(
        [FromBody] RequestCodeBody body,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new RequestAdminLoginCodeCommand(body.Email), cancellationToken))
            .ToActionResult(StatusCodes.Status202Accepted);

    [HttpPost("auth/verify")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> VerifyCode(
        [FromBody] VerifyCodeBody body,
        CancellationToken cancellationToken) =>
        (await Sender.Send(
            new VerifyAdminLoginCodeCommand(
                body.Email,
                body.Code,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken)).ToActionResult();

    /// <summary>Panelin oturumun hâlâ yönetici olduğunu doğrulaması için.</summary>
    [HttpGet("me")]
    public IActionResult Me() => NoContent();

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search,
        [FromQuery] UserStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new ListAdminUsersQuery(search, status, page, pageSize), cancellationToken))
            .ToActionResult();

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken) =>
        (await Sender.Send(new GetAdminUserQuery(userId), cancellationToken)).ToActionResult();

    [HttpPost("users/{userId:guid}/ban")]
    public async Task<IActionResult> Ban(
        Guid userId,
        [FromBody] BanBody body,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new BanUserCommand(userId, body.Reason, body.RemoveAvatar), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("users/{userId:guid}/unban")]
    public async Task<IActionResult> Unban(Guid userId, CancellationToken cancellationToken) =>
        (await Sender.Send(new UnbanUserCommand(userId), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("users/{userId:guid}/test-account")]
    public async Task<IActionResult> SetTestAccount(
        Guid userId,
        [FromBody] TestAccountBody body,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new SetTestAccountCommand(userId, body.IsTestAccount), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpGet("users/{userId:guid}/content")]
    public async Task<IActionResult> ListUserContent(
        Guid userId,
        [FromQuery] string? type,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new ListAdminUserContentQuery(userId, type, page, pageSize), cancellationToken))
            .ToActionResult();

    [HttpGet("reports")]
    public async Task<IActionResult> ListReports(
        [FromQuery] short? status,
        [FromQuery] short? entityType,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new ListAdminReportsQuery(status, entityType, page, pageSize), cancellationToken))
            .ToActionResult();

    [HttpPost("reports/{reportId:guid}/resolve")]
    public async Task<IActionResult> ResolveReport(
        Guid reportId,
        [FromBody] NoteBody body,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new AdminResolveReportCommand(reportId, body.Note), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("reports/{reportId:guid}/reject")]
    public async Task<IActionResult> RejectReport(
        Guid reportId,
        [FromBody] NoteBody body,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new AdminRejectReportCommand(reportId, body.Note), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("posts/{postId:guid}/hide")]
    public async Task<IActionResult> HidePost(Guid postId, CancellationToken cancellationToken) =>
        (await Sender.Send(new SetPostHiddenCommand(postId, true), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("posts/{postId:guid}/unhide")]
    public async Task<IActionResult> UnhidePost(Guid postId, CancellationToken cancellationToken) =>
        (await Sender.Send(new SetPostHiddenCommand(postId, false), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("comments/{commentId:guid}/hide")]
    public async Task<IActionResult> HideComment(Guid commentId, CancellationToken cancellationToken) =>
        (await Sender.Send(new SetCommentHiddenCommand(commentId, true), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("comments/{commentId:guid}/unhide")]
    public async Task<IActionResult> UnhideComment(Guid commentId, CancellationToken cancellationToken) =>
        (await Sender.Send(new SetCommentHiddenCommand(commentId, false), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("events/{eventId:guid}/cancel")]
    public async Task<IActionResult> CancelEvent(Guid eventId, CancellationToken cancellationToken) =>
        (await Sender.Send(new AdminCancelEventCommand(eventId), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpGet("feedback")]
    public async Task<IActionResult> ListFeedback(
        [FromQuery] AppFeedbackStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new ListAdminFeedbackQuery(status, page, pageSize), cancellationToken))
            .ToActionResult();

    [HttpGet("feedback/{feedbackId:guid}")]
    public async Task<IActionResult> GetFeedback(Guid feedbackId, CancellationToken cancellationToken) =>
        (await Sender.Send(new GetAdminFeedbackQuery(feedbackId), cancellationToken)).ToActionResult();

    [HttpPut("feedback/{feedbackId:guid}/status")]
    public async Task<IActionResult> SetFeedbackStatus(
        Guid feedbackId,
        [FromBody] FeedbackStatusBody body,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new SetFeedbackStatusCommand(feedbackId, body.Status), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpPost("feedback/{feedbackId:guid}/reply")]
    public async Task<IActionResult> ReplyToFeedback(
        Guid feedbackId,
        [FromBody] ReplyBody body,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new ReplyToFeedbackCommand(feedbackId, body.Message), cancellationToken))
            .ToActionResult(StatusCodes.Status204NoContent);

    [HttpGet("audit")]
    public async Task<IActionResult> ListAudit(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        (await Sender.Send(new ListAdminAuditQuery(page, pageSize), cancellationToken)).ToActionResult();

    public sealed record RequestCodeBody(string Email);

    public sealed record VerifyCodeBody(string Email, string Code);

    public sealed record BanBody(string? Reason, bool RemoveAvatar);

    public sealed record FeedbackStatusBody(AppFeedbackStatus Status);

    public sealed record ReplyBody(string Message);

    public sealed record NoteBody(string Note);

    public sealed record TestAccountBody(bool IsTestAccount);
}
