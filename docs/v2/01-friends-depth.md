# 01 — Friends depth

**Amaç:** V1 friendship state machine’i koruyarak keşif, mutual ve arama eklemek.  
**Bağımlılık:** V1 Social (`docs/features/06-social.md`). Reco sinyalleri için 03’e girdi sağlar.  
**Durum:** Done (2026-08-14).

---

## V1 baseline (dokunma / bozma)

Zaten var:

- Send / Accept / Reject / Block / Remove
- List friends, pending in/out
- `FriendsCount`, `FIRST_FRIEND`, bildirimler

V2 bunları yeniden yazmaz.

---

## V2 kapsamı

| Madde | Açıklama |
| ----- | -------- |
| Friend suggestions | “Seni tanıyor olabilecekler” listesi |
| Mutual friends | Ortak arkadaş sayısı + örnek liste |
| Friend search | Kendi arkadaşlarında username/displayName arama |
| Incoming request richer DTO | Avatar, mutual count, ortak spor |
| Privacy | Suggestions’ta block / reject / “gizli profil” saygı |

### Bilinçli defer

- Yakınlık tabanlı “people nearby map” (04 Explore People’a bağlı)
- Follow modeli (friend ≠ follow; V2’de follow yok)
- Arkadaşlık süresi / anniversary badge (05’e aday)

---

## Karar kapısı

### Kilitli

| Soru | Karar |
| ---- | ----- |
| Suggestion kaynakları | Mutual friends ≥1 **veya** shared `UserSports` ≥1 **veya** aynı city (profile location) |
| Exclude | Self, already friends/pending, blocked either way, banned/deleted users |
| Ranking | 03 engine `ScorePeople` (geçici: mutual > shared sport > city) |
| Mutual endpoint | `GET /api/friendships/mutual/{userId}` — her iki taraf da public/friends-visible |
| Search scope | Sadece **accepted friends** (global people search → 04) |
| Limit | Suggestions default 20, max 50; cursor yok (score page) day-1 |
| A1 Private profil | Suggestion’da **görünmez** |
| A2 Rejected pair | **30 gün cooldown** sonra tekrar aday olabilir |
| A3 Ignore suggestion | **V2.1** — day-1 tablo yok |

---

## Domain / Application plan

**Domain:** Yeni aggregate yok. Gerekirse `Friendship` query helpers Application’da kalır.

**Application slices (taslak):**

| Use case | Type | Endpoint |
| -------- | ---- | -------- |
| `GetFriendSuggestions` | Query | `GET /api/friendships/suggestions` |
| `GetMutualFriends` | Query | `GET /api/friendships/mutual/{userId}` |
| `SearchFriends` | Query | `GET /api/friendships/search?q=` |
| `ListPendingRequests` enrich | Query (extend) | mevcut route — response’a mutual/sports ekle |

**Notifications:** Yeni tip yok (suggestion push V2.1).

---

## Dokunulacak alanlar

- `Application/Features/Social/Friendships/*`
- `API/Controllers/FriendshipsController.cs`
- Tests: suggestion exclude rules, mutual count
- Docs: `docs/features/06-social.md`

DB migration: day-1 yok (A3 = V2.1).

---

## Uygulama sırası

1. Response DTO’lar (`FriendSuggestionItem`, `MutualFriendsResponse`).
2. `GetMutualFriends` (en basit).
3. `SearchFriends`.
4. `GetFriendSuggestions` (geçici skor; 03 gelince adapter; private exclude + 30g reject cooldown).
5. Pending list enrich.
6. Tests + features doc.

---

## Exit criteria

- [x] Suggestions: exclude kuralları testli
- [x] Mutual friends doğru ve block’ta 403/empty kararı net
- [x] Search sadece accepted friends
- [x] Pending DTO zenginleşti
- [x] 06-social.md güncellendi
- [x] status.md → 01 Done

## Sonraki

→ [02-dm-depth.md](02-dm-depth.md) (paralel mümkün) veya → [03-recommendation-engine.md](03-recommendation-engine.md)
