# Organizations

## Module

Organizations

## Aggregate Root

Organization

---

# Purpose

Stores invite-only communities (university teams, venue groups, clubs).

Membership is requested with a unique 8-character invite code and approved by the founder or an admin.

Organization events are private to approved members and are not listed in public discovery.

---

# Columns

| Column             | Type         | Nullable | Description                          |
| ------------------ | ------------ | -------- | ------------------------------------ |
| id                 | UUID         | No       | Primary key                          |
| founder_user_id    | UUID         | No       | Creating user; cannot leave          |
| name               | VARCHAR(80)  | No       | Display name                         |
| description        | TEXT         | Yes      | Optional, max 1000                   |
| city_id            | UUID         | Yes      | References `Cities(Id)`              |
| invite_code        | VARCHAR(8)   | No       | Unique, case-insensitive             |
| created_at         | TIMESTAMPTZ  | No       | Creation timestamp                   |
| updated_at         | TIMESTAMPTZ  | Yes      | Last update                          |
| created_by_user_id | UUID         | Yes      | Audit                                |
| updated_by_user_id | UUID         | Yes      | Audit                                |

---

# Indexes

- `PK(id)`
- `INDEX(founder_user_id)`
- `INDEX(city_id)`
- `UNIQUE(invite_code)`

---

# Foreign Keys

| Column          | References  | Delete Behavior |
| --------------- | ----------- | --------------- |
| founder_user_id | Users(Id)   | Restrict        |
| city_id         | Cities(Id)  | Restrict        |

---

# Business Rules

- Invite code is 8 characters from `23456789ABCDEFGHJKMNPQRSTUVWXYZ`.
- Only the founder may rotate the invite code.
- Founder and admins may update name, description and city.
- City, when set, must exist in the catalog.
- Organization events never appear on public / friends discovery.
