# 03 — Recommendation engine

**Amaç:** Explore, friend suggestions ve event discover için **tek skor sözlüğü**.  
**Bağımlılık:** Identity (sports, location, stats), Social (friendships, posts), Events (attendance).  
**Durum:** **Done** (2026-08-14).

---

## V1 baseline

- Event discover: filtre + tarih (`DiscoverEvents`)
- Feed explore: recency
- Friend suggestions: yok

ML / harici AI **yok**. V2 = açıklanabilir heuristic.

---

## Tasarım ilkeleri

1. **Sinyal ≠ ürün yüzeyi.** Engine skor üretir; 01/04 sıralar ve keser.
2. **Açıklanabilir.** Her skor bileşeni loglanabilir / debug DTO’da opsiyonel `reasons[]`.
3. **Cheap path.** Day-1: EF/SQL projection + in-memory weighted sum. Candidate set sınırlı (örn. 500).
4. **Block-aware.** Blocked users/content aday setine girmez.
5. **Cold start.** Yeni kullanıcıda city + sport preference + recency fallback.

---

## Karar kapısı

### Kilitli

| Soru | Karar |
| ---- | ----- |
| Nerede yaşar? | `Application/Services/Recommendations/` |
| API public mi? | **Hayır** — sadece query handler’lar çağırır |
| Model | Weighted sum |
| C1 Ağırlıklar | **`appsettings` `Recommendation` section** |
| Persist skor? | Day-1 hayır (online). V2.1 snapshot opsiyonel |
| Candidate caps | People 200, Events 300, Posts 300 |
| C2 Debug reasons | **Asla client response’ta** — sadece server log/dev |
| C3 Interacted penalty | **V2.1** — day-1 dismiss tablosu yok |

---

## Sinyal sözlüğü (v1)

### People (`ScorePeople`)

| Sinyal | Kaynak | Not |
| ------ | ------ | --- |
| `mutualFriends` | Friendships | En güçlü |
| `sharedSports` | UserSports overlap | |
| `sameCity` | UserProfile location | |
| `skillProximity` | UserSports skill delta | opsiyonel / day-1 yok |
| `activity` | UserStatistics events+posts | soft |
| `reputation` | average_rating | soft |

### Events (`ScoreEvents`)

| Sinyal | Kaynak | Not |
| ------ | ------ | --- |
| `sportMatch` | UserSports vs event.sport | |
| `distance` | lat/lng bbox + Haversine | |
| `timeFit` | starts_at proximity | |
| `fillRatio` | participants/capacity sweet spot | |
| `friendsAttending` | approved/attended friends | |
| `organizerRep` | organizer stats | soft |

### Posts (`ScorePosts`)

| Sinyal | Kaynak | Not |
| ------ | ------ | --- |
| `recency` | created_at decay (~72h) | |
| `engagement` | like+comment counts | |
| `authorFriend` | friendship | |
| `authorRep` | stats | soft |
| `notSeen` | (V2.1) impression | day-1 yok |

---

## Kod

- `IRecommendationService` + `Scored<T>` / DTO’lar: `Application/Abstractions/Recommendations/`
- `RecommendationService` + `RecommendationOptions`: `Application/Services/Recommendations/`
- DI: `AddApplication(IConfiguration)` → `Configure<RecommendationOptions>` + scoped service
- Config: API `appsettings.json` → `Recommendation` section
- Wired: `GetFriendSuggestions` → `ScorePeopleAsync` (reasons client’a gitmez)
- Ready for 04: `ScoreEventsAsync` / `ScorePostsAsync`
- Tests: `RecommendationServiceTests` + friendship suggestions regression

**Migration:** yok (C3 = V2.1).

---

## Exit criteria

- [x] Üç scorer testli ve config’ten ağırlık okur
- [x] Block/banned aday dışı
- [x] Cold-start fallback tanımlı
- [x] Public endpoint yok
- [x] status.md → 03 Done

## Sonraki

→ [04-explore.md](04-explore.md)
