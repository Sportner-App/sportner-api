# 05 — Reviews

Table: `Reviews`. Domain: `src/Domain/Reviews/Review.cs`. Spec: `docs/database/15-reviews.md`.

Depends on: [03-events.md](03-events.md) (event Completed + `EventParticipant.CanReview` / Attended).

---

## Progress

- [x] Create / update review
- [x] List reviews for user / event
- [x] Sync profile + statistics rating caches
- [~] Report linkage (mark reported via Moderation) — deferred to [09-moderation.md](09-moderation.md)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `ReviewsController` | `/api/reviews` |
| `UserReviewsController` | `/api/users/{userId}/reviews` |
| `EventsController` (nested) | `/api/events/{eventId}/reviews`, `/reviewable` |

---

## Features

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `CreateReview` | Command | `POST /api/reviews` | Event Completed; reviewer is organizer **or** Attended+`CanReview`; reviewed is organizer **or** Attended; no self-review; unique `(event, reviewer, reviewed)` → 409. Syncs rating caches; awards `FIRST_REVIEW` once. |
| [x] | `UpdateReview` | Command | `PUT /api/reviews/{id}` | Reviewer only; recalculates caches. |
| [x] | `GetReviewById` | Query | `GET /api/reviews/{id}` | Reported reviews hidden (404) except to the original reviewer. |
| [x] | `ListReviewsForUser` | Query | `GET /api/users/{userId}/reviews` | Received, non-reported; paginated. |
| [x] | `ListReviewsForEvent` | Query | `GET /api/events/{eventId}/reviews` | Non-reported; paginated. |
| [x] | `ListReviewablePeers` | Query | `GET /api/events/{eventId}/reviewable` | Organizer or Attended reviewer; Attended peers (plus organizer for attendees) not yet reviewed by me. |
| [x] | `ListPendingAttendanceEvents` | Query | `GET /api/events/mine/pending-attendance` | Events I organized that are Completed but still have Approved (attendance-unconfirmed) participants — nobody there can review anyone yet. Drives the app-launch prompt. |
| [x] | `ConfirmAllAttendance` | Command | `POST /api/events/{eventId}/attendance/confirm-all` | Organizer's one-tap close-out: every still-Approved participant → Attended, except any listed in `absentUserIds` (→ No-Show instead). Requires the event already be Completed. |

---

## Side effects (same feature / UoW)

1. `Profile.UpdateCachedRating` + `UserStatistics.UpdateAverageRating` via `ReviewRatingSync`.
2. `UserStatistics.IncreaseReviewCount` on create (received count).
3. Optional `FIRST_REVIEW` badge + `IncreaseBadgesCount` for the reviewer.
4. `Review.MarkAsReported` — deferred to Moderation.
5. `NotificationType.EventReviewPrompt` prompts each newly review-eligible user to go rate their teammates — organizer at event completion, each participant at their own attendance confirmation (see [07-notifications.md](07-notifications.md)). The app also surfaces a prominent in-app CTA on the event-detail screen for attended participants once the event is completed.
6. **Attendance safety net** — an organizer who never opens the app to take attendance would otherwise block reviews for everyone in that event forever. Two mechanisms cover this: (a) `ListPendingAttendanceEvents` + `ConfirmAllAttendance` back an app-launch prompt (dismissible) that lets the organizer close out attendance in one tap; (b) `AttendanceAutoConfirmDispatcher` (hourly cron, `BackgroundJobsOptions.AttendanceAutoConfirmCron`) auto-confirms every still-Approved participant `AttendanceAutoConfirmGraceDays` (default 3) after the event's scheduled end, regardless of whether the organizer ever returns. Note: `Event.Create` auto-enrolls the organizer as an Approved participant of their own event, so the organizer's own attendance is swept by these same mechanisms.

---

## Exit criteria

- [x] Cannot review without Attended eligibility
- [x] Unique constraint failures mapped to Conflict (pre-check)
- [x] Rating caches update after create/update
