# 04 — Messaging (Event + Direct + Group)

Tables: `Conversations`, `ConversationMembers`, `Messages`.

Domain: `src/Domain/Messaging/*`. Specs: `docs/database/12`–`14`.

Depends on: [03-events.md](03-events.md) (event conversation on publish); Friendship for Direct/Group.

**Scope:** Event (auto) + Direct + Group (max 50).  
**Realtime:** REST write; SignalR push on `conversation:{id}` (`/hubs/event-chat`).

---

## Progress

- [x] Get conversation by event / list my conversations
- [x] List messages (cursor)
- [x] Send / edit / redact text (and media)
- [x] Event membership stays event-driven
- [x] Direct create (friends only; idempotent)
- [x] Group create / invite / leave

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `ConversationsController` | `/api/conversations` |
| `MessagesController` | `/api/conversations/{conversationId}/messages` |
| `EventsController` (nested) | `GET /api/events/{eventId}/conversation` |

---

## Features

### Conversations

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `GetConversationByEvent` | Query | `GET /api/events/{eventId}/conversation` | Active members only. |
| [x] | `GetConversationById` | Query | `GET /api/conversations/{id}` | Active member. |
| [x] | `ListMyConversations` | Query | `GET /api/conversations?type=` | All types; optional `type` filter (0/1/2). |
| [x] | `CreateDirectConversation` | Command | `POST /api/conversations/direct` | Accepted friends only; returns existing DM if present. |
| [x] | `CreateGroupConversation` | Command | `POST /api/conversations/groups` | Title required; members must be friends; max 50. |
| [x] | `InviteConversationMember` | Command | `POST /api/conversations/{id}/members` | Group only; owner/moderator + friend. |
| [x] | `LeaveConversation` | Command | `POST /api/conversations/{id}/leave` | Direct/Group; owner cannot leave. |
| [~] | `CloseConversation` | Command | — | Event path via `EventAccess.CloseEventConversationAsync`. |

### Messages

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListMessages` | Query | `GET .../messages?before=&limit=` | Cursor; member only. |
| [x] | `SendTextMessage` | Command | `POST .../messages` | + realtime `MessageCreated`. |
| [x] | `SendMediaMessage` | Command | `POST .../messages/media` | + realtime. |
| [x] | `EditMessage` | Command | `PUT .../messages/{id}` | + realtime. |
| [x] | `RedactMessage` | Command | `DELETE .../messages/{id}` | + realtime. |

**Deferred:** `MessageType.Location`; typing indicators.

**Realtime:** `/hubs/event-chat?access_token={jwt}` → `JoinConversation(id)` → `MessageCreated` / `Edited` / `Redacted`.

---

## Rules

- Closed conversation → no send.
- Direct: fixed 2 members; no invite.
- Group: max `Conversation.MaxGroupMembers` (50); invite = owner/moderator.
- Direct/Group create requires accepted friendship (not blocked).
- Event membership still orchestrated from Events.

---

## Exit criteria

- [x] Approved participant can list and send in event chat
- [x] Direct create + message via existing message APIs
- [x] Group create / invite / leave
- [x] Non-members Forbidden
- [x] Cursor pagination stable
- [x] Redact clears content
