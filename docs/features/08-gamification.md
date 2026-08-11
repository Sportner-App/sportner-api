# 08 — Gamification (Badges)

Tables: `Badges`, `UserBadges`.

Domain: `src/Domain/Badges/*`. Specs: `docs/database/23`–`24`. Constants: `BadgeCodes`.

Depends on: seed ([00-prerequisites.md](00-prerequisites.md)). Award hooks fire from Identity/Events/Social/Reviews.

---

## Progress

- [x] List badge catalog
- [x] List my / user badges
- [x] Award service + hooks for FIRST_* codes
- [x] Advanced badge thresholds (`BadgeThresholds` + evaluate hooks)
- [ ] Admin badge CRUD (optional; seed-first)

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
| [x] | `ListBadges` | Query | `GET /api/badges` | Active definitions; anonymous OK. |
| [x] | `ListMyBadges` | Query | `GET /api/badges/me` | Joined with definition. |
| [x] | `ListUserBadges` | Query | `GET /api/users/{userId}/badges` | Public profile section. |

### Award (internal)

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `IBadgeAwarder.TryAwardAsync` | Domain service | **Not public** | `Badge.IsEarnable` → `UserBadge.Award`; unique `(user, badge)`; `IncreaseBadgesCount`; notify `BadgeEarned`. |

### Hook points (implement with producers)

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

### Admin (optional v1)

| Status | Use case | Type | Endpoint | Notes |
| ------ | -------- | ---- | -------- | ----- |
| [ ] | Create/Update/Deactivate badge | Commands | `/api/admin/badges` | Prefer seed; deactivate instead of delete. |

---

## Exit criteria

- [x] Catalog + earned badges readable
- [x] At least FIRST_* awards work idempotently
- [x] Duplicate award attempts are no-ops / Conflict handled cleanly
