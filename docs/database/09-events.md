# Events

## Module

Events

## Aggregate Root

Event

---

# Purpose

The `events` table stores individual sports events created on the platform.

An event represents one scheduled sports activity organized by a user and attended by approved participants.

Each event has its own:

- Schedule
- Location
- Participant applications
- Waiting list
- Attendance records
- Conversation
- Reviews
- Lifecycle status

Every event record represents exactly one occurrence. Recurring events are
modelled as a chain of such occurrences linked by `series_id`; see
[Recurring Series](#recurring-series).

---

# Responsibilities

- Store event information
- Manage event lifecycle
- Store event schedule and duration
- Store event location
- Manage participant capacity
- Define organizer ownership
- Act as the parent entity for participation and waiting-list records

---

# Columns

| Column             | Type         | Nullable | Description                                    |
| ------------------ | ------------ | -------: | ---------------------------------------------- |
| id                 | UUID         |       No | Primary key                                    |
| organizer_user_id  | UUID         |       No | References `users(id)`                         |
| sport_id           | UUID         |       No | References `sports(id)`                        |
| organization_id    | UUID         |      Yes | Club the event belongs to; `NULL` for a personal event |
| is_organization_only | BOOLEAN    |       No | Default false; true = visible/joinable by approved club members only. Forced false when `organization_id` is NULL |
| is_organizer_participant | BOOLEAN |       No | Default true. Whether the creator also plays. `false` is only allowed on club events (`organization_id` set); no active organizer participant row exists then |
| title              | VARCHAR(150) |       No | Event title                                    |
| description        | TEXT         |      Yes | Optional event description                     |
| event_date         | TIMESTAMPTZ  |       No | Event start date and time                      |
| duration_minutes   | INTEGER      |      Yes | Estimated duration in minutes; when omitted (`NULL`) end-time logic (completion, attendance, chat lock) assumes 60 minutes (`Event.DefaultDurationMinutes`) |
| latitude           | DECIMAL(9,6) |       No | Event latitude                                 |
| longitude          | DECIMAL(9,6) |       No | Event longitude                                |
| address            | TEXT         |       No | Full formatted address                         |
| district           | VARCHAR(100) |      Yes | District parsed from `address` (`EventAddressParts`); key of district rankings |
| city               | VARCHAR(100) |      Yes | City parsed from `address`; used together with `district` |
| max_participants   | INTEGER      |      Yes | Maximum occupied slots; `NULL` means unlimited |
| skill_level        | SMALLINT     |      Yes | Optional informational skill (0–4); not a join gate |
| is_paid            | BOOLEAN      |       No | Whether attendance has an advertised fee; default `false` |
| fee_amount         | DECIMAL(10,2)|      Yes | Advertised TRY amount when paid; `NULL` when free |
| status             | SMALLINT     |       No | Event lifecycle status                         |
| series_id          | UUID         |      Yes | Groups the occurrences of one recurring series; `NULL` for standalone events |
| series_sequence    | INTEGER      |      Yes | 1-based position within the series             |
| series_interval_weeks | INTEGER   |      Yes | Weeks between occurrences (1, 2 or 4)          |
| series_total_occurrences | INTEGER |     Yes | How many occurrences the series will produce in total |
| created_at         | TIMESTAMPTZ  |       No | Creation timestamp                             |
| updated_at         | TIMESTAMPTZ  |      Yes | Last update timestamp                          |
| created_by_user_id | UUID         |      Yes | Audit field                                    |
| updated_by_user_id | UUID         |      Yes | Audit field                                    |

---

# Recurring Series

Every event record still represents exactly one occurrence — each has its own
participants, waiting list, attendance, conversation, reviews and cancellation
status. A series is only a lightweight link between those occurrences.

Occurrences are **not** materialised upfront. `POST /api/events/recurring`
creates one event, stamps it with a fresh `series_id`, `series_sequence = 1`
and the series quota. The `event-series` cron job in `Sportner.Events.Worker`
then finds series whose newest occurrence has passed `event_date +
duration_minutes` and, while `series_sequence < series_total_occurrences`,
clones that occurrence forward by `series_interval_weeks` and publishes it.

Consequences worth knowing:

- Editing an occurrence changes the template the next one is cloned from.
- Cancelling an occurrence does not stop the series; the job advances from the
  newest occurrence regardless of its status.
- If the worker is down, no occurrences are lost: the next run rolls the date
  forward on the same weekly rhythm until it lands in the future.

A dedicated `event_series` table was deliberately not introduced — the series
carries no state beyond the template already stored on each occurrence.

# Removed Fields

The following fields are not part of the first version:

- `is_recurring`
- `recurrence_rule`

Recurrence is expressed by the `series_*` columns above rather than by an
RFC 5545-style recurrence rule.

---

# Indexes

- `PK(id)`
- `INDEX(organizer_user_id)`
- `INDEX(sport_id)`
- `INDEX(event_date)`
- `INDEX(status)`
- `INDEX(skill_level)`
- `INDEX(is_paid)`
- `INDEX(organization_id)`
- `INDEX(status, event_date)`
- `INDEX(city, district, event_date)`

The composite index supports common discovery queries for upcoming published events.

---

# Foreign Keys

| Column            | References | Delete Behavior |
| ----------------- | ---------- | --------------- |
| organizer_user_id | users(id)          | Restrict        |
| sport_id          | sports(id)         | Restrict        |
| organization_id   | organizations(id)  | Restrict        |

Events must remain available for historical participation, review and moderation records.

Users and sports referenced by historical events must not be cascade-deleted.

---

# Relationships

## Belongs To

- `users` as Organizer
- `sports`

## Owns

- `event_participants`
- `event_waitlist`

## Referenced By

- `conversations`
- `reviews`
- `notifications`
- `reports`

---

# Event Status

| Value | Name      |
| ----: | --------- |
|     0 | Draft     |
|     1 | Published |
|     2 | Full      |
|     3 | Completed |
|     4 | Cancelled |

Enum values must match `database-reference.md`.

---

# Capacity Definition

The organizer is automatically added as an approved participant and occupies one capacity slot, unless the event is a club event whose creator opted out (`is_organizer_participant = false`, see Organizer participation); then the organizer occupies no slot.

The following participant statuses occupy capacity:

- `Pending`
- `Approved`
- `Attended`
- `NoShow`

The following statuses do not occupy capacity:

- `Rejected`
- `Cancelled`

This means pending applications temporarily reserve event capacity.

Example for an event with `max_participants = 10`:

- Organizer occupies one slot (unless a club event creator opted out; then 8 remain after the guests below).
- Two organizer-assigned guests occupy two more slots (7 remain for the public).
- The first seven user applications create pending participant records.
- The next application enters the waiting list.
- Rejecting or cancelling a pending application frees one slot.
- Cancelling an approved participation also frees one slot.

When `max_participants` is `NULL`, capacity is unlimited and applications never enter the waiting list because of participant count.

---

# Business Rules

- Every event has exactly one organizer (the creator). Wherever this document says the organizer manages something (approve/reject, assign/remove, waitlist promotion, edit, cancel, attendance), club founder/admins may do it too on club events (see [Club co-management](#club-co-management)).
- The organizer is automatically added as an approved participant, except on club events created with `OrganizerParticipates = false` (see Organizer participation).
- Only active users may create events.
- Only active sports may be selected.
- An event cannot be created in the past.
- Duration must be greater than zero.
- `max_participants`, when provided, must be greater than zero.
- Capacity cannot be reduced below the current occupied participant count.
- Event location must contain valid coordinates and a non-empty address.
- Participant applications require organizer approval.
- When capacity is full, new applications enter the waiting list.
- Waiting-list users are not participants.
- Waiting-list promotion is always initiated manually by the organizer.
- Completing an event enables attendance confirmation.
- Only users marked as attended become eligible to review other attendees. Exception: the creator is always eligible (reviews and is reviewed) only while `is_organizer_participant` is true; a non-participating club-event creator neither reviews nor is reviewed.
- Cancelling an event stops applications and participant management.
- Completed and cancelled events cannot return to an earlier lifecycle state.
- `is_paid` is informational only. The platform does not collect, hold, or transfer money.
- When `is_paid` is `false`, `fee_amount` must be `NULL`.
- When `is_paid` is `true`, `fee_amount` is required, must be greater than zero, and is stored as TRY with two decimal places (maximum `99999.99`).
- Existing events without a fee default to free.

---

# Organizer participation (club events)

A club event is the club's event: responses always carry `organizationId/organizationName/organizationImageUrl`, and the app shows the club as host whether or not the creator plays. The creator chooses whether they also take a player slot.

- `is_organizer_participant` (default `true`, so every pre-existing event keeps its behavior). `false` is only valid when `organization_id` is set (`Event.Create` throws otherwise; the create validator rejects it).
- `CreateEvent` accepts optional `organizerParticipates` (null means true). When `false` no organizer participant row is created and no capacity is consumed.
- `POST /api/events/{eventId}/organizer-participation` with `{ "participates": bool }` toggles it. Only the creator may call it (club managers cannot, it is personal). Allowed on club events only (`Event.OrganizerParticipationClubOnly`), before the event ends and while it is not Cancelled/Completed (`Event.ParticipationLocked`). Draft is allowed.
- Joining needs a free slot (`Event.CapacityFull`); an existing cancelled organizer row is reopened as Approved, otherwise a new Approved row is added. Leaving cancels the organizer row and frees the slot exactly like a normal cancellation: the capacity status is refreshed (Full becomes Published) and nobody is promoted from the waiting list automatically. Requesting the current state is a no-op.
- Counters: `EventsJoined` is never bumped for the organizer (neither at creation nor on toggle); hosted/cancelled event counters stay attached to the creator. The creator stays a member of the event conversation either way.
- Reviews: when `is_organizer_participant = false` the creator neither reviews nor is reviewed for that event (`ReviewEligibility`, `ListReviewablePeers`). Streaks, recaps, leaderboards, challenge "played" metrics and engagement reminders do not credit a non-playing creator with having played; challenge/`EventsOrganized` hosting credit is kept. The attendance prompt and share prompt still go to the creator.
- `EventParticipantRemoval.OrganizerUserId` (a legacy name) stores the user who performed the removal: the creator, or the club manager who removed the participant.
- Recurring series occurrences copy the creator's choice.

# Club co-management

Events with `organization_id` can be managed by the creator **and** by approved Founder/Admin members of a non-deleted club (`EventAccess.CanManageAsync` / `LoadOrganizerEventAsync`). This covers edit/update, publish, cancel, complete, approve/reject, assign/remove participants, waitlist list/promote, participant list (pending applicants), pending join-request list, attendance confirmation, event albums and Q&A replies. A deleted club grants no manager rights. Personal checks stay creator-only: applying to own event, organizer participation toggle, attendance/share prompts, `EventsOrganized`-style statistics.

- Drafts are visible only to people who can manage the event: event detail returns `Event.NotFound` to everyone else and the club event list omits drafts for non-managers; discovery, recommendations and my-participating lists never include drafts.
- Cancelling or updating as a manager notifies everyone except the actor and also the creator (even when not a participant); `EventsCancelled` is charged to the creator only when the creator cancelled (not a club manager, not a club deletion). Attendance prompts go to the creator and approved club managers, and the pending-attendance list includes club events the caller manages.
- Event detail has `canManage` (creator or club manager) next to `canCancel`; list items and detail carry `organizationImageUrl` and `isOrganizerParticipant`.
- Q&A answers written by the creator or a club manager on a club event carry `answeredByClub = true` plus `clubName/clubImageUrl`; the app shows the club as the answerer.
- Join requests (and new questions) notify the creator plus the club's approved Founder/Admin members, deduplicated and excluding the actor.
- Notifications sent in the organizer's name (cancel, update, approve/reject, assign/invite, remove, waitlist promotion, Q&A reply by a manager) use the club name as actor when the club still exists.
- Managers who are not participants are not added to the event conversation.
- Inviting people (`friendUserIds` of assign-participants): for a club event each invitee must be an **approved member** of a non-deleted club (`Event.InviteeNotClubMember`, 403 otherwise); friendship is not required. For non-club events invitees must still be accepted friends of the acting user (`Event.NotFriends`). Block, deleted/banned user, age/gender, already-associated and capacity checks apply in both cases.

---

# Lifecycle

## Draft

The organizer prepares the event.

Allowed actions:

- Update details
- Update schedule
- Update location
- Update capacity
- Publish
- Cancel

Applications are not accepted.

---

## Published

The event is visible and accepts applications.

Allowed actions:

- Receive applications
- Approve or reject pending applicants
- Manage the waiting list
- Update permitted event information
- Cancel
- Completes automatically after the scheduled end (`event_date + duration_minutes`)

---

## Full

The occupied capacity has reached `max_participants`.

Behavior:

- New applications enter the waiting list.
- Existing pending applications may still be approved or rejected.
- Rejection or cancellation may free capacity.
- When capacity becomes available, status returns to `Published`.
- Waiting-list promotion remains organizer-controlled.
- The event may be cancelled or completed.

---

## Completed

The event has ended.

Behavior:

- No new applications
- No participant approval or rejection
- Attendance may be confirmed
- Approved participants may be marked as `Attended` or `NoShow`
- Reviews become available only for attended users
- Event conversation becomes read-only
- Status cannot return to an earlier value

The event cannot be completed before:

```text
event_date + duration_minutes
```

## Cancelled

The event has been cancelled.

Behavior:

No new applications
No participant or waiting-list changes
Related users receive notifications
Event conversation is closed
Historical records remain available
Status cannot return to an earlier value

# Application Flow

## Capacity Available
User applies
    ↓
event_participants record created
    ↓
status = Pending
    ↓
Organizer approves or rejects

## Capacity Full

User applies
    ↓
event_waitlist record created
    ↓
No participant record is created
    ↓
Organizer selects a waitlist user when capacity becomes available
    ↓
Waitlist entry is removed
    ↓
Approved participant record is created

# Attendance Rules

Attendance is managed only after the event becomes Completed.

The organizer confirms each approved participant as either:

Attended
NoShow

Pending, rejected and cancelled participants cannot receive an attendance result.

Review eligibility is granted only when:

participant.status = Attended

No-show users are not eligible to review.

# Conversation Rules

Every published event has exactly one event conversation.

The conversation is managed by the Messaging module.

The events table does not store a conversation_id.

Instead:

conversations.event_id → events.id

The conversation:

Is created when the event is published
Includes the organizer as Owner (also when a club-event creator opted out of playing; club managers who are not participants are not added)
Includes approved participants as Members
Excludes pending and waiting-list users
Becomes read-only when the event is completed or cancelled
# Deletion Policy

Events are not physically deleted after publication.

Draft events with no dependent records may be physically removed if the application explicitly supports draft deletion.

Published, full, completed and cancelled events must remain in the database because they may be referenced by:

Participants
Waiting lists
Messages
Reviews
Notifications
Reports
User statistics

Lifecycle changes must be managed through status.

# Performance Notes

Common event queries filter by:

Status
Event date
Sport
Organizer
Geographic proximity

Upcoming event lists should generally filter with:

status IN (Published, Full)
event_date > now

Collection endpoints must use pagination.

Location-based search may initially use latitude and longitude bounding queries.

PostGIS may be introduced later without changing the core event ownership model.

# Future Extensions

Possible future additions include:

Skill-level requirements
Friends-only or private visibility
Invite-only participation
Event cover media
Event tags
Paid events
Payment and refund records
Cancellation reasons
Check-in with QR code
GPS attendance verification
Weather snapshots
Event invitations

Recurring events must be implemented through a dedicated event_series model rather than reintroducing recurrence fields directly into this table.

# Notes

The events table stores one concrete event occurrence.

Participants, waiting lists, conversations, attendance and reviews are intentionally stored in dedicated tables.

The event aggregate is responsible for enforcing:

Lifecycle transitions
Capacity
Participant applications
Organizer approval
Waiting-list promotion
Attendance eligibility

The database protects structural consistency through foreign keys and constraints.

Business workflow and aggregate invariants are enforced by backend domain logic.
