# 08 — Gamification (Badges)

Tables: `Badges`, `UserBadges`.

Domain: `src/Domain/Badges/*`. Specs: `docs/database/23`–`24`. Constants: `BadgeCodes`, `BadgeThresholds`.

Depends on: seed ([00-prerequisites.md](00-prerequisites.md)). Award hooks fire from Identity/Events/Social/Reviews.

---

## Progress

- [x] List badge catalog (+ category / earned filters)
- [x] List my / user badges (showcase fields)
- [x] My badge progress (`current` / `target` / `percent`)
- [x] Set showcased badges (max 3)
- [x] Award service + hooks for FIRST_* + threshold + V2 +4 codes
- [x] Advanced badge thresholds (`BadgeThresholds` + evaluate hooks)
- [ ] Admin badge CRUD (optional; seed-first)
- [ ] Secret badges (V2.1)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `BadgesController` | `/api/badges` |
| `UserBadgesController` | `/api/users/{userId}/badges` |

---

## Features

### Read APIs

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListBadges` | Query | `GET /api/badges?category=&earned=` | Active definitions; anonymous OK. `earned` requires auth. When auth’d, items include `earned`. |
| [x] | `ListMyBadges` | Query | `GET /api/badges/me` | Joined with definition; showcase first. |
| [x] | `GetMyBadgeProgress` | Query | `GET /api/badges/me/progress` | Private progress read-model. |
| [x] | `ListUserBadges` | Query | `GET /api/users/{userId}/badges` | Public profile; includes `isShowcased` / `showcaseOrder`. |

### Commands

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `SetShowcasedBadges` | Command | `PUT /api/badges/me/showcase` | Body `{ "badgeIds": [...] }` max 3, owned only; replaces previous showcase set. |

### Award (internal)

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `IBadgeAwarder.TryAwardAsync` | Domain service | **Not public** | `Badge.IsEarnable` → `UserBadge.Award`; unique `(user, badge)`; `IncreaseBadgesCount`; notify `BadgeEarned`. |

### Hook points

| Code | When | Status |
| ---- | ---- | ------ |
| `FIRST_EVENT` | After `ConfirmAttendance` | [x] |
| `FIRST_POST` | After first successful `CreatePost` | [x] |
| `FIRST_FRIEND` | After first `AcceptFriendRequest` (both users) | [x] |
| `FIRST_REVIEW` | After first `CreateReview` | [x] |
| `SPORTS_EXPLORER` | ≥3 UserSports **veya** ≥3 distinct attended sports | [x] |
| `EVENT_MASTER` | ≥10 Attended | [x] |
| `MARATHON_RUNNER` | 4 consecutive ISO weeks with ≥1 attended | [x] (hook + Events.Worker sweep) |
| `COMMUNITY_HELPER` | ≥5 resolved reports as reporter **veya** ≥20 comments | [x] |
| `SOCIAL_BUTTERFLY` | ≥20 accepted friends | [x] |
| `HOST_HERO` | ≥5 organized events completed | [x] |
| `REVIEW_GURU` | ≥10 reviews written | [x] |
| `EARLY_BIRD` | ≥5 attended events with start hour &lt; 09:00 UTC | [x] |

### Admin (optional v1)

| Status | Use case | Type | Endpoint | Notes |
| ------ | -------- | ---- | -------- | ----- |
| [ ] | Create/Update/Deactivate badge | Commands | `/api/admin/badges` | Prefer seed; deactivate instead of delete. |

---

## Exit criteria

- [x] Catalog + earned badges readable
- [x] Progress + showcase (V2/05)
- [x] At least FIRST_* awards work idempotently
- [x] Duplicate award attempts are no-ops / Conflict handled cleanly
