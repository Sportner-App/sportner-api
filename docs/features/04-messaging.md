# 04 — Messaging (Event chat)

Tables: `Conversations`, `ConversationMembers`, `Messages`.

Domain: `src/Domain/Messaging/*`. Specs: `docs/database/12`–`14`.

Depends on: [03-events.md](03-events.md) (conversation created on publish).

**v1 scope:** `ConversationType.Event` only. Direct / Group = Future. REST first; SignalR in Phase 8 ([10-cross-cutting.md](10-cross-cutting.md)).

---

## Progress

- [x] Get conversation by event / list my conversations
- [x] List messages (cursor)
- [x] Send / edit / redact text (and media)
- [x] Membership stays event-driven (no free-form invite in v1)

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
| [x] | `GetConversationByEvent` | Query | `GET /api/events/{eventId}/conversation` | Active members only → 403 otherwise. |
| [x] | `GetConversationById` | Query | `GET /api/conversations/{id}` | Must be active member. |
| [x] | `ListMyConversations` | Query | `GET /api/conversations` | Event chats only; offset pagination; last message preview. |
| [~] | `CloseConversation` | Command | — | **Not public.** Invoked from Events Cancel/Complete via `EventAccess.CloseEventConversationAsync`. |

Membership add/remove remains orchestrated from Events.

### Messages

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListMessages` | Query | `GET .../messages?before=&limit=` | Cursor keyset `(CreatedAt, Id)`; chronological page; member only. |
| [x] | `SendTextMessage` | Command | `POST .../messages` | `Message.CreateText` after membership + open checks. Notifies other members (`NewMessage`). |
| [x] | `SendMediaMessage` | Command | `POST .../messages/media` | Multipart → `chat-media` bucket → `CreateMedia` (image/video/pdf). |
| [x] | `EditMessage` | Command | `PUT .../messages/{id}` | Sender only; text only. |
| [x] | `RedactMessage` | Command | `DELETE .../messages/{id}` | Soft clear (`Redact`). Sender or owner/moderator. |
| [~] | `CreateSystemMessage` | Command | (internal) | Deferred until Events needs system lines in chat. |

**Deferred:** `MessageType.Location`; Direct/Group create; typing indicators; realtime push.

---

## Rules

- Closed conversation → no send.
- Pending participants / waitlist are not members (Events sync).
- `NewMessage` notifications respect in-app settings via `INotificationPublisher`.

---

## Exit criteria

- [x] Approved participant can list and send in event chat
- [x] Non-members receive Forbidden
- [x] Cursor pagination stable
- [x] Redact leaves row with cleared content
