using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Localization;
using Sportner.Application.Features.Notifications;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events;

internal static class EventCompletion
{
    internal static async Task ApplySideEffectsAsync(
        IApplicationDbContext dbContext,
        Event @event,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
        INotificationPublisher notificationPublisher,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        await EventAccess.CloseEventConversationAsync(dbContext, @event.Id, utcNow, cancellationToken);
        await badgeAwarder.EvaluateAfterEventCompletedAsync(@event.OrganizerUserId, cancellationToken);
        await questProgressTracker.ReportAsync(
            @event.OrganizerUserId,
            QuestMetrics.EventsOrganizedCompleted,
            1,
            cancellationToken);

        // Burada değerlendirme bildirimi gönderilmiyor. Daha önce organizatöre
        // gönderiliyordu, "bu anda yalnızca organizatör değerlendirmeye uygun"
        // varsayımıyla — ama CompleteIfDue yalnızca durumu Completed yapıyor,
        // kimsenin yoklamasını almıyor. Dolayısıyla organizatör dahil hiç kimsenin
        // CanReview değeri true olmuyor ve bildirime dokunan organizatör
        // "şu an puanlanacak kimse yok, organizatör yoklamayı tamamlayınca açılır"
        // diyen boş bir ekrana düşüyordu: kendisini beklemesi söyleniyordu.
        //
        // Gerçek bildirim zaten doğru anda gidiyor: yoklama onaylanınca
        // AttendanceConfirmation.ConfirmAsync herkese kendi bildirimini gönderiyor.
        // Organizatör yoklamayı hiç almazsa AttendanceAutoConfirmDispatcher bunu
        // AttendanceAutoConfirmGraceDays sonra devralıyor.
        //
        // Organizatöre bunun yerine yoklama bildirimi gidiyor: değerlendirmelerin
        // açılması onun bir adımına bağlı, dolayısıyla istenen eylem bu.
        await SendAttendancePromptAsync(
            dbContext, @event, notificationPublisher, cancellationToken);
    }

    /// <summary>
    /// Etkinlik bittiğinde organizatörden yoklamayı almasını ister. Yoklama
    /// alınmadan kimse değerlendirme yapamıyor, yani bu adım atılmazsa etkinlik
    /// otomatik onay devreye girene kadar kilitli kalıyor.
    ///
    /// Tamamlama birden fazla yoldan tetiklenebiliyor (zamanlanmış iş ve
    /// organizatörün elle kapatması), o yüzden aynı (etkinlik, alıcı) çifti için
    /// daha önce gönderilmişse tekrarlanmıyor.
    /// </summary>
    internal static async Task SendAttendancePromptAsync(
        IApplicationDbContext dbContext,
        Event @event,
        INotificationPublisher notificationPublisher,
        CancellationToken cancellationToken)
    {
        var alreadySent = await dbContext.Notifications.AsNoTracking().AnyAsync(
            notification =>
                notification.RecipientUserId == @event.OrganizerUserId
                && notification.NotificationType == NotificationType.EventAttendancePending
                && notification.EntityType == NotificationEntityType.Event
                && notification.EntityId == @event.Id,
            cancellationToken);

        if (alreadySent)
        {
            return;
        }

        var language = await NotificationActor.ResolveRecipientLanguageAsync(
            dbContext, @event.OrganizerUserId, cancellationToken);

        await notificationPublisher.PublishAsync(
            @event.OrganizerUserId,
            NotificationType.EventAttendancePending,
            NotificationActor.Format(
                language, nameof(NotificationsResource.EventAttendancePending_Title)),
            NotificationActor.Format(
                language, nameof(NotificationsResource.EventAttendancePending_Body), @event.Title),
            NotificationEntityType.Event,
            @event.Id,
            actorUserId: null,
            cancellationToken);
    }

    /// <summary>
    /// Prompts <paramref name="recipientId"/> to go rate their teammates for this event.
    /// Multiple call sites can reach the same (event, recipient) pair - e.g. the organizer is
    /// auto-enrolled as a participant, so both event-completion (organizer) and their own
    /// attendance confirmation (via ConfirmAllAttendance's sweep) would otherwise fire this
    /// twice - so this is a no-op if that prompt was already sent.
    /// </summary>
    internal static async Task SendReviewPromptAsync(
        IApplicationDbContext dbContext,
        Event @event,
        Guid recipientId,
        INotificationPublisher notificationPublisher,
        CancellationToken cancellationToken)
    {
        var alreadySent = await dbContext.Notifications.AsNoTracking().AnyAsync(
            notification =>
                notification.RecipientUserId == recipientId
                && notification.NotificationType == NotificationType.EventReviewPrompt
                && notification.EntityType == NotificationEntityType.Event
                && notification.EntityId == @event.Id,
            cancellationToken);

        if (alreadySent)
        {
            return;
        }

        var language = await NotificationActor.ResolveRecipientLanguageAsync(
            dbContext, recipientId, cancellationToken);

        await notificationPublisher.PublishAsync(
            recipientId,
            NotificationType.EventReviewPrompt,
            NotificationActor.Format(language, nameof(NotificationsResource.EventReviewPrompt_Title)),
            NotificationActor.Format(
                language, nameof(NotificationsResource.EventReviewPrompt_Body), @event.Title),
            NotificationEntityType.Event,
            @event.Id,
            actorUserId: null,
            cancellationToken);
    }
}
