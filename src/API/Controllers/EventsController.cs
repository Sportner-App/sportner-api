using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sportner.API.Authorization;
using Sportner.API.Extensions.RateLimiting;
using Sportner.API.Common;
using Sportner.Application.Features.Events.ApplyToEvent;
using Sportner.Application.Features.Events.AcceptEventInvitation;
using Sportner.Application.Features.Events.DeclineEventInvitation;
using Sportner.Application.Features.Events.ApproveParticipant;
using Sportner.Application.Features.Events.AssignEventParticipants;
using Sportner.Application.Features.Events.CancelEvent;
using Sportner.Application.Features.Events.CancelParticipation;
using Sportner.Application.Features.Events.CompleteEvent;
using Sportner.Application.Features.Events.ConfirmAttendance;
using Sportner.Application.Features.Events.CreateEvent;
using Sportner.Application.Features.Events.CreateRecurringEvents;
using Sportner.Application.Features.Events.DiscoverEvents;
using Sportner.Application.Features.Events.GetEventById;
using Sportner.Application.Features.Albums.CreateEventAlbum;
using Sportner.Application.Features.Albums.ListEventAlbums;
using Sportner.Application.Features.Events.ListMyOrganizedEvents;
using Sportner.Application.Features.Events.ListMyParticipatingEvents;
using Sportner.Application.Features.Events.ListParticipants;
using Sportner.Application.Features.Events.ListWaitlist;
using Sportner.Application.Features.Events.MarkNoShow;
using Sportner.Application.Features.Events.PromoteFromWaitlist;
using Sportner.Application.Features.Events.PublishEvent;
using Sportner.Application.Features.Events.RejectParticipant;
using Sportner.Application.Features.Events.RemoveAssignedParticipant;
using Sportner.Application.Features.Events.UpdateEventCapacity;
using Sportner.Application.Features.Events.UpdateEventDetails;
using Sportner.Application.Features.Events.UpdateEventLocation;
using Sportner.Application.Features.Events.UpdateEventSchedule;
using Sportner.Application.Features.Events.EventQuestions.AskEventQuestion;
using Sportner.Application.Features.Events.EventQuestions.ListEventQuestions;
using Sportner.Application.Features.Events.EventQuestions.ReplyToEventQuestion;
using Sportner.Application.Features.Messaging.GetConversationByEvent;
using Sportner.Application.Features.Reviews.ListReviewablePeers;
using Sportner.Application.Features.Reviews.ListReviewsForEvent;

namespace Sportner.API.Controllers;

[Authorize]
public sealed class EventsController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Discover(
        [FromQuery] Guid? sportId,
        [FromQuery] string? city,
        [FromQuery] decimal? lat,
        [FromQuery] decimal? lng,
        [FromQuery] double? radiusKm,
        [FromQuery] int? minAge,
        [FromQuery] int? maxAge,
        [FromQuery] short? gender,
        [FromQuery] short? skillLevel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new DiscoverEventsQuery(
                sportId,
                city,
                lat,
                lng,
                radiusKm,
                minAge,
                maxAge,
                gender,
                skillLevel,
                page,
                pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("mine/organized")]
    public async Task<IActionResult> ListMyOrganized(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListMyOrganizedEventsQuery(page, pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("mine/participating")]
    public async Task<IActionResult> ListMyParticipating(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? scope = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListMyParticipatingEventsQuery(page, pageSize, scope),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{eventId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetEventByIdQuery(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{eventId:guid}/albums")]
    [AllowAnonymous]
    public async Task<IActionResult> ListAlbums(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListEventAlbumsQuery(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/albums")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> CreateAlbum(
        Guid eventId,
        [FromBody] CreateEventAlbumBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateEventAlbumCommand(
                eventId,
                request.Title,
                request.Description,
                request.Visibility),
            cancellationToken);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpGet("{eventId:guid}/conversation")]
    public async Task<IActionResult> GetConversation(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetConversationByEventQuery(eventId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{eventId:guid}/reviews")]
    public async Task<IActionResult> ListReviews(
        Guid eventId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListReviewsForEventQuery(eventId, page, pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{eventId:guid}/reviewable")]
    public async Task<IActionResult> ListReviewablePeers(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListReviewablePeersQuery(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEventCommand(
            request.SportId,
            request.Title,
            request.Description,
            request.EventDate,
            request.DurationMinutes,
            request.Latitude,
            request.Longitude,
            request.Address,
            request.MaxParticipants,
            request.MinParticipantAge,
            request.MaxParticipantAge,
            request.SkillLevel);

        var result = await Sender.Send(command, cancellationToken);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPut("{eventId:guid}")]
    public async Task<IActionResult> UpdateDetails(
        Guid eventId,
        [FromBody] UpdateDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateEventDetailsCommand(eventId, request.Title, request.Description),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("recurring")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> CreateRecurring(
        [FromBody] CreateRecurringEventsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateRecurringEventsCommand(
            request.SportId, request.Title, request.Description, request.EventDate,
            request.DurationMinutes, request.Latitude, request.Longitude, request.Address,
            request.MaxParticipants, request.MinParticipantAge, request.MaxParticipantAge,
            request.IntervalWeeks, request.OccurrenceCount), cancellationToken);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPut("{eventId:guid}/schedule")]
    public async Task<IActionResult> UpdateSchedule(
        Guid eventId,
        [FromBody] UpdateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateEventScheduleCommand(eventId, request.EventDate, request.DurationMinutes),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{eventId:guid}/location")]
    public async Task<IActionResult> UpdateLocation(
        Guid eventId,
        [FromBody] UpdateEventLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateEventLocationCommand(
                eventId,
                request.Latitude,
                request.Longitude,
                request.Address),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{eventId:guid}/capacity")]
    public async Task<IActionResult> UpdateCapacity(
        Guid eventId,
        [FromBody] UpdateCapacityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateEventCapacityCommand(eventId, request.MaxParticipants),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new PublishEventCommand(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CancelEventCommand(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CompleteEventCommand(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/apply")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Apply(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ApplyToEventCommand(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/invitations/me/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new AcceptEventInvitationCommand(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/invitations/me/decline")]
    public async Task<IActionResult> DeclineInvitation(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeclineEventInvitationCommand(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{eventId:guid}/participants")]
    [AllowAnonymous]
    public async Task<IActionResult> ListParticipants(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListParticipantsQuery(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/participants/assign")]
    public async Task<IActionResult> AssignParticipants(
        Guid eventId,
        [FromBody] AssignParticipantsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AssignEventParticipantsCommand(eventId, request.Guests, request.FriendUserIds),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/participants/{participantId:guid}/remove")]
    public async Task<IActionResult> RemoveAssignedParticipant(
        Guid eventId,
        Guid participantId,
        [FromBody] RemoveParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RemoveAssignedParticipantCommand(
                eventId,
                participantId,
                request.ReportReasonId,
                request.Note),
            cancellationToken);

        return result.ToActionResult();
    }

    public sealed record RemoveParticipantRequest(Guid ReportReasonId, string? Note);

    [HttpPost("{eventId:guid}/participants/{userId:guid}/approve")]
    public async Task<IActionResult> ApproveParticipant(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ApproveParticipantCommand(eventId, userId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/participants/{userId:guid}/reject")]
    public async Task<IActionResult> RejectParticipant(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RejectParticipantCommand(eventId, userId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/participants/me/cancel")]
    public async Task<IActionResult> CancelMyParticipation(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CancelParticipationCommand(eventId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{eventId:guid}/participants/{userId:guid}/attended")]
    public async Task<IActionResult> ConfirmAttendance(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ConfirmAttendanceCommand(eventId, userId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/participants/{userId:guid}/no-show")]
    public async Task<IActionResult> MarkNoShow(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new MarkNoShowCommand(eventId, userId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{eventId:guid}/waitlist")]
    public async Task<IActionResult> ListWaitlist(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListWaitlistQuery(eventId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/waitlist/{userId:guid}/promote")]
    public async Task<IActionResult> PromoteFromWaitlist(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new PromoteFromWaitlistCommand(eventId, userId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{eventId:guid}/questions")]
    public async Task<IActionResult> ListQuestions(
        Guid eventId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListEventQuestionsQuery(eventId, page, pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{eventId:guid}/questions")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    [EnableRateLimiting(RateLimitingExtensions.EventQnAPolicy)]
    public async Task<IActionResult> AskQuestion(
        Guid eventId,
        [FromBody] AskEventQuestionBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AskEventQuestionCommand(eventId, request.Content),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPost("{eventId:guid}/questions/{questionId:guid}/replies")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    [EnableRateLimiting(RateLimitingExtensions.EventQnAPolicy)]
    public async Task<IActionResult> ReplyToQuestion(
        Guid eventId,
        Guid questionId,
        [FromBody] AskEventQuestionBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ReplyToEventQuestionCommand(eventId, questionId, request.Content),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    public sealed record CreateEventRequest(
        Guid SportId,
        string Title,
        string? Description,
        DateTimeOffset EventDate,
        int DurationMinutes,
        decimal Latitude,
        decimal Longitude,
        string Address,
        int? MaxParticipants,
        int MinParticipantAge,
        int MaxParticipantAge,
        short? SkillLevel = null);

    public sealed record CreateRecurringEventsRequest(
        Guid SportId, string Title, string? Description, DateTimeOffset EventDate,
        int DurationMinutes, decimal Latitude, decimal Longitude, string Address,
        int? MaxParticipants, int MinParticipantAge, int MaxParticipantAge,
        int IntervalWeeks, int OccurrenceCount);

    public sealed record UpdateDetailsRequest(string Title, string? Description);

    public sealed record UpdateScheduleRequest(DateTimeOffset EventDate, int DurationMinutes);

    public sealed record UpdateEventLocationRequest(decimal Latitude, decimal Longitude, string Address);

    public sealed record UpdateCapacityRequest(int? MaxParticipants);

    public sealed record AssignParticipantsRequest(
        IReadOnlyList<GuestAssignmentRequest>? Guests,
        IReadOnlyList<Guid>? FriendUserIds);

    public sealed record CreateEventAlbumBody(
        string Title,
        string? Description,
        short? Visibility);

    public sealed record AskEventQuestionBody(string Content);
}
