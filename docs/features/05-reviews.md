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
| [x] | `CreateReview` | Command | `POST /api/reviews` | Event Completed; both Attended; reviewer `CanReview`; no self-review; unique `(event, reviewer, reviewed)` → 409. Syncs rating caches; awards `FIRST_REVIEW` once. |
| [x] | `UpdateReview` | Command | `PUT /api/reviews/{id}` | Reviewer only; recalculates caches. |
| [x] | `GetReviewById` | Query | `GET /api/reviews/{id}` | Reported reviews hidden (404) except to the original reviewer. |
| [x] | `ListReviewsForUser` | Query | `GET /api/users/{userId}/reviews` | Received, non-reported; paginated. |
| [x] | `ListReviewsForEvent` | Query | `GET /api/events/{eventId}/reviews` | Non-reported; paginated. |
| [x] | `ListReviewablePeers` | Query | `GET /api/events/{eventId}/reviewable` | Attended peers not yet reviewed by me. |

---

## Side effects (same feature / UoW)

1. `Profile.UpdateCachedRating` + `UserStatistics.UpdateAverageRating` via `ReviewRatingSync`.
2. `UserStatistics.IncreaseReviewCount` on create (received count).
3. Optional `FIRST_REVIEW` badge + `IncreaseBadgesCount` for the reviewer.
4. `Review.MarkAsReported` — deferred to Moderation.

---

## Exit criteria

- [x] Cannot review without Attended eligibility
- [x] Unique constraint failures mapped to Conflict (pre-check)
- [x] Rating caches update after create/update
