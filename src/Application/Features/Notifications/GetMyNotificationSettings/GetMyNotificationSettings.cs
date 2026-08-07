using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;

namespace Sportner.Application.Features.Notifications.GetMyNotificationSettings;

public sealed record GetMyNotificationSettingsQuery
    : IQuery<IReadOnlyList<NotificationSettingResponse>>;

internal sealed class GetMyNotificationSettingsQueryHandler
    : IQueryHandler<GetMyNotificationSettingsQuery, IReadOnlyList<NotificationSettingResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetMyNotificationSettingsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<NotificationSettingResponse>>> Handle(
        GetMyNotificationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<NotificationSettingResponse>>.Failure(
                NotificationErrors.NotAuthenticated);
        }

        var settings = await _dbContext.NotificationSettings
            .Where(setting => setting.UserId == userId)
            .ToListAsync(cancellationToken);

        // Backfill any missing types (e.g. enum grew after signup).
        var utcNow = _timeProvider.GetUtcNow();
        var existingTypes = settings.Select(setting => setting.NotificationType).ToHashSet();
        var added = false;

        foreach (var type in Enum.GetValues<NotificationType>())
        {
            if (existingTypes.Contains(type))
            {
                continue;
            }

            var created = NotificationSetting.CreateDefault(userId, type, utcNow);
            _dbContext.NotificationSettings.Add(created);
            settings.Add(created);
            added = true;
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var response = settings
            .OrderBy(setting => setting.NotificationType)
            .Select(setting => new NotificationSettingResponse(
                (short)setting.NotificationType,
                setting.InAppEnabled,
                setting.PushEnabled,
                setting.EmailEnabled))
            .ToList();

        return Result<IReadOnlyList<NotificationSettingResponse>>.Success(response);
    }
}
