using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Email;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Feedback;

namespace Sportner.Application.Features.Admin.Feedback;

public sealed record AdminFeedbackItem(
    Guid Id,
    string Content,
    AppFeedbackStatus Status,
    DateTimeOffset CreatedAt,
    string? AdminReply,
    DateTimeOffset? RepliedAt,
    Guid UserId,
    string? Username,
    string? FirstName,
    string? Email,
    string? ProfileImageUrl);

public sealed record AdminFeedbackCounts(int New, int Read, int Resolved);

public sealed record AdminFeedbackPage(PagedResult<AdminFeedbackItem> Page, AdminFeedbackCounts Counts);

public sealed record ListAdminFeedbackQuery(AppFeedbackStatus? Status, int? Page, int? PageSize)
    : IQuery<AdminFeedbackPage>;

internal sealed class ListAdminFeedbackQueryHandler : IQueryHandler<ListAdminFeedbackQuery, AdminFeedbackPage>
{
    private readonly IApplicationDbContext _dbContext;

    public ListAdminFeedbackQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<AdminFeedbackPage>> Handle(ListAdminFeedbackQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPaging.Normalize(request.Page, request.PageSize);
        var query = _dbContext.AppFeedbacks.AsNoTracking();
        if (request.Status is { } status)
        {
            query = query.Where(item => item.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(
                _dbContext.Users.AsNoTracking(),
                item => item.UserId,
                user => user.Id,
                (item, user) => new AdminFeedbackItem(
                    item.Id,
                    item.Content,
                    item.Status,
                    item.CreatedAt,
                    item.AdminReply,
                    item.RepliedAt,
                    user.Id,
                    user.UserProfile != null ? user.UserProfile.Username : null,
                    user.UserProfile != null ? user.UserProfile.FirstName : null,
                    user.Email,
                    user.UserProfile != null ? user.UserProfile.ProfileImageUrl : null))
            .ToListAsync(cancellationToken);

        var grouped = await _dbContext.AppFeedbacks.AsNoTracking()
            .GroupBy(item => item.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        int CountOf(AppFeedbackStatus status) => grouped.FirstOrDefault(row => row.Key == status)?.Count ?? 0;

        return Result<AdminFeedbackPage>.Success(new AdminFeedbackPage(
            PagedResult<AdminFeedbackItem>.Create(items, page, pageSize, total),
            new AdminFeedbackCounts(
                CountOf(AppFeedbackStatus.New),
                CountOf(AppFeedbackStatus.Read),
                CountOf(AppFeedbackStatus.Resolved))));
    }
}

public sealed record GetAdminFeedbackQuery(Guid FeedbackId) : IQuery<AdminFeedbackItem>;

/// <summary>İşlem kaydından tek bir geri bildirime gitmek için.</summary>
internal sealed class GetAdminFeedbackQueryHandler : IQueryHandler<GetAdminFeedbackQuery, AdminFeedbackItem>
{
    private readonly IApplicationDbContext _dbContext;

    public GetAdminFeedbackQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<AdminFeedbackItem>> Handle(GetAdminFeedbackQuery request, CancellationToken cancellationToken)
    {
        var item = await _dbContext.AppFeedbacks.AsNoTracking()
            .Where(feedback => feedback.Id == request.FeedbackId)
            .Join(
                _dbContext.Users.AsNoTracking(),
                feedback => feedback.UserId,
                user => user.Id,
                (feedback, user) => new AdminFeedbackItem(
                    feedback.Id,
                    feedback.Content,
                    feedback.Status,
                    feedback.CreatedAt,
                    feedback.AdminReply,
                    feedback.RepliedAt,
                    user.Id,
                    user.UserProfile != null ? user.UserProfile.Username : null,
                    user.UserProfile != null ? user.UserProfile.FirstName : null,
                    user.Email,
                    user.UserProfile != null ? user.UserProfile.ProfileImageUrl : null))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null
            ? Result<AdminFeedbackItem>.Failure(AdminErrors.FeedbackNotFound)
            : Result<AdminFeedbackItem>.Success(item);
    }
}

public sealed record SetFeedbackStatusCommand(Guid FeedbackId, AppFeedbackStatus Status) : ICommand;

internal sealed class SetFeedbackStatusCommandHandler : ICommandHandler<SetFeedbackStatusCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SetFeedbackStatusCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(SetFeedbackStatusCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Result.Failure(AdminErrors.InvalidUserState);
        }

        var feedback = await _dbContext.AppFeedbacks
            .FirstOrDefaultAsync(item => item.Id == request.FeedbackId, cancellationToken);
        if (feedback is null)
        {
            return Result.Failure(AdminErrors.FeedbackNotFound);
        }

        if (feedback.Status == request.Status)
        {
            return Result.Success();
        }

        var utcNow = _timeProvider.GetUtcNow();
        feedback.SetStatus(request.Status, utcNow);
        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.FeedbackStatus, AdminActions.FeedbackTarget, feedback.Id,
            request.Status.ToString(), utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Önce mail gönderilir, başarılıysa yanıt kaydedilir: kayıtlı ama gitmemiş yanıt olmasın.</summary>
public sealed record ReplyToFeedbackCommand(Guid FeedbackId, string Message) : ICommand;

internal sealed class ReplyToFeedbackCommandHandler : ICommandHandler<ReplyToFeedbackCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReplyToFeedbackCommandHandler> _logger;

    public ReplyToFeedbackCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILogger<ReplyToFeedbackCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(ReplyToFeedbackCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var message = request.Message?.Trim() ?? string.Empty;
        if (message.Length == 0 || message.Length > AppFeedback.MaxReplyLength)
        {
            return Result.Failure(AdminErrors.InvalidReply);
        }

        var feedback = await _dbContext.AppFeedbacks
            .FirstOrDefaultAsync(item => item.Id == request.FeedbackId, cancellationToken);
        if (feedback is null)
        {
            return Result.Failure(AdminErrors.FeedbackNotFound);
        }

        var email = await _dbContext.Users.AsNoTracking()
            .Where(user => user.Id == feedback.UserId)
            .Select(user => user.Email)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure(AdminErrors.NoEmail);
        }

        var sent = await _emailSender.SendFeedbackReplyAsync(email, feedback.Content, message, cancellationToken);
        if (!sent.Succeeded)
        {
            _logger.LogWarning("Feedback reply email failed: {Error}", sent.ErrorMessage);
            return Result.Failure(AdminErrors.EmailFailed);
        }

        var utcNow = _timeProvider.GetUtcNow();
        feedback.RecordReply(adminId, message, utcNow);
        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.FeedbackReply, AdminActions.FeedbackTarget, feedback.Id, null, utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
