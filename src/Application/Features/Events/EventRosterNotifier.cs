using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events;

/// <summary>
/// Fans out a notification to every pending/approved participant of an event (excluding the
/// organizer), each rendered in their own preferred language. Used when the organizer changes
/// something attendees have already committed to - schedule, location, fee, capacity.
/// </summary>
internal static class EventRosterNotifier
{
    /// <param name="previousTitle">
    /// Guncelleme uygulanmadan onceki baslik. Bildirim metni bunu kullaniyor:
    /// katilimci etkinligi eski adiyla taniyor, yeni adi yazarsak hangi
    /// etkinlikten bahsedildigini anlamiyor.
    /// </param>
    /// <summary>
    /// Bekleyen/onaylı katılımcılar; eylemi yapan hariç. Eylemi yapan oluşturan değilse (kulüp yöneticisi)
    /// oluşturan da, katılımcı olmasa bile bilgilendirilir.
    /// </summary>
    internal static List<Guid> ResolveRecipients(Domain.Events.Event @event, Guid actorUserId)
    {
        var recipients = @event.Participants
            .Where(participant => participant.UserId is { } userId
                && participant.Status is ParticipantStatus.Pending
                    or ParticipantStatus.Approved)
            .Select(participant => participant.UserId!.Value)
            .ToHashSet();

        if (actorUserId != @event.OrganizerUserId)
        {
            recipients.Add(@event.OrganizerUserId);
        }

        recipients.Remove(actorUserId);
        return recipients.ToList();
    }

    internal static async Task NotifyParticipantsOfUpdateAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        Domain.Events.Event @event,
        Guid actorUserId,
        IReadOnlyCollection<EventUpdateField> changes,
        string previousTitle,
        CancellationToken cancellationToken)
    {
        // Bekleyenler de dahil: tekil guncelleme uclari (UpdateEventSchedule
        // vb.) zaten onlara da haber veriyordu, toplu uc vermeyince ayni
        // degisiklik hangi uctan yapildigina gore farkli kisilere ulasiyordu.
        var recipients = ResolveRecipients(@event, actorUserId);

        if (recipients.Count == 0 || changes.Count == 0) return;

        var organizerActor = await NotificationActor.ResolveEventActorNameAsync(
            dbContext, @event, actorUserId, cancellationToken);
        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            dbContext, recipients, cancellationToken);

        foreach (var recipientId in recipients)
        {
            var language = languages.GetValueOrDefault(recipientId);
            var changedLabels = changes
                .Select(change => NotificationActor.Format(language, $"EventUpdated_Field_{change}"))
                .ToArray();

            await notificationPublisher.PublishAsync(
                recipientId,
                NotificationType.EventUpdated,
                NotificationActor.Format(language, "EventUpdated_Title", NotificationActor.FormatPrefix(organizerActor, language)),
                NotificationActor.Format(language, "EventUpdated_Body", previousTitle, string.Join(", ", changedLabels)),
                NotificationEntityType.Event,
                @event.Id,
                actorUserId,
                cancellationToken);
        }
    }

    internal static async Task NotifyParticipantsAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        Domain.Events.Event @event,
        Guid actorUserId,
        NotificationType notificationType,
        string titleResourceKey,
        string bodyResourceKey,
        CancellationToken cancellationToken)
    {
        var recipients = ResolveRecipients(@event, actorUserId);

        if (recipients.Count == 0)
        {
            return;
        }

        var organizerActor = await NotificationActor.ResolveEventActorNameAsync(
            dbContext, @event, actorUserId, cancellationToken);
        var languagesByRecipient = await NotificationActor.ResolveRecipientLanguagesAsync(
            dbContext, recipients, cancellationToken);

        foreach (var recipientId in recipients)
        {
            var language = languagesByRecipient.GetValueOrDefault(recipientId);

            await notificationPublisher.PublishAsync(
                recipientId,
                notificationType,
                NotificationActor.Format(
                    language,
                    titleResourceKey,
                    NotificationActor.FormatPrefix(organizerActor, language)),
                NotificationActor.Format(
                    language,
                    bodyResourceKey,
                    @event.Title),
                NotificationEntityType.Event,
                @event.Id,
                actorUserId,
                cancellationToken);
        }
    }
}

internal enum EventUpdateField
{
    // Baslik ve aciklama ayri: tek bir "etkinlik bilgileri" etiketi
    // katilimciya neyin degistigini soylemiyordu.
    Title,
    Description,
    Schedule,
    Location,
    Capacity,
    Fee
}
