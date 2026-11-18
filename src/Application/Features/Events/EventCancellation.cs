using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events;

/// <summary>
/// Etkinliği iptal eder; konuşmayı kapatır, katılımcılara bildirim gönderir ve istatistikleri geri alır.
/// <see cref="Domain.Events.Event.Participants"/> yüklü olmalı. Kaydetmek çağıranın işi.
/// </summary>
internal static class EventCancellation
{
    internal static async Task CancelAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        Domain.Events.Event @event,
        Guid actorUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var wasAlreadyCancelled = @event.Status == EventStatus.Cancelled;

        @event.Cancel(utcNow);

        if (wasAlreadyCancelled)
        {
            return;
        }

        await EventAccess.CloseEventConversationAsync(dbContext, @event.Id, utcNow, cancellationToken);

        // Eylemi yapan hariç herkese; yapan oluşturan değilse (kulüp yöneticisi / kulüp silen kurucu)
        // oluşturan da katılımcı olmasa bile bilgilendirilir.
        var recipients = EventRosterNotifier.ResolveRecipients(@event, actorUserId);

        var organizerActor = await NotificationActor.ResolveEventActorNameAsync(
            dbContext, @event, actorUserId, cancellationToken);
        var languagesByRecipient = await NotificationActor.ResolveRecipientLanguagesAsync(
            dbContext, recipients, cancellationToken);

        foreach (var recipientId in recipients)
        {
            var language = languagesByRecipient.GetValueOrDefault(recipientId);

            await notificationPublisher.PublishAsync(
                recipientId,
                NotificationType.EventCancelled,
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.EventCancelled_Title),
                    NotificationActor.FormatPrefix(organizerActor, language)),
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.EventCancelled_Body),
                    @event.Title),
                NotificationEntityType.Event,
                @event.Id,
                actorUserId,
                cancellationToken);
        }

        // İptal sayacı yalnızca oluşturan kendisi iptal ettiğinde ona yazılır; kulüp yöneticisi
        // iptal ettiğinde ya da kulüp silindiğinde oluşturanın istatistiği değişmez.
        if (actorUserId == @event.OrganizerUserId)
        {
            var organizerStatistics = await dbContext.UserStatistics
                .FirstOrDefaultAsync(
                    candidate => candidate.UserId == @event.OrganizerUserId,
                    cancellationToken);

            organizerStatistics?.IncreaseCancelledEvents(utcNow);
        }

        // Approved attendees had EventsJoined bumped on approve/promote — reverse it.
        var approvedAttendeeIds = @event.Participants
            .Where(participant =>
                participant.UserId is { } userId
                && userId != @event.OrganizerUserId
                && participant.Status is ParticipantStatus.Approved)
            .Select(participant => participant.UserId!.Value)
            .Distinct()
            .ToList();

        foreach (var attendeeId in approvedAttendeeIds)
        {
            var attendeeStatistics = await dbContext.UserStatistics
                .FirstOrDefaultAsync(candidate => candidate.UserId == attendeeId, cancellationToken);

            if (attendeeStatistics is not null && attendeeStatistics.EventsJoined > 0)
            {
                attendeeStatistics.DecreaseEventsJoined(utcNow);
            }
        }
    }
}
