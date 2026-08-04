# Conversation Members

## Module

Messaging

## Aggregate Root

Conversation

---

# Purpose

The `conversation_members` table manages membership within conversations.

Only users who are members of a conversation are allowed to read and send messages.

For event conversations, members are automatically synchronized with approved event participants.

---

# Responsibilities

- Store conversation participants
- Control conversation access
- Track membership lifecycle
- Support future conversation roles

---

# Columns

| Column             | Type        | Nullable | Description                  |
| ------------------ | ----------- | -------- | ---------------------------- |
| id                 | UUID        | No       | Primary Key                  |
| conversation_id    | UUID        | No       | References conversations(id) |
| user_id            | UUID        | No       | References users(id)         |
| role               | SMALLINT    | No       | Member role                  |
| joined_at          | TIMESTAMPTZ | No       | Member joined date           |
| left_at            | TIMESTAMPTZ | Yes      | Member left date             |
| created_at         | TIMESTAMPTZ | No       | Created date                 |
| updated_at         | TIMESTAMPTZ | Yes      | Updated date                 |
| created_by_user_id | UUID        | Yes      | Audit                        |
| updated_by_user_id | UUID        | Yes      | Audit                        |

---

# Indexes

- PK(id)
- INDEX(conversation_id)
- INDEX(user_id)
- INDEX(role)

---

# Unique Constraints

- UNIQUE(conversation_id, user_id)

---

# Foreign Keys

| Column          | References        |
| --------------- | ----------------- |
| conversation_id | conversations(id) |
| user_id         | users(id)         |

---

# Relationships

## Belongs To

- conversations
- users

---

# Member Roles

| Value | Name      |
| ----- | --------- |
| 0     | Member    |
| 1     | Owner     |
| 2     | Moderator |

---

# Business Rules

- A user can join a conversation only once.
- The event organizer is automatically added as the Owner.
- Approved event participants are automatically added as Members.
- Waiting list users cannot join the conversation.
- Rejected users cannot join the conversation.
- Removing a participant from the event also removes them from the conversation.
- A member who has left the conversation cannot send new messages.
- Only active members can send messages.

---

# Lifecycle

### Event Published

↓

Conversation created

↓

Organizer added as Owner

↓

Participant approved

↓

Participant added as Member

↓

Participant leaves or is removed

↓

Conversation membership ends

↓

Event completed

↓

Conversation closed

---

# Performance Notes

Most queries filter by:

- conversation_id
- user_id

These fields should always be indexed to support fast authorization checks.

---

# Future Extensions

Possible future additions:

- Nickname inside conversation
- Mute notifications
- Last read message
- Last read timestamp
- Pinned member
- Custom permissions

---

# Notes

This table controls access to conversations.

Every message operation should first verify that the user has an active membership in the conversation.

For event conversations, membership is managed automatically by backend business logic and should never require manual intervention.
