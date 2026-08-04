# Friendships

## Module

Social

## Aggregate Root

Friendship

---

# Purpose

The `friendships` table manages friendship relationships between users.

A friendship begins with a friend request and becomes active after acceptance.

Only accepted friendships allow users to access friend-specific features such as direct messaging, upcoming events, and personalized recommendations.

Each friendship is represented by a single database record.

---

# Responsibilities

- Store friend requests
- Manage friendship lifecycle
- Prevent duplicate friendships
- Support friend discovery
- Enable friend-based features

---

# Columns

| Column             | Type        | Nullable | Description                   |
| ------------------ | ----------- | -------- | ----------------------------- |
| id                 | UUID        | No       | Primary Key                   |
| requester_user_id  | UUID        | No       | User who sent the request     |
| addressee_user_id  | UUID        | No       | User who received the request |
| status             | SMALLINT    | No       | Friendship status             |
| responded_at       | TIMESTAMPTZ | Yes      | Acceptance or rejection date  |
| created_at         | TIMESTAMPTZ | No       | Request creation date         |
| updated_at         | TIMESTAMPTZ | Yes      | Last update date              |
| created_by_user_id | UUID        | Yes      | Audit                         |
| updated_by_user_id | UUID        | Yes      | Audit                         |

---

# Indexes

- PK(id)
- INDEX(requester_user_id)
- INDEX(addressee_user_id)
- INDEX(status)

---

# Unique Constraints

- UNIQUE(requester_user_id, addressee_user_id)

---

# Check Constraints

- requester_user_id <> addressee_user_id

---

# Foreign Keys

| Column            | References |
| ----------------- | ---------- |
| requester_user_id | users(id)  |
| addressee_user_id | users(id)  |

---

# Relationships

## Belongs To

- users (Requester)
- users (Addressee)

---

# Friendship Status

| Value | Name     |
| ----- | -------- |
| 0     | Pending  |
| 1     | Accepted |
| 2     | Rejected |
| 3     | Blocked  |

---

# Business Rules

- A user cannot send a friend request to themselves.
- A user cannot send multiple pending requests to the same user.
- Friend requests require acceptance before becoming active.
- Either user may remove an accepted friendship.
- Blocking immediately ends the friendship if one exists.
- Blocked users cannot send friend requests or direct messages.
- Accepted friendships allow access to friend-only features.
- Friendship status changes must be handled only through backend business logic.

---

# Lifecycle

### Send Friend Request

↓

Status = Pending

↓

Recipient accepts

↓

Status = Accepted

↓

Users become friends

---

Alternative flows

Pending

↓

Rejected

or

Pending

↓

Blocked

or

Accepted

↓

Friendship removed

↓

Record deleted

---

# Performance Notes

Most queries retrieve friendships by:

- requester_user_id
- addressee_user_id
- status

Friend lists should always return only records with `Accepted` status.

---

# Future Extensions

Possible future additions:

- Best friends
- Close friends
- Favorite friends
- Friend nicknames
- Friendship anniversary
- Mutual friend count
- Friend categories

---

# Notes

Each friendship exists as a single record regardless of direction.

When querying a user's friends, both `requester_user_id` and `addressee_user_id` should be considered.

Removing a friendship permanently deletes the record.

Blocking is represented by the `Blocked` status and prevents future interactions until the block is removed.
