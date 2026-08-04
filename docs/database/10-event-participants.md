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
| user_id            | UUID        | No       | References users(id)                       |
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

---

# Unique Constraints

- UNIQUE(event_id, user_id)

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

# Business Rules

- A user can apply only once for the same event.
- Organizer approval is required before joining.
- Organizer is automatically inserted as an approved participant.
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
