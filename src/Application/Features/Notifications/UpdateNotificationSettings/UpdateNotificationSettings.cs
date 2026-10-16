using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;

namespace Sportner.Application.Features.Notifications.UpdateNotificationSettings;

public sealed record NotificationSettingUpdate(
    short NotificationType,
    bool InAppEnabled,
    bool PushEnabled,
    bool EmailEnabled);

/// <summary>
/// Birden çok bildirim türünü tek işlemde günceller.
///
/// Ayarlar ekranı türleri gruplayarak sunuyor (etkinlikler, sosyal, mesajlar …),
/// yani tek bir anahtar onlarca türü birden değiştiriyor. Tür başına istek
/// atılsaydı yarısı başarılı olduğunda kullanıcı grubu "kapalı" görürken bir
/// kısmı açık kalırdı; burada ya hepsi yazılır ya hiçbiri.
/// </summary>
public sealed record UpdateNotificationSettingsCommand(
    IReadOnlyList<NotificationSettingUpdate> Updates)
    : ICommand<IReadOnlyList<NotificationSettingResponse>>;

public sealed class UpdateNotificationSettingsCommandValidator
    : AbstractValidator<UpdateNotificationSettingsCommand>
{
    public UpdateNotificationSettingsCommandValidator()
    {
        RuleFor(command => command.Updates)
            .NotEmpty()
            .WithMessage("At least one notification setting is required.");

        // Üst sınır tür sayısından türetiliyor: tek istekte her türe en fazla
        // bir kez dokunulabilir.
        RuleFor(command => command.Updates)
            .Must(updates => updates.Count <= Enum.GetValues<NotificationType>().Length)
            .WithMessage("Too many notification settings in one request.");

        RuleFor(command => command.Updates)
            .Must(updates => updates
                .Select(update => update.NotificationType)
                .Distinct()
                .Count() == updates.Count)
            .WithMessage("The same notification type appears more than once.");

        RuleForEach(command => command.Updates)
            .Must(update => Enum.IsDefined((NotificationType)update.NotificationType))
            .WithMessage("The notification type is invalid.");
    }
}

internal sealed class UpdateNotificationSettingsCommandHandler
    : ICommandHandler<UpdateNotificationSettingsCommand, IReadOnlyList<NotificationSettingResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateNotificationSettingsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<NotificationSettingResponse>>> Handle(
        UpdateNotificationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<NotificationSettingResponse>>.Failure(
                NotificationErrors.NotAuthenticated);
        }

        if (request.Updates.Any(update => !Enum.IsDefined((NotificationType)update.NotificationType)))
        {
            return Result<IReadOnlyList<NotificationSettingResponse>>.Failure(
                NotificationErrors.InvalidType);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var requestedTypes = request.Updates
            .Select(update => (NotificationType)update.NotificationType)
            .ToList();

        var existing = await _dbContext.NotificationSettings
            .Where(setting =>
                setting.UserId == userId
                && requestedTypes.Contains(setting.NotificationType))
            .ToListAsync(cancellationToken);

        var byType = existing.ToDictionary(setting => setting.NotificationType);
        var responses = new List<NotificationSettingResponse>(request.Updates.Count);

        foreach (var update in request.Updates)
        {
            var type = (NotificationType)update.NotificationType;

            // Satırlar kayıt sırasında yazılıyor ama eksik olabiliyor: yayıncı
            // satır yoksa tür varsayılanına düşüyor, yani kullanıcı ayarı hiç
            // değiştirmediyse satır hiç oluşmamış olabilir.
            if (!byType.TryGetValue(type, out var setting))
            {
                setting = NotificationSetting.CreateDefault(userId, type, utcNow);
                _dbContext.NotificationSettings.Add(setting);
            }

            setting.UpdateChannels(
                update.InAppEnabled,
                update.PushEnabled,
                update.EmailEnabled,
                utcNow);

            responses.Add(new NotificationSettingResponse(
                (short)setting.NotificationType,
                setting.InAppEnabled,
                setting.PushEnabled,
                setting.EmailEnabled));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<NotificationSettingResponse>>.Success(responses);
    }
}
