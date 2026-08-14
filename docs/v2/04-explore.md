# 04 — Explore

**Amaç:** Keşif ürün yüzeyi — People / Events / Social feed, hepsi 03 skor motorunu kullanır.  
**Bağımlılık:** [03-recommendation-engine.md](03-recommendation-engine.md) Done.  
**Durum:** **Done** (2026-08-14).

---

## V1 baseline

| Yüzey | V1 |
| ----- | -- |
| Social explore | `GET /api/feed/explore` — recency |
| Events | `GET /api/events` — filtre + tarih; bbox index var |
| People | Yok (sadece username profile get) |

---

## V2 ürün modeli

**Tab’lı Explore** — authenticated only:

| Tab | Endpoint | Kaynak |
| --- | -------- | ------ |
| People | `GET /api/explore/people` | `ScorePeople` (+ optional `sportId`, `city`) |
| Events | `GET /api/explore/events` | `ScoreEvents` (`sportId`, `city`, `lat`, `lng`, `radiusKm`) |
| For You | `GET /api/explore/posts` | `ScorePosts` |

Compat: `GET /api/events` + `GET /api/feed/explore` **kırılmadan** kaldı (filtre/recency).

---

## Karar kapısı

| Soru | Karar |
| ---- | ----- |
| UX | Tab’lı |
| Auth | Authenticated only (D2) |
| Pagination | Day-1 `limit` (1–50). People refresh-page; ranked cursor V2.1 |
| Filters | Events: sport/city/geo. People: sportId/city. Skill day-1 yok (event’te skill alanı yok) |
| Score / reasons | Client’a gitmez (C2) |
| Not interested | V2.1 (D3) |

---

## Response

- **People:** `ExplorePersonItemResponse` (suggestions ile aynı alanlar)
- **Events:** `ExploreEventItemResponse` = list card + `distanceKm`, `friendsAttending`, `sportMatch`
- **Posts:** mevcut `PostResponse`

---

## Kod

- `Application/Features/Explore/*`
- `API/Controllers/ExploreController.cs` → `/api/explore/{people|events|posts}`
- Tests: `ExploreHandlerTests`

**Migration:** yok.

---

## Exit criteria

- [x] Üç tab ranked çalışıyor
- [x] Block / privacy V1 ile tutarlı (reco + handler)
- [x] V1 endpoint’leri bozulmadı
- [x] Paging dokümante (`limit`)
- [x] status.md → 04 Done

## Sonraki

→ [05-badges-depth.md](05-badges-depth.md)
