# 09 — Quests (Badge görevleri)

Tables: `Quests`, `UserQuests`. Specs: `docs/database/29`–`30`.

Depends on: [08-gamification.md](08-gamification.md). Progress via `IQuestProgressTracker`; rewards via `IBadgeAwarder`.

---

## Progress

- [x] Quest / UserQuest domain + migration
- [x] Seed 5 evergreen quests
- [x] Progress tracker + auto-complete
- [x] List catalog / my quests
- [x] Hooks: attend, post, friend, review, host complete
- [ ] Seasonal windows (V2.1)
- [ ] Claim endpoint (not planned — auto-complete)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `QuestsController` | `/api/quests` |

---

## Features

| Status | Use case | Type | Endpoint | Notes |
| ------ | -------- | ---- | -------- | ----- |
| [x] | `ListQuests` | Query | `GET /api/quests` | Active catalog; joins viewer progress when authenticated. |
| [x] | `ListMyQuests` | Query | `GET /api/quests/me` | Started/completed rows for current user. |
| [x] | `IQuestProgressTracker.ReportAsync` | Internal | — | Metric delta → progress → auto-complete → badge + `QuestCompleted` notify. |

### Seed quests

| Code | Metric | Target | Reward badge |
| ---- | ------ | ------ | ------------ |
| `Q_ATTEND_3` | events_attended | 3 | FIRST_EVENT |
| `Q_POST_5` | posts_created | 5 | FIRST_POST |
| `Q_MAKE_FRIENDS_5` | friends_accepted | 5 | FIRST_FRIEND |
| `Q_HOST_1` | events_organized_completed | 1 | HOST_HERO |
| `Q_REVIEW_3` | reviews_created | 3 | FIRST_REVIEW |

### Hooks

| Metric | Handler |
| ------ | ------- |
| events_attended | `ConfirmAttendance` |
| posts_created | `CreatePost` |
| friends_accepted | `AcceptFriendRequest` (both users) |
| events_organized_completed | `CompleteEvent` |
| reviews_created | `CreateReview` |

---

## Exit criteria

- [x] List + progress readable
- [x] ≥3 metric hooks
- [x] Complete → badge award idempotent
- [x] Unique `(UserId, QuestId)`
