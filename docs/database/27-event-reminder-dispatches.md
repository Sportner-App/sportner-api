# Event Reminder Dispatches

## Module

Events / Notifications

## Purpose

Idempotency log for scheduled event reminders (`NotificationType.EventReminder`).  
One row per `(EventId, UserId, WindowMinutes)` so the Worker does not re-send the same window.

## Columns

| Column | Type | Nullable | Description |
| ------ | ---- | -------- | ----------- |
| id | UUID | No | PK |
| event_id | UUID | No | FK → Events |
| user_id | UUID | No | FK → Users (approved participant) |
| window_minutes | INT | No | Minutes before start (1440 / 60) |
| sent_at | TIMESTAMPTZ | No | When the reminder was attempted |
| created_at / updated_at / audit | | | Standard |

## Indexes

- UNIQUE(event_id, user_id, window_minutes)

## Notes

- Written only by background jobs (`IEventReminderDispatcher`).
- Organizer is never reminded (participants only).
