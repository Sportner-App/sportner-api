# User Blocks

## Module

Social

## Aggregate Root

UserBlock

---

# Purpose

The `UserBlocks` table stores a one-way hide between two users.

Blocking is independent of friendship. A user may block a stranger. Each direction is its own row, so A and B may both block each other.

---

# Responsibilities

- Persist who blocked whom
- Support the blocker's "blocked users" list
- Drive either-way visibility and interaction rules in Application queries and commands
- Unblock by physical delete (no soft delete)

---

# Columns

| Column             | Type        | Nullable | Description                |
| ------------------ | ----------- | -------- | -------------------------- |
| id                 | UUID        | No       | Primary Key                |
| blocker_user_id    | UUID        | No       | User who created the block |
| blocked_user_id    | UUID        | No       | User who is blocked        |
| created_at         | TIMESTAMPTZ | No       | When the block was created |
| updated_at         | TIMESTAMPTZ | Yes      | Audit                      |
| created_by_user_id | UUID        | Yes      | Audit                      |
| updated_by_user_id | UUID        | Yes      | Audit                      |

---

# Indexes

- PK(id)
- UNIQUE(blocker_user_id, blocked_user_id)
- INDEX(blocker_user_id, created_at)
- INDEX(blocked_user_id)

---

# Check Constraints

- blocker_user_id <> blocked_user_id

---

# Foreign Keys

| Column          | References |
| --------------- | ---------- |
| blocker_user_id | users(id)  |
| blocked_user_id | users(id)  |

---

# Relationships

## Belongs To

- users (Blocker)
- users (Blocked)

---

# Business Rules

- A user cannot block themselves.
- Re-blocking the same pair is idempotent (existing row is kept).
- Unblock deletes only the caller's row. The other party's row, if any, remains.
- Unblock does not restore a friendship.
- Blocking an accepted friend deletes the friendship row and decreases both `FriendsCount` values.
- Blocking a pending or rejected pair deletes that friendship row.
- Visibility is either-way: if a row exists in either direction, neither user should see the other in public lists, profiles, feeds, or discovery.
- The blocked user must not be told they were blocked (profile/post reads return not found).
- Actions (friend request, DM, apply, like, comment) return forbidden when a pair is blocked.
- Blocking does not create a report and does not kick existing event participants.
- Blocking does not auto-remove conversation membership.

---

# Notes

Friendship `Status = Blocked` is legacy. Historical blocked friendships are copied into this table and those friendship rows are deleted.
