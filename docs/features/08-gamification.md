# 08 — Gamification (Badges)

Tables: `Badges`, `UserBadges`.

Domain: `src/Domain/Badges/*`. Specs: `docs/database/23`–`24`. Constants: `BadgeCodes`.

Depends on: seed ([00-prerequisites.md](00-prerequisites.md)). Award hooks fire from Identity/Events/Social/Reviews.

---

## Progress

- [ ] List badge catalog
- [ ] List my / user badges
- [ ] Award service + hooks for FIRST_* codes
- [ ] Admin badge CRUD (optional; seed-first)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `BadgesController` | `/api/badges` |

---

## Features

### Read APIs

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ListBadges` | Query | `GET /api/badges` | Active definitions; cacheable. |
| [ ] | `ListMyBadges` | Query | `GET /api/badges/me` | Joined with definition. |
| [ ] | `ListUserBadges` | Query | `GET /api/users/{userId}/badges` | Public profile section. |

### Award (internal)

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `AwardBadge` | Command / domain service | **Not public** | `Badge.IsEarnable` → `UserBadge.Award`; unique `(user, badge)`; `IncreaseBadgesCount`; notify `BadgeEarned`. |

Suggested Application helper: `IBadgeAwarder.TryAwardAsync(userId, BadgeCodes.X)`.

### Hook points (implement with producers)

| Code | When |
| ---- | ---- |
| `FIRST_EVENT` | First Attended (or first completed participation — pick one rule and stick to it) |
| `FIRST_POST` | After first successful `CreatePost` |
| `FIRST_FRIEND` | After first `AcceptFriendRequest` |
| `FIRST_REVIEW` | After first `CreateReview` |
| `SPORTS_EXPLORER` | N distinct user sports or sports played in events |
| `EVENT_MASTER` | N attended events |
| `MARATHON_RUNNER` | Streak / volume rule |
| `COMMUNITY_HELPER` | Reports helpful / comments threshold — define explicitly before coding |

### Admin (optional v1)

| Status | Use case | Type | Endpoint | Notes |
| ------ | -------- | ---- | -------- | ----- |
| [ ] | Create/Update/Deactivate badge | Commands | `/api/admin/badges` | Prefer seed; deactivate instead of delete. |

---

## Exit criteria

- [ ] Catalog + earned badges readable
- [ ] At least FIRST_* awards work idempotently
- [ ] Duplicate award attempts are no-ops / Conflict handled cleanly
