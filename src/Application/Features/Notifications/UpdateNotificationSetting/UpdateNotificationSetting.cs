using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;

namespace Sportner.Application.Features.Notifications.UpdateNotificationSetting;

public sealed record UpdateNotificationSettingCommand(
    short NotificationType,
    bool InAppEnabled,
    bool PushEnabled,
    bool EmailEnabled) : ICommand<NotificationSettingResponse>;

public sealed class UpdateNotificationSettingCommandValidator
    : AbstractValidator<UpdateNotificationSettingCommand>
{
    public UpdateNotificationSettingCommandValidator()
    {
        RuleFor(command => command.NotificationType)
            .Must(type => Enum.IsDefined((NotificationType)type))
            .WithMessage("The notification type is invalid.");
    }
}

internal sealed class UpdateNotificationSettingCommandHandler
    : ICommandHandler<UpdateNotificationSettingCommand, NotificationSettingResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateNotificationSettingCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<NotificationSettingResponse>> Handle(
        UpdateNotificationSettingCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<NotificationSettingResponse>.Failure(NotificationErrors.NotAuthenticated);
        }

        if (!Enum.IsDefined((NotificationType)request.NotificationType))
        {
            return Result<NotificationSettingResponse>.Failure(NotificationErrors.InvalidType);
        }

        var type = (NotificationType)request.NotificationType;
        var utcNow = _timeProvider.GetUtcNow();

        var setting = await _dbContext.NotificationSettings
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == userId && candidate.NotificationType == type,
                cancellationToken);

        if (setting is null)
        {
            setting = NotificationSetting.CreateDefault(userId, type, utcNow);
            _dbContext.NotificationSettings.Add(setting);
        }

        setting.UpdateChannels(
            request.InAppEnabled,
            request.PushEnabled,
            request.EmailEnabled,
            utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<NotificationSettingResponse>.Success(
            new NotificationSettingResponse(
                (short)setting.NotificationType,
                setting.InAppEnabled,
                setting.PushEnabled,
                setting.EmailEnabled));
    }
}
