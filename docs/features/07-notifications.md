# 07 — Notifications

Tables: `Notifications`, `NotificationSettings`.

Domain: `src/Domain/Notifications/*`. Specs: `docs/database/21`–`22`.

Depends on: Identity (settings seed). Most **creates** are side effects from Events / Social / Messaging / Gamification.

---

## Progress

- [x] Inbox list / mark read / delete
- [x] Settings get / update
- [x] Delivery helper used by other modules (`INotificationPublisher`)
- [ ] Push/email workers deferred to jobs

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `NotificationsController` | `/api/notifications` |
| `NotificationSettingsController` | `/api/notification-settings` |

---

## Features

### Inbox

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListMyNotifications` | Query | `GET /api/notifications` | Cursor; filter unread optional. Recipient = current user only. |
| [x] | `MarkNotificationRead` | Command | `POST /api/notifications/{id}/read` | `MarkAsRead`. |
| [x] | `MarkAllNotificationsRead` | Command | `POST /api/notifications/read-all` | Batch. |
| [x] | `MarkNotificationUnread` | Command | `POST /api/notifications/{id}/unread` | Optional. |
| [x] | `DeleteNotification` | Command | `DELETE /api/notifications/{id}` | Physical delete OK (no soft delete). |

### Settings

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `GetMyNotificationSettings` | Query | `GET /api/notification-settings` | One row per `NotificationType`; backfills missing types. |
| [x] | `UpdateNotificationSetting` | Command | `PUT /api/notification-settings/{type}` | Channel flags via `UpdateChannels`. |

Settings rows are created at user activation ([01-identity.md](01-identity.md)).

---

## Publisher contract (Application)

`INotificationPublisher` + `InAppNotificationPublisher` (Infrastructure). Callers own `SaveChanges`.

Pipeline:

1. Skip if recipient == actor (no self-notify).
2. Load `NotificationSetting` for `(user, type)`.
3. If in-app allowed → `Notification.Create` (no save).
4. If push/email allowed → no-op for v1 (jobs deferred).

Default channel matrix lives in `NotificationSetting.CreateDefault`.

### Types that producers emit

| Type | Typical producer |
| ---- | ---------------- |
| FriendRequest / FriendAccepted | Social |
| EventRequestApproved / Rejected / Cancelled / Reminder / Invitation | Events (+ jobs for reminder) |
| PostLiked / PostCommented / CommentReplied | Social |
| BadgeEarned | Gamification |
| NewMessage | Messaging |
| System | Platform |

---

## Exit criteria

- [x] User can list and mark notifications
- [x] Settings readable/updatable
- [x] At least one producer module uses the shared publisher
- [x] Push/email can be no-op stubs without blocking MVP
