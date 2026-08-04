# Events

## Module

Events

## Aggregate Root

Event

---

# Purpose

The `events` table stores all sports events created on the platform.

An event represents a scheduled activity organized by a user and joined by other participants.

Events support both one-time and recurring schedules.

This table contains only event metadata.

Participants, waiting lists and conversations are managed by separate tables.

---

# Responsibilities

- Store event information
- Manage event lifecycle
- Store event location
- Store participant limits
- Support recurring events
- Define organizer ownership

---

# Columns

| Column             | Type         | Nullable | Description                                  |
| ------------------ | ------------ | -------- | -------------------------------------------- |
| id                 | UUID         | No       | Primary Key                                  |
| organizer_user_id  | UUID         | No       | References users(id)                         |
| sport_id           | UUID         | No       | References sports(id)                        |
| title              | VARCHAR(150) | No       | Event title                                  |
| description        | TEXT         | Yes      | Event description                            |
| event_date         | TIMESTAMPTZ  | No       | Event start date                             |
| duration_minutes   | INTEGER      | No       | Estimated duration                           |
| latitude           | DECIMAL(9,6) | No       | Latitude                                     |
| longitude          | DECIMAL(9,6) | No       | Longitude                                    |
| address            | TEXT         | No       | Full address                                 |
| max_participants   | INTEGER      | Yes      | Maximum participant count (NULL = Unlimited) |
| is_recurring       | BOOLEAN      | No       | Indicates recurring event                    |
| recurrence_rule    | TEXT         | Yes      | RRULE definition for recurring events        |
| status             | SMALLINT     | No       | Event status                                 |
| created_at         | TIMESTAMPTZ  | No       | Created date                                 |
| updated_at         | TIMESTAMPTZ  | Yes      | Updated date                                 |
| created_by_user_id | UUID         | Yes      | Audit                                        |
| updated_by_user_id | UUID         | Yes      | Audit                                        |

---

# Indexes

- PK(id)
- INDEX(organizer_user_id)
- INDEX(sport_id)
- INDEX(event_date)
- INDEX(status)

---

# Foreign Keys

| Column            | References |
| ----------------- | ---------- |
| organizer_user_id | users(id)  |
| sport_id          | sports(id) |

---

# Relationships

## Belongs To

- users (Organizer)
- sports

## Referenced By

- event_participants
- event_waitlist
- conversations
- reviews

---

# Event Status

| Value | Name      |
| ----- | --------- |
| 0     | Draft     |
| 1     | Published |
| 2     | Full      |
| 3     | Completed |
| 4     | Cancelled |

---

# Business Rules

- Every event has exactly one organizer.
- Organizer is automatically considered an approved participant.
- Only active users can create events.
- Only active sports can be selected.
- Events cannot be created in the past.
- `max_participants` may be NULL for unlimited events.
- When capacity is reached, new applications are added to the waiting list.
- Completing an event enables participant reviews.
- Cancelling an event closes new applications.

---

# Lifecycle

### Draft

The organizer prepares the event.

↓

### Published

Users can submit participation requests.

↓

### Full (Optional)

Maximum participant count has been reached.

↓

### Completed

The organizer marks attendance.

Participants can review each other.

↓

### Cancelled

The event is cancelled.

Waiting list and participant notifications are sent.

---

# Performance Notes

The majority of queries will filter by:

- Sport
- Event date
- Status
- Organizer
- User location (handled by PostGIS in the future)

Indexes should prioritize these access patterns.

---

# Future Extensions

Possible future additions:

- Paid events
- Skill level requirement
- Gender restriction
- Age restriction
- Private events
- Invite-only events
- Event cover image
- Event tags
- Weather snapshot
- Cancellation reason

No schema redesign should be required.

---

# Notes

This table stores only event metadata.

Participants, waiting lists, chats, attendance and reviews are intentionally separated into dedicated tables.

This keeps the Event aggregate modular and scalable.
