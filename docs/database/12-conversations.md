# Conversations

## Module

Messaging

## Aggregate Root

Conversation

Child entities:

- ConversationMember

Domain aggregate boundary:

```text
Conversation
└── ConversationMember
```

`Message` is a separate aggregate root that references `Conversation` by id.

Database relationships are unchanged. Messages remain in the `messages` table and continue to reference `conversations(id)`.

---

# Purpose

The `conversations` table represents chat rooms within the application.

A conversation serves as a container for participants and conversation metadata.

Messages belong to a separate Message aggregate and are stored in the `messages` table.

Initially, conversations are created automatically for events. The same infrastructure is designed to support direct messaging and group chats in future versions.

Messages are stored separately in the `messages` table.

---

# Responsibilities

- Store conversation metadata
- Associate conversations with events
- Define conversation type
- Manage conversation lifecycle

---

# Columns

| Column             | Type         | Nullable | Description                                  |
| ------------------ | ------------ | -------- | -------------------------------------------- |
| id                 | UUID         | No       | Primary Key                                  |
| type               | SMALLINT     | No       | Conversation type                            |
| event_id           | UUID         | Yes      | References events(id)                        |
| title              | VARCHAR(100) | Yes      | Conversation title                           |
| is_closed          | BOOLEAN      | No       | Indicates whether the conversation is closed |
| closed_at          | TIMESTAMPTZ  | Yes      | Conversation close date                      |
| created_at         | TIMESTAMPTZ  | No       | Created date                                 |
| updated_at         | TIMESTAMPTZ  | Yes      | Updated date                                 |
| created_by_user_id | UUID         | Yes      | Audit                                        |
| updated_by_user_id | UUID         | Yes      | Audit                                        |

---

# Indexes

- PK(id)
- INDEX(type)
- INDEX(event_id)
- INDEX(is_closed)

---

# Unique Constraints

- UNIQUE(event_id)

(Event conversations have exactly one conversation.)

---

# Foreign Keys

| Column   | References |
| -------- | ---------- |
| event_id | events(id) |

---

# Relationships

## Belongs To

- events (optional)

## Referenced By

- conversation_members
- messages

---

# Conversation Types

| Value | Name   |
| ----- | ------ |
| 0     | Event  |
| 1     | Direct |
| 2     | Group  |

---

# Business Rules

- Every event has exactly one conversation.
- Event conversations are created automatically when an event is published.
- Event conversations are closed automatically after the event ends.
- Closed conversations become read-only.
- Direct and Group conversations are supported (`CreateDirectConversation` / `CreateGroupConversation`). Group max members = 50. Direct membership is fixed (2).

---

# Lifecycle

### Event Created

↓

Conversation created automatically

↓

Participants are approved

↓

Members are added automatically

↓

Event ends

↓

Conversation is closed

↓

Messages remain accessible as read-only

---

# Performance Notes

Queries typically filter by:

- event_id
- type
- is_closed

The number of conversations is expected to remain significantly lower than the number of messages.

---

# Future Extensions

Possible future additions:

- Conversation avatar
- Last message cache
- Pinned messages
- Archived conversations
- Muted conversations
- Conversation description
- Admin users
- Typing indicators

---

# Notes

A conversation contains only metadata and membership.

Participants are managed in `conversation_members`.

Messages are a separate aggregate root stored in the `messages` table and reference the conversation by id.

Closing a conversation does not delete its messages.

Conversation history remains available for future reference.
