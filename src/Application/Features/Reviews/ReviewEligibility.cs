using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Reviews;

internal static class ReviewEligibility
{
    public static bool CanReviewEvent(Event @event, Guid reviewerUserId, EventParticipant? reviewer)
    {
        if (@event.Status is not EventStatus.Completed)
        {
            return false;
        }

        // Oynamadan yöneten kulüp etkinliği oluşturanı değerlendirmeye girmez.
        if (reviewerUserId == @event.OrganizerUserId)
        {
            return @event.IsOrganizerParticipant;
        }

        return reviewer is { Status: ParticipantStatus.Attended, CanReview: true };
    }

    public static bool CanBeReviewed(Event @event, Guid reviewedUserId, EventParticipant? reviewed)
    {
        if (reviewedUserId == @event.OrganizerUserId)
        {
            return @event.IsOrganizerParticipant;
        }

        return reviewed is { Status: ParticipantStatus.Attended };
    }
}
