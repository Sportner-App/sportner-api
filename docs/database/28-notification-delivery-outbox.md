# Notification Delivery Outbox

## Module

Notifications

## Purpose

Durable queue for push (and later email) delivery. Written in the same UoW as in-app
`Notifications` by `INotificationPublisher`; drained by `Notifications.Worker`.

## Columns

| Column | Type | Nullable | Description |
| ------ | ---- | -------- | ----------- |
| id | UUID | No | PK |
| recipient_user_id | UUID | No | FK → Users |
| notification_id | UUID | Yes | FK → Notifications (SetNull on delete) |
| channel | SMALLINT | No | Push / Email |
| status | SMALLINT | No | Pending / Sent / Failed / Cancelled |
| notification_type | SMALLINT | No | Same as Notifications |
| entity_type | SMALLINT | No | Deep-link entity family |
| entity_id | UUID | Yes | Deep-link id |
| title | TEXT | No | Push/email title |
| body | TEXT | No | Push/email body |
| attempt_count | INT | No | Retry counter |
| next_attempt_at | TIMESTAMPTZ | Yes | Due time for pending rows |
| sent_at | TIMESTAMPTZ | Yes | When marked Sent |
| last_error | TEXT | Yes | Last provider / pipeline error |
| created_at / updated_at / audit | | | Standard |

## Indexes

- `(status, next_attempt_at)` — worker poll
- `recipient_user_id`
- `notification_id`
- `created_at`

## Rules

- Enqueue only when `NotificationSetting.PushEnabled` (or type default) is true.
- Email channel reserved; worker cancels Email rows until provider exists.
- Max 5 attempts with backoff; then `Failed`.
- No devices with token → `Cancelled` (no retry).
- Invalid provider token → clear `UserDevices.PushToken`.
