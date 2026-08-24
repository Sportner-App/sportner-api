# 04 — Messaging (Event + Direct + Group)

Tables: `Conversations`, `ConversationMembers`, `Messages`.

Domain: `src/Domain/Messaging/*`. Specs: `docs/database/12`–`14`.

Depends on: [03-events.md](03-events.md) (event conversation on publish); Friendship for **Group** (Direct = stranger OK, block enforced).

**Scope:** Event (auto) + Direct + Group (max 50).  
**Realtime:** REST write; SignalR push on `conversation:{id}` (`/hubs/event-chat`).

---

## Progress

- [x] Get conversation by event / list my conversations
- [x] List messages (cursor)
- [x] Send / edit / redact text (and media)
- [x] Event membership stays event-driven
- [x] Direct create (**stranger OK**; block forbidden; idempotent)
- [x] Group create / invite / leave
- [x] Read receipts / mute / unread / search / typing (V2/02)

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
| [x] | `GetConversationById` | Query | `GET /api/conversations/{id}` | Active member; includes member last-read for receipts. |
| [x] | `ListMyConversations` | Query | `GET /api/conversations?type=` | unreadCount, isMuted, isFriend (direct), peer summary. |
| [x] | `SearchMyConversations` | Query | `GET /api/conversations/search?q=` | Title / peer username / first name. |
| [x] | `CreateDirectConversation` | Command | `POST /api/conversations/direct` | Friendship not required; blocked → 403; idempotent. |
| [x] | `CreateGroupConversation` | Command | `POST /api/conversations/groups` | Title required; members must be friends; max 50. |
| [x] | `InviteConversationMember` | Command | `POST /api/conversations/{id}/members` | Group only; owner/moderator + friend. |
| [x] | `LeaveConversation` | Command | `POST /api/conversations/{id}/leave` | Direct/Group; owner cannot leave. |
| [x] | `MarkConversationRead` | Command | `POST /api/conversations/{id}/read` | Body: `messageId`; forward-only cursor. |
| [x] | `MuteConversation` | Command | `POST /api/conversations/{id}/mute` | Optional `until`; default forever. Skips NewMessage notify. |
| [x] | `UnmuteConversation` | Command | `POST /api/conversations/{id}/unmute` | |
| [~] | `CloseConversation` | Command | — | Event path via `EventAccess.CloseEventConversationAsync`. |

### Messages

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListMessages` | Query | `GET .../messages?before=&limit=` | Cursor; member only. Sender snippet includes last name + profile image. |
| [x] | `SearchMessages` | Query | `GET .../messages/search?q=` | Content contains; member only. |
| [x] | `SendTextMessage` | Command | `POST .../messages` | + realtime `MessageCreated`; skip muted recipients. |
| [x] | `SendMediaMessage` | Command | `POST .../messages/media` | + realtime; skip muted recipients. |
| [x] | `EditMessage` | Command | `PUT .../messages/{id}` | + realtime; receipt unchanged. |
| [x] | `RedactMessage` | Command | `DELETE .../messages/{id}` | + realtime. |

**Deferred:** `MessageType.Location`; message-request inbox UI.

**Realtime:** `/hubs/event-chat?access_token={jwt}` → `JoinConversation(id)` → `MessageCreated` / `Edited` / `Redacted` / hub `Typing` → `UserTyping`.

---

## Rules

- Closed conversation → no send.
- Direct: fixed 2 members; no invite; **stranger allowed**; block either-way forbidden.
- Group: max `Conversation.MaxGroupMembers` (50); invite = owner/moderator; create still friends-only.
- Unread = others' messages after `LastReadAt` (cap 99).
- Event membership still orchestrated from Events.

---

## Exit criteria

- [x] Approved participant can list and send in event chat
- [x] Direct create + message via existing message APIs
- [x] Group create / invite / leave
- [x] Non-members Forbidden
- [x] Cursor pagination stable
- [x] Redact clears content
- [x] Read / mute / search / stranger DM (V2/02)
