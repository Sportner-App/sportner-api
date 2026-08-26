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

Recurring event series are intentionally excluded from the first version.

Every event record represents exactly one occurrence.

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
| title              | VARCHAR(150) |       No | Event title                                    |
| description        | TEXT         |      Yes | Optional event description                     |
| event_date         | TIMESTAMPTZ  |       No | Event start date and time                      |
| duration_minutes   | INTEGER      |       No | Estimated duration in minutes                  |
| latitude           | DECIMAL(9,6) |       No | Event latitude                                 |
| longitude          | DECIMAL(9,6) |       No | Event longitude                                |
| address            | TEXT         |       No | Full formatted address                         |
| max_participants   | INTEGER      |      Yes | Maximum occupied slots; `NULL` means unlimited |
| status             | SMALLINT     |       No | Event lifecycle status                         |
| created_at         | TIMESTAMPTZ  |       No | Creation timestamp                             |
| updated_at         | TIMESTAMPTZ  |      Yes | Last update timestamp                          |
| created_by_user_id | UUID         |      Yes | Audit field                                    |
| updated_by_user_id | UUID         |      Yes | Audit field                                    |

---

# Removed Fields

The following fields are not part of the first version:

- `is_recurring`
- `recurrence_rule`

Recurring events require a separate series model because every occurrence may have different:

- Participants
- Waiting lists
- Attendance
- Conversation messages
- Reviews
- Cancellation status

A future implementation should introduce an `event_series` model rather than storing recurrence rules directly on `events`.

---

# Indexes

- `PK(id)`
- `INDEX(organizer_user_id)`
- `INDEX(sport_id)`
- `INDEX(event_date)`
- `INDEX(status)`
- `INDEX(status, event_date)`

The composite index supports common discovery queries for upcoming published events.

---

# Foreign Keys

| Column            | References | Delete Behavior |
| ----------------- | ---------- | --------------- |
| organizer_user_id | users(id)  | Restrict        |
| sport_id          | sports(id) | Restrict        |

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

The organizer is automatically added as an approved participant and occupies one capacity slot.

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

- Organizer occupies one slot.
- Two organizer-assigned guests occupy two more slots (7 remain for the public).
- The first seven user applications create pending participant records.
- The next application enters the waiting list.
- Rejecting or cancelling a pending application frees one slot.
- Cancelling an approved participation also frees one slot.

When `max_participants` is `NULL`, capacity is unlimited and applications never enter the waiting list because of participant count.

---

# Business Rules

- Every event has exactly one organizer.
- The organizer is automatically added as an approved participant.
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
- Only users marked as attended become eligible to review other attendees.
- Cancelling an event stops applications and participant management.
- Completed and cancelled events cannot return to an earlier lifecycle state.

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
Includes the organizer as Owner
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

event_series for recurring event definitions
Event instances generated from a series
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
