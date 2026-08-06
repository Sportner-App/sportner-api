# 07 — Notifications

Tables: `Notifications`, `NotificationSettings`.

Domain: `src/Domain/Notifications/*`. Specs: `docs/database/21`–`22`.

Depends on: Identity (settings seed). Most **creates** are side effects from Events / Social / Messaging / Gamification.

---

## Progress

- [ ] Inbox list / mark read / delete
- [ ] Settings get / update
- [ ] Delivery helper used by other modules (`INotificationPublisher` or similar)
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
| [ ] | `ListMyNotifications` | Query | `GET /api/notifications` | Cursor; filter unread optional. Recipient = current user only. |
| [ ] | `MarkNotificationRead` | Command | `POST /api/notifications/{id}/read` | `MarkAsRead`. |
| [ ] | `MarkAllNotificationsRead` | Command | `POST /api/notifications/read-all` | Batch. |
| [ ] | `MarkNotificationUnread` | Command | `POST /api/notifications/{id}/unread` | Optional. |
| [ ] | `DeleteNotification` | Command | `DELETE /api/notifications/{id}` | Physical delete OK (no soft delete). |

### Settings

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `GetMyNotificationSettings` | Query | `GET /api/notification-settings` | One row per `NotificationType`. |
| [ ] | `UpdateNotificationSetting` | Command | `PUT /api/notification-settings/{type}` | Channel flags via `UpdateChannels` / enable-disable helpers. |

Settings rows are created at user activation ([01-identity.md](01-identity.md)).

---

## Publisher contract (Application)

Other modules must not sprinkle ad-hoc insert logic. Introduce something like:

```text
INotificationPublisher.PublishAsync(recipientId, type, title, body, entityType?, entityId?, actorUserId?)
```

Pipeline:

1. Skip if recipient == actor (no self-notify).
2. Load `NotificationSetting` for `(user, type)`.
3. If in-app allowed → `Notification.Create` + save.
4. If push/email allowed → enqueue delivery (Phase 9); v1 may persist in-app only.

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

- [ ] User can list and mark notifications
- [ ] Settings readable/updatable
- [ ] At least one producer module uses the shared publisher
- [ ] Push/email can be no-op stubs without blocking MVP
