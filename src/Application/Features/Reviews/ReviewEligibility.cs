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

        if (reviewerUserId == @event.OrganizerUserId)
        {
            return true;
        }

        return reviewer is { Status: ParticipantStatus.Attended, CanReview: true };
    }

    public static bool CanBeReviewed(Event @event, Guid reviewedUserId, EventParticipant? reviewed)
    {
        if (reviewedUserId == @event.OrganizerUserId)
        {
            return true;
        }

        return reviewed is { Status: ParticipantStatus.Attended };
    }
}
