# 05 — Badges depth

**Amaç:** Rozetleri “kazandın/kazanamadın”dan çıkarıp ilerleme, vitrin ve geniş katalog.  
**Bağımlılık:** V1 Gamification (`docs/features/08-gamification.md`). Quest’ler 06’da.  
**Durum:** **Done** (2026-08-14).

---

## V1 baseline

- Badge catalog + my/user badges
- `IBadgeAwarder` + FIRST_* ve eşik rozetleri
- Marathon sweep worker
- Admin CRUD yok (seed)

---

## V2 kapsamı (shipped)

| Madde | Durum |
| ----- | ----- |
| Progress API | `GET /api/badges/me/progress` — `current` / `target` / `percent` |
| Showcase | `UserBadge.IsShowcased` + `ShowcaseOrder` (max 3); `PUT /api/badges/me/showcase` |
| Catalog UX | `GET /api/badges?category=&earned=` (+ `earned` auth gerektirir) |
| +4 kod | `SOCIAL_BUTTERFLY`, `HOST_HERO`, `REVIEW_GURU`, `EARLY_BIRD` |
| Secret | V2.1 — `IsSecret` yok |

---

## Yeni badge kuralları

| Code | Kural | Hook |
| ---- | ----- | ---- |
| `SOCIAL_BUTTERFLY` | ≥ 20 accepted friends | `EvaluateAfterFriendshipAcceptedAsync` |
| `HOST_HERO` | ≥ 5 organized events `Completed` | `EvaluateAfterEventCompletedAsync` |
| `REVIEW_GURU` | ≥ 10 reviews written | `EvaluateAfterReviewCreatedAsync` |
| `EARLY_BIRD` | ≥ 5 attended with `EventDate.Hour < 9` (UTC) | `EvaluateAfterAttendanceAsync` |

---

## Migration

`AddUserBadgeShowcase` — `UserBadges.IsShowcased`, `ShowcaseOrder`.

Seed: `DatabaseSeeder` eksik kodları ekler (restart/seed).

---

## Exit criteria

- [x] Progress API
- [x] Showcase max 3, public list’te `isShowcased` / `showcaseOrder`
- [x] +4 kod idempotent award
- [x] 08-gamification.md güncel
- [x] status.md → 05 Done

## Sonraki

→ [06-badge-quests.md](06-badge-quests.md)
