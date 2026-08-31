# Users

## Module

Identity

## Aggregate Root

User

---

# Purpose

The `users` table is the core identity table of the application.

It stores only authentication and account lifecycle information.

Public profile information is intentionally stored in the `user_profiles` table.

---

# Responsibilities

- Authentication
- Phone verification
- Account status
- Onboarding completion state
- Last activity
- Audit information

This table must never contain profile-related information.

---

# Columns

| Column             | Type        | Nullable | Description             |
| ------------------ | ----------- | -------- | ----------------------- |
| id                 | UUID        | No       | Primary Key             |
| phone_number       | VARCHAR(20) | Yes      | Optional contact phone  |
| phone_verified_at  | TIMESTAMPTZ | Yes      | Phone verification date |
| password_hash      | VARCHAR(500)| Yes      | Password auth hash (PBKDF2) |
| status             | SMALLINT    | No       | User status             |
| onboarding_completed_at | TIMESTAMPTZ | Yes | Onboarding completion date |
| last_seen_at       | TIMESTAMPTZ | Yes      | Last activity           |
| created_at         | TIMESTAMPTZ | No       | Creation date           |
| updated_at         | TIMESTAMPTZ | Yes      | Last update date        |
| created_by_user_id | UUID        | Yes      | Audit                   |
| updated_by_user_id | UUID        | Yes      | Audit                   |

---

# Indexes

- PK(id)
- UNIQUE(phone_number) WHERE phone_number IS NOT NULL
- INDEX(status)
- INDEX(last_seen_at)

---

# Relationships

## One To One

- user_profiles
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
- user_blocks
- user_badges

---

# Status

| Value | Name                |
| ----- | ------------------- |
| 0     | PendingVerification |
| 1     | Active              |
| 2     | Suspended           |
| 3     | Banned              |
| 4     | Deleted             |

---

# Business Rules

- Phone number must be unique.
- A user can exist without a profile.
- `onboarding_completed_at` stays NULL until the client explicitly completes the onboarding flow.
- Onboarding cannot be completed before the profile exists and at least one sport with a skill level is selected.
- Completing onboarding is idempotent; the first completion date is never overwritten.
- The authentication response exposes the onboarding state so clients can redirect incomplete users to the onboarding screen.
- A user cannot log in until the phone number is verified.
- Deleted users cannot authenticate.
- Suspended users cannot create events or posts.
- Banned users cannot use the platform.

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
