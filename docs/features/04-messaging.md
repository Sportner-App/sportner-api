# 04 — Messaging (Event chat)

Tables: `Conversations`, `ConversationMembers`, `Messages`.

Domain: `src/Domain/Messaging/*`. Specs: `docs/database/12`–`14`.

Depends on: [03-events.md](03-events.md) (conversation created on publish).

**v1 scope:** `ConversationType.Event` only. Direct / Group = Future. REST first; SignalR in Phase 8 ([10-cross-cutting.md](10-cross-cutting.md)).

---

## Progress

- [ ] Get conversation by event / list my conversations
- [ ] List messages (cursor)
- [ ] Send / edit / redact text (and media)
- [ ] Membership stays event-driven (no free-form invite in v1)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `ConversationsController` | `/api/conversations` |
| `MessagesController` | `/api/conversations/{conversationId}/messages` |

---

## Features

### Conversations

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `GetConversationByEvent` | Query | `GET /api/events/{eventId}/conversation` | Active members only. |
| [ ] | `GetConversationById` | Query | `GET /api/conversations/{id}` | Must be active member. |
| [ ] | `ListMyConversations` | Query | `GET /api/conversations` | Event chats for current user; paginated. |
| [ ] | `CloseConversation` | Command | `POST /api/conversations/{id}/close` | System/organizer after cancel/complete — often invoked from Events handler, not public. |

Membership add/remove/promote is **orchestrated from Events**, not exposed as open social DM APIs in v1.

### Messages

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ListMessages` | Query | `GET .../messages` | Cursor pagination; member only. |
| [ ] | `SendTextMessage` | Command | `POST .../messages` | `Message.CreateText` after `Conversation.CanUserSendMessage`. |
| [ ] | `SendMediaMessage` | Command | `POST .../messages/media` | Upload via storage → `CreateMedia`. |
| [ ] | `EditMessage` | Command | `PUT .../messages/{id}` | Sender only; text only. |
| [ ] | `RedactMessage` | Command | `DELETE .../messages/{id}` | Soft clear content (domain `Redact`); no physical delete. |
| [ ] | `CreateSystemMessage` | Command | (internal) | Backend-only; e.g. “Event cancelled”. |

**Deferred:** `MessageType.Location` factory; Direct/Group create; typing indicators; realtime push.

---

## Rules

- Closed conversation → no send / membership changes.
- Pending participants / waitlist are not members (Events sync).
- Notify `NewMessage` respecting settings (batching later).

---

## Exit criteria

- [ ] Approved participant can list and send in event chat
- [ ] Non-members receive Forbidden
- [ ] Cursor pagination stable
- [ ] Redact leaves row with cleared content
