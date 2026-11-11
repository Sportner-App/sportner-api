using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Admin.Users;

public sealed record AdminUserListItem(
    Guid Id,
    string? Username,
    string? FirstName,
    string? LastName,
    string? Email,
    string? ProfileImageUrl,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt);

public sealed record AdminUserDetail(
    AdminUserListItem User,
    string? City,
    string? Bio,
    bool IsAdmin,
    bool HasPassword,
    IReadOnlyList<string> LoginProviders,
    int PostsCount,
    int EventsJoined,
    int EventsOrganized,
    int FriendsCount,
    int ActiveSessions,
    int ReportsAgainst,
    int FeedbackCount);

public sealed record ListAdminUsersQuery(string? Search, UserStatus? Status, int? Page, int? PageSize)
    : IQuery<PagedResult<AdminUserListItem>>;

internal sealed class ListAdminUsersQueryHandler
    : IQueryHandler<ListAdminUsersQuery, PagedResult<AdminUserListItem>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListAdminUsersQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PagedResult<AdminUserListItem>>> Handle(
        ListAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPaging.Normalize(request.Page, request.PageSize);
        var query = _dbContext.Users.AsNoTracking();

        if (request.Status is { } status)
        {
            query = query.Where(user => user.Status == status);
        }

        var search = request.Search?.Trim().TrimStart('@').ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            query = Guid.TryParse(search, out var id)
                ? query.Where(user => user.Id == id)
                : query.Where(user =>
                    (user.Email ?? string.Empty).ToLower().Contains(search)
                    || (user.UserProfile != null && (
                        user.UserProfile.Username.ToLower().Contains(search)
                        || user.UserProfile.FirstName.ToLower().Contains(search)
                        || (user.UserProfile.LastName ?? string.Empty).ToLower().Contains(search))));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(user => user.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new AdminUserListItem(
                user.Id,
                user.UserProfile != null ? user.UserProfile.Username : null,
                user.UserProfile != null ? user.UserProfile.FirstName : null,
                user.UserProfile != null ? user.UserProfile.LastName : null,
                user.Email,
                user.UserProfile != null ? user.UserProfile.ProfileImageUrl : null,
                user.Status,
                user.CreatedAt,
                user.LastSeenAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AdminUserListItem>>.Success(
            PagedResult<AdminUserListItem>.Create(items, page, pageSize, total));
    }
}

public sealed record GetAdminUserQuery(Guid UserId) : IQuery<AdminUserDetail>;

internal sealed class GetAdminUserQueryHandler : IQueryHandler<GetAdminUserQuery, AdminUserDetail>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAdminDirectory _admins;
    private readonly TimeProvider _timeProvider;

    public GetAdminUserQueryHandler(IApplicationDbContext dbContext, IAdminDirectory admins, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _admins = admins;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AdminUserDetail>> Handle(GetAdminUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .Include(candidate => candidate.UserProfile)
            .Include(candidate => candidate.Statistics)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<AdminUserDetail>.Failure(AdminErrors.UserNotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var providers = await _dbContext.UserExternalLogins.AsNoTracking()
            .Where(login => login.UserId == user.Id)
            .Select(login => login.Provider.ToString())
            .ToListAsync(cancellationToken);
        var activeSessions = await _dbContext.UserSessions.AsNoTracking()
            .CountAsync(session => session.UserId == user.Id
                && session.RevokedAt == null
                && session.ExpiresAt > utcNow, cancellationToken);
        var reports = await _dbContext.Reports.AsNoTracking()
            .CountAsync(report => report.EntityType == ReportEntityType.User
                && report.EntityId == user.Id, cancellationToken);
        var feedback = await _dbContext.AppFeedbacks.AsNoTracking()
            .CountAsync(item => item.UserId == user.Id, cancellationToken);

        var profile = user.UserProfile;
        var stats = user.Statistics;

        return Result<AdminUserDetail>.Success(new AdminUserDetail(
            new AdminUserListItem(
                user.Id,
                profile?.Username,
                profile?.FirstName,
                profile?.LastName,
                user.Email,
                profile?.ProfileImageUrl,
                user.Status,
                user.CreatedAt,
                user.LastSeenAt),
            profile?.City,
            profile?.Bio,
            _admins.IsAdmin(user.Id),
            !string.IsNullOrEmpty(user.PasswordHash),
            providers,
            stats?.PostsCount ?? 0,
            stats?.EventsJoined ?? 0,
            stats?.EventsOrganized ?? 0,
            stats?.FriendsCount ?? 0,
            activeSessions,
            reports,
            feedback));
    }
}

/// <summary>
/// Banlar, açık oturumları kapatır ve istenirse profil fotoğrafını depodan siler.
/// Ban her istekte veritabanından kontrol edildiği için kullanıcı hemen dışarıda kalır.
/// </summary>
public sealed record BanUserCommand(Guid UserId, string? Reason, bool RemoveAvatar) : ICommand;

internal sealed class BanUserCommandHandler : ICommandHandler<BanUserCommand>
{
    private const string PublicAvatarMarker = "/object/public/" + StorageBuckets.Avatars + "/";

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAdminDirectory _admins;
    private readonly IFileStorage _fileStorage;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BanUserCommandHandler> _logger;

    public BanUserCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAdminDirectory admins,
        IFileStorage fileStorage,
        TimeProvider timeProvider,
        ILogger<BanUserCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _admins = admins;
        _fileStorage = fileStorage;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        if (_admins.IsAdmin(request.UserId))
        {
            return Result.Failure(AdminErrors.CannotModerateAdmin);
        }

        var user = await _dbContext.Users
            .Include(candidate => candidate.UserProfile)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AdminErrors.UserNotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        try
        {
            user.Ban(utcNow);
        }
        catch (DomainException)
        {
            return Result.Failure(AdminErrors.InvalidUserState);
        }

        var sessions = await _dbContext.UserSessions
            .Where(session => session.UserId == user.Id && session.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(utcNow);
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.BanUser, AdminActions.UserTarget, user.Id, reason, utcNow));

        string? avatarPath = null;
        if (request.RemoveAvatar && user.UserProfile?.ProfileImageUrl is { } avatarUrl)
        {
            var index = avatarUrl.IndexOf(PublicAvatarMarker, StringComparison.Ordinal);
            avatarPath = index >= 0 ? avatarUrl[(index + PublicAvatarMarker.Length)..] : null;
            user.UserProfile.UpdateAvatar(null, utcNow);
            _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
                adminId, AdminActions.RemoveAvatar, AdminActions.UserTarget, user.Id, null, utcNow));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Dosya silme veritabanı kaydından sonra: depo hatası banı geri almamalı.
        // Google gibi dış adreslerde silinecek dosya yok, sadece bağlantı kaldırılır.
        if (avatarPath is not null)
        {
            try
            {
                await _fileStorage.DeleteAsync(StorageBuckets.Avatars, avatarPath, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Avatar file of banned user {UserId} could not be deleted.", user.Id);
            }
        }

        return Result.Success();
    }
}

public sealed record UnbanUserCommand(Guid UserId) : ICommand;

internal sealed class UnbanUserCommandHandler : ICommandHandler<UnbanUserCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UnbanUserCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UnbanUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AdminErrors.UserNotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        try
        {
            user.Unban(utcNow);
        }
        catch (DomainException)
        {
            return Result.Failure(AdminErrors.InvalidUserState);
        }

        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.UnbanUser, AdminActions.UserTarget, user.Id, null, utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
