# Database Reference

This document contains all shared reference data used throughout the database.

Unless explicitly stated otherwise, all enum values are stored as `SMALLINT` in PostgreSQL.

---

# User Status

| Value | Name                |
| ----: | ------------------- |
|     0 | PendingVerification |
|     1 | Active              |
|     2 | Suspended           |
|     3 | Banned              |
|     4 | Deleted             |

---

# Skill Level

| Value | Name         |
| ----: | ------------ |
|     0 | Beginner     |
|     1 | Intermediate |
|     2 | Advanced     |
|     3 | Expert       |
|     4 | Professional |

---

# Device Platform

| Value | Name    |
| ----: | ------- |
|     0 | iOS     |
|     1 | Android |

---

# Event Status

| Value | Name      |
| ----: | --------- |
|     0 | Draft     |
|     1 | Published |
|     2 | Full      |
|     3 | Completed |
|     4 | Cancelled |

---

# Participant Status

| Value | Name      |
| ----: | --------- |
|     0 | Pending   |
|     1 | Approved  |
|     2 | Rejected  |
|     3 | Cancelled |
|     4 | Attended  |
|     5 | NoShow    |

---

# Participant Kind

| Value | Name       |
| ----: | ---------- |
|     0 | Registered |
|     1 | Guest      |

---

# Conversation Type

| Value | Name   |
| ----: | ------ |
|     0 | Event  |
|     1 | Direct |
|     2 | Group  |

---

# Conversation Member Role

| Value | Name      |
| ----: | --------- |
|     0 | Member    |
|     1 | Owner     |
|     2 | Moderator |

---

# Message Type

| Value | Name     |
| ----: | -------- |
|     0 | Text     |
|     1 | Image    |
|     2 | Video    |
|     3 | File     |
|     4 | Location |
|     5 | System   |

---

# Notification Type

| Value | Name                 |
| ----: | -------------------- |
|     0 | FriendRequest        |
|     1 | FriendAccepted       |
|     2 | EventInvitation      |
|     3 | EventRequestApproved |
|     4 | EventRequestRejected |
|     5 | EventReminder        |
|     6 | EventCancelled       |
|     7 | PostLiked            |
|     8 | PostCommented        |
|     9 | CommentReplied       |
|    10 | BadgeEarned          |
|    11 | NewMessage           |
|    12 | System               |

---

# Notification Entity Type

| Value | Name         |
| ----: | ------------ |
|     0 | User         |
|     1 | Event        |
|     2 | Post         |
|     3 | Comment      |
|     4 | Conversation |
|     5 | Badge        |

---

# Badge Category

| Value | Name        |
| ----: | ----------- |
|     0 | Sports      |
|     1 | Events      |
|     2 | Social      |
|     3 | Community   |
|     4 | Streak      |
|     5 | Achievement |
|     6 | Special     |

---

# Badge Rarity

| Value | Name      |
| ----: | --------- |
|     0 | Common    |
|     1 | Rare      |
|     2 | Epic      |
|     3 | Legendary |

---

# Media Type

| Value | Name  |
| ----: | ----- |
|     0 | Image |
|     1 | Video |

---

# Report Entity Type

| Value | Name    |
| ----: | ------- |
|     0 | User    |
|     1 | Event   |
|     2 | Post    |
|     3 | Comment |
|     4 | Review  |
|     5 | Message |

---

# Report Status

| Value | Name        |
| ----: | ----------- |
|     0 | Pending     |
|     1 | UnderReview |
|     2 | Resolved    |
|     3 | Rejected    |

---

# Friendship Status

| Value | Name     |
| ----: | -------- |
|     0 | Pending  |
|     1 | Accepted |
|     2 | Rejected |
|     3 | Blocked  |

---

# Report Reason Codes

| Code                  | Description                    |
| --------------------- | ------------------------------ |
| SPAM                  | Spam content                   |
| HARASSMENT            | Harassment or bullying         |
| HATE_SPEECH           | Hate speech                    |
| INAPPROPRIATE_CONTENT | Inappropriate content          |
| VIOLENCE              | Violent content                |
| NUDITY                | Nudity or sexual content       |
| FAKE_INFORMATION      | Fake or misleading information |
| IMPERSONATION         | Fake account or impersonation  |
| SCAM                  | Scam or fraud                  |
| OTHER                 | Other                          |

---

# Badge Codes

| Code             | Description                     |
| ---------------- | ------------------------------- |
| FIRST_EVENT      | First completed event           |
| FIRST_POST       | First shared post               |
| FIRST_FRIEND     | First accepted friendship       |
| FIRST_REVIEW     | First received review           |
| COMMUNITY_HELPER | Community contribution          |
| SPORTS_EXPLORER  | Participated in multiple sports |
| EVENT_MASTER     | Organized many events           |
| MARATHON_RUNNER  | High participation achievement  |

---

# General Rules

- All enums are represented as `SMALLINT`.
- Enum values must never be reordered after production release.
- New enum values should always be appended to the end.
- Backend and frontend must use the same enum definitions.
- Display names may be localized without changing enum values.
- Business logic must never rely on localized strings.
- Permanent identifiers should always use enum values or predefined codes.

---

# Naming Conventions

## Tables

EF Core convention using plural `DbSet` names

Example

Users

EventParticipants

PostComments

---

## Columns

Entity property names

Example

CreatedAt

UserId

EventId

---

## Foreign Keys

<Entity>Id

Example

UserId

BadgeId

ConversationId

---

## Primary Keys

UUID

---

## Date Types

TIMESTAMPTZ

---

## File Storage

All files are stored in Supabase Storage.

The database stores only metadata and storage paths.

Binary files must never be stored in PostgreSQL.

---

# Architecture Notes

- Clean Architecture
- Domain Driven Design (DDD)
- CQRS
- MediatR
- Entity Framework Core
- Fluent API Configuration
- Mapster
- FluentValidation
- Serilog
- PostgreSQL
- Supabase Storage

---

# Important Rules

- Never use soft delete.
- Use status fields where applicable.
- Store Refresh Tokens only in `user_sessions`.
- Store media only in Supabase Storage.
- Keep cached counters synchronized by backend business logic.
- Use UUID as the primary key for every table.
- Store timestamps as `TIMESTAMPTZ`.
- Protect aggregate boundaries through backend business logic.
