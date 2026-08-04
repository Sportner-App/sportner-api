# Messages

## Module

Messaging

## Aggregate Root

Conversation

---

# Purpose

The `messages` table stores all messages sent within conversations.

Each message belongs to exactly one conversation and is sent by one user.

The messaging infrastructure is designed to support text, media and system messages while remaining scalable for future messaging features.

Messages are never physically deleted to preserve conversation history and reply relationships.

---

# Responsibilities

- Store conversation messages
- Support different message types
- Preserve conversation history
- Support message editing
- Support message replies
- Support media attachments

---

# Columns

| Column              | Type         | Nullable | Description                          |
| ------------------- | ------------ | -------- | ------------------------------------ |
| id                  | UUID         | No       | Primary Key                          |
| conversation_id     | UUID         | No       | References conversations(id)         |
| sender_user_id      | UUID         | No       | References users(id)                 |
| message_type        | SMALLINT     | No       | Message type                         |
| content             | TEXT         | Yes      | Message content                      |
| media_url           | TEXT         | Yes      | File path stored in Supabase Storage |
| media_size          | BIGINT       | Yes      | File size in bytes                   |
| media_mime_type     | VARCHAR(100) | Yes      | MIME type                            |
| reply_to_message_id | UUID         | Yes      | References messages(id)              |
| edited_at           | TIMESTAMPTZ  | Yes      | Last edit timestamp                  |
| created_at          | TIMESTAMPTZ  | No       | Created date                         |
| updated_at          | TIMESTAMPTZ  | Yes      | Updated date                         |
| created_by_user_id  | UUID         | Yes      | Audit                                |
| updated_by_user_id  | UUID         | Yes      | Audit                                |

---

# Indexes

- PK(id)
- INDEX(conversation_id)
- INDEX(sender_user_id)
- INDEX(created_at)

---

# Foreign Keys

| Column              | References        |
| ------------------- | ----------------- |
| conversation_id     | conversations(id) |
| sender_user_id      | users(id)         |
| reply_to_message_id | messages(id)      |

---

# Relationships

## Belongs To

- conversations
- users

## Self Reference

- reply_to_message_id → messages(id)

---

# Message Types

| Value | Name     |
| ----- | -------- |
| 0     | Text     |
| 1     | Image    |
| 2     | Video    |
| 3     | File     |
| 4     | Location |
| 5     | System   |

---

# Business Rules

- Only active conversation members can send messages.
- Messages cannot be sent to closed conversations.
- Every message belongs to exactly one conversation.
- Only the sender can edit a message.
- Messages are never physically deleted.
- Deleting a message removes its content and media information while preserving the database record.
- Deleted messages are displayed as a placeholder in the client application.
- Reply messages must always reference another message within the same conversation.
- Media files are stored in Supabase Storage. Only metadata and storage paths are stored in PostgreSQL.
- System messages are created only by backend business logic.

---

# Lifecycle

### Send Message

- Validate conversation membership.
- Validate conversation status.
- Validate message type.
- Upload media to Supabase Storage if necessary.
- Store the message.
- Broadcast the message to conversation members.

### Edit Message

- Validate sender ownership.
- Update message content.
- Update `edited_at`.

### Delete Message

- Validate sender ownership.
- Clear `content`.
- Clear `media_url`.
- Clear `media_size`.
- Clear `media_mime_type`.
- Convert the message into a deleted placeholder.
- Preserve reply chains and message ordering.

---

# Performance Notes

Most queries retrieve messages using:

- conversation_id
- created_at

Messages should always be ordered by `created_at ASC`.

Large conversations should use cursor-based pagination instead of OFFSET pagination.

---

# Future Extensions

Possible future additions:

- Emoji reactions
- Voice messages
- Read receipts
- Typing indicators
- Message forwarding
- Message pinning
- Poll messages
- AI message summaries
- Message translation

---

# Notes

This table stores only message metadata.

Binary files must never be stored inside PostgreSQL.

All uploaded files are stored in Supabase Storage.

Authorization is handled through the `conversation_members` table.

Deleting a message never removes the database record. This preserves conversation history, reply relationships and chronological message ordering.
