# Users

## Module

Identity

## Aggregate Root

User

---

# Purpose

The `users` table is the core identity table of the application.

It stores only authentication and account lifecycle information.

Public profile information is intentionally stored in the `profiles` table.

---

# Responsibilities

- Authentication
- Phone verification
- Account status
- Last activity
- Audit information

This table must never contain profile-related information.

---

# Columns

| Column             | Type        | Nullable | Description             |
| ------------------ | ----------- | -------- | ----------------------- |
| id                 | UUID        | No       | Primary Key             |
| phone_number       | VARCHAR(20) | No       | User phone number       |
| phone_verified_at  | TIMESTAMPTZ | Yes      | Phone verification date |
| status             | SMALLINT    | No       | User status             |
| last_seen_at       | TIMESTAMPTZ | Yes      | Last activity           |
| created_at         | TIMESTAMPTZ | No       | Creation date           |
| updated_at         | TIMESTAMPTZ | Yes      | Last update date        |
| created_by_user_id | UUID        | Yes      | Audit                   |
| updated_by_user_id | UUID        | Yes      | Audit                   |

---

# Indexes

- PK(id)
- UNIQUE(phone_number)
- INDEX(status)
- INDEX(last_seen_at)

---

# Relationships

## One To One

- profiles
- user_statistics

## One To Many

- user_sessions
- user_devices
- user_saved_locations
- user_sports
- events (Organizer)
- event_participants
- event_waitlist
- reviews
- posts
- notifications
- friendships
- user_badges

---

# Status

| Value | Name                |
| ----- | ------------------- |
| 0     | PendingVerification |
| 1     | Active              |
| 2     | Suspended           |
| 3     | Blocked             |
| 4     | Deleted             |

---

# Business Rules

- Phone number must be unique.
- A user can exist without a profile.
- A user cannot log in until the phone number is verified.
- Deleted users cannot authenticate.
- Suspended users cannot create events or posts.
- Blocked users cannot use the platform.

---

# Future Extensions

Possible future additions:

- Email authentication
- Two-factor authentication
- External providers (Google, Apple)

No schema changes should be required for these features.

---

# Notes

This table must remain small.

Only identity and authentication data belong here.

Any public information must be stored in related tables.
