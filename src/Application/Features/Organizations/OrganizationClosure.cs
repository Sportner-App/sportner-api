using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Events;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Organizations;

namespace Sportner.Application.Features.Organizations;

/// <summary>
/// Kulübü kapatır (soft delete) ve başlamamış etkinliklerini iptal eder. Kurucunun kulüp silmesi
/// ile yönetici kapatması aynı akışı kullanır. Kaydetmek çağıranın işi.
/// </summary>
internal static class OrganizationClosure
{
    /// <returns>İptal edilen etkinlik sayısı.</returns>
    internal static async Task<int> CloseAsync(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        Organization organization,
        Guid actorUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken,
        bool cancelledByAdmin = false)
    {
        organization.MarkDeleted(utcNow);

        var organizationId = organization.Id;
        var upcomingEvents = await dbContext.Events
            .Include(candidate => candidate.Participants)
            .Include(candidate => candidate.Waitlist)
            .Where(candidate =>
                candidate.OrganizationId == organizationId
                // Başlamış etkinliği iptal etme: tamamlama worker'ı henüz Completed yapmamış olabilir
                // (EventCompletionDispatcher da EventDate <= now ile başlar).
                && candidate.EventDate > utcNow
                && (candidate.Status == EventStatus.Draft
                    || candidate.Status == EventStatus.Published
                    || candidate.Status == EventStatus.Full))
            .ToListAsync(cancellationToken);

        foreach (var @event in upcomingEvents)
        {
            await EventCancellation.CancelAsync(
                dbContext, notificationPublisher, @event, actorUserId, utcNow, cancellationToken,
                cancelledByAdmin);
        }

        return upcomingEvents.Count;
    }
}
