# Event Participants

## Module

Events

## Aggregate Root

Event

---

# Purpose

The `event_participants` table stores every user's participation lifecycle for an event.

A record is created as soon as a user submits a participation request.

The participant status changes throughout the event lifecycle, allowing the application to track applications, approvals, attendance and review eligibility.

---

# Responsibilities

- Store participation requests
- Store organizer decisions
- Track attendance
- Enable participant reviews
- Preserve participation history

---

# Columns

| Column             | Type        | Nullable | Description                                |
| ------------------ | ----------- | -------- | ------------------------------------------ |
| id                 | UUID        | No       | Primary Key                                |
| event_id           | UUID        | No       | References events(id)                      |
| user_id            | UUID        | Yes      | References users(id). Null for guest rows  |
| kind               | SMALLINT    | No       | Registered (0) or Guest (1)                |
| guest_first_name   | TEXT        | Yes      | Optional display name for guests           |
| guest_last_name    | TEXT        | Yes      | Optional display name for guests           |
| status             | SMALLINT    | No       | Participation status                       |
| joined_at          | TIMESTAMPTZ | Yes      | Approval date                              |
| attended_at        | TIMESTAMPTZ | Yes      | Attendance confirmation date               |
| left_at            | TIMESTAMPTZ | Yes      | Leave or cancellation date                 |
| can_review         | BOOLEAN     | No       | Whether the participant can submit reviews |
| created_at         | TIMESTAMPTZ | No       | Application date                           |
| updated_at         | TIMESTAMPTZ | Yes      | Last update                                |
| created_by_user_id | UUID        | Yes      | Audit                                      |
| updated_by_user_id | UUID        | Yes      | Audit                                      |

---

# Indexes

- PK(id)
- INDEX(event_id)
- INDEX(user_id)
- INDEX(status)
- INDEX(kind)

---

# Unique Constraints

- UNIQUE(event_id, user_id) WHERE user_id IS NOT NULL

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

# Participant Status

| Value | Name      |
| ----- | --------- |
| 0     | Pending   |
| 1     | Approved  |
| 2     | Rejected  |
| 3     | Cancelled |
| 4     | Attended  |
| 5     | NoShow    |

---

# Participant Kind

| Value | Name       |
| ----- | ---------- |
| 0     | Registered |
| 1     | Guest      |

`ParticipantStatus` is the lifecycle. `ParticipantKind` is the identity type. Do not store "anonymous" as a status.

---

# Business Rules

- A registered user has at most one participant row per event (`UNIQUE(event_id, user_id)` where `user_id` is not null).
- Guest rows have `user_id = null`, `kind = Guest`, and optional `guest_first_name` / `guest_last_name`.
- Guests are created as Approved by the organizer and occupy capacity immediately.
- Guests cannot review, join chat, receive notifications, or be marked Attended / NoShow.
- The organizer may assign accepted friends as Approved participants (draft, published, or full when slots remain).
- Cancelled users may apply again; the existing row returns to Pending (or waitlist if the event is full).
- Organizer approval is required before a registered user joins by applying.
- Organizer is automatically inserted as an approved registered participant.
- Rejected users cannot apply again unless the organizer reopens applications.
- If the participant limit is reached, new applications are stored in the waiting list instead.
- Only attendees can review other participants.
- A participant marked as **NoShow** affects attendance statistics.
- Attendance is confirmed by the organizer after the event.

---

# Lifecycle

### Pending

User submits a participation request.

↓

### Approved

Organizer accepts the request.

↓

### Attended

Organizer confirms attendance after the event.

↓

### Review Enabled

Participant can review other attendees.

---

Alternative flows

Pending

↓

Rejected

or

Pending

↓

Cancelled (by participant)

or

Approved

↓

NoShow

---

# Performance Notes

Most queries filter by:

- event_id
- user_id
- status

These columns should always be indexed.

---

# Future Extensions

Possible future additions:

- Join message
- Organizer notes
- Check-in timestamp
- Check-out timestamp
- QR code attendance
- Attendance verification by GPS

---

# Notes

This table represents the complete participation lifecycle.

Waiting list entries are managed separately in the `event_waitlist` table.

Participant reviews are only allowed when `can_review = true`.
