# Event Waitlist

## Module

Events

## Aggregate Root

Event

---

# Purpose

The `event_waitlist` table stores users who attempt to join an event after the maximum participant capacity has been reached.

Users on the waiting list are not considered participants.

If a participant leaves or is removed from the event, the organizer can manually approve users from the waiting list.

---

# Responsibilities

- Store waiting list entries
- Preserve waiting order
- Support manual promotion to participants
- Prevent duplicate waiting list entries

---

# Columns

| Column             | Type        | Nullable | Description                           |
| ------------------ | ----------- | -------- | ------------------------------------- |
| id                 | UUID        | No       | Primary Key                           |
| event_id           | UUID        | No       | References events(id)                 |
| user_id            | UUID        | No       | References users(id)                  |
| position           | INTEGER     | No       | Position in the waiting list          |
| created_at         | TIMESTAMPTZ | No       | Date the user joined the waiting list |
| updated_at         | TIMESTAMPTZ | Yes      | Last update date                      |
| created_by_user_id | UUID        | Yes      | Audit                                 |
| updated_by_user_id | UUID        | Yes      | Audit                                 |

---

# Indexes

- PK(id)
- INDEX(event_id)
- INDEX(user_id)
- INDEX(position)

---

# Unique Constraints

- UNIQUE(event_id, user_id)
- UNIQUE(event_id, position)

---

# Foreign Keys

| Column   | References |
| -------- | ---------- |
| event_id | events(id) |
| user_id  | users(id)  |

---

# Relationships

## Belongs To

- events
- users

---

# Business Rules

- Users are added to the waiting list only when the event has reached its participant limit.
- A user can appear only once in the waiting list for an event.
- Users on the waiting list are **not** event participants.
- Promotion from the waiting list is always initiated by the organizer.
- Promoting a user removes them from the waiting list and creates a new record in `event_participants`.
- Waiting list order is determined by the `position` field.

---

# Lifecycle

### Event Full

↓

User requests to join

↓

User is added to the waiting list

↓

A participant leaves the event

↓

Organizer reviews the waiting list

↓

Organizer approves a user

↓

User is removed from the waiting list

↓

A new participant record is created

---

# Performance Notes

Queries will typically filter by:

- event_id
- position

Waiting lists are expected to remain small, but indexing ensures efficient ordering and retrieval.

---

# Future Extensions

Possible future additions:

- Automatic promotion
- Waiting list expiration
- Priority users
- Premium queue priority
- Organizer notes

---

# Notes

The waiting list is intentionally separated from the participant lifecycle.

This simplifies business logic and keeps `event_participants` focused solely on users actively involved in the participation workflow.

Promotion from the waiting list should always be performed through backend business logic to ensure participant limits are respected.
