# 05 — Reviews

Table: `Reviews`. Domain: `src/Domain/Reviews/Review.cs`. Spec: `docs/database/15-reviews.md`.

Depends on: [03-events.md](03-events.md) (event Completed + `EventParticipant.CanReview` / Attended).

---

## Progress

- [ ] Create / update review
- [ ] List reviews for user / event
- [ ] Sync profile + statistics rating caches
- [ ] Report linkage (mark reported via Moderation)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `ReviewsController` | `/api/reviews` |

---

## Features

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `CreateReview` | Command | `POST /api/reviews` | `Review.Create`. App must enforce: same completed event, both Attended, no self-review, unique `(event, reviewer, reviewed)`. |
| [ ] | `UpdateReview` | Command | `PUT /api/reviews/{id}` | Reviewer only; recalculate caches. |
| [ ] | `GetReviewById` | Query | `GET /api/reviews/{id}` | Hide if reported pending moderation (product rule). |
| [ ] | `ListReviewsForUser` | Query | `GET /api/users/{userId}/reviews` | Received reviews; paginated. |
| [ ] | `ListReviewsForEvent` | Query | `GET /api/events/{eventId}/reviews` | Paginated. |
| [ ] | `ListReviewablePeers` | Query | `GET /api/events/{eventId}/reviewable` | Attended peers not yet reviewed by me. |

---

## Side effects (same UoW)

1. Recompute / update `Profile.UpdateCachedRating` for reviewed user.
2. Update `UserStatistics` (`UpdateAverageRating`, `Increase*` review counters as per domain helpers).
3. Optional badge hook `FIRST_REVIEW` ([08-gamification.md](08-gamification.md)).
4. On moderation report: `Review.MarkAsReported`.

---

## Exit criteria

- [ ] Cannot review without Attended eligibility
- [ ] Unique constraint failures mapped to Conflict
- [ ] Rating caches update after create/update
