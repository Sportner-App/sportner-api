# V2 — Ürün derinliği roadmap

V1 backend feature set tamam. Bu klasör **kod yazmadan önce** V2 kapsamını,
kararları ve uygulama sırasını netleştirir.

Uygulamaya başlarken: “`docs/v2/0X-….md` ile başla” demen yeterli.

| # | Playbook | Ne |
| - | -------- | -- |
| — | [status.md](status.md) | Anlık V2 ilerleme |
| 00 | [00-execution-rules.md](00-execution-rules.md) | Her fazdan önce |
| 01 | [01-friends-depth.md](01-friends-depth.md) | Arkadaşlık derinliği (öneri, mutual, arama) |
| 02 | [02-dm-depth.md](02-dm-depth.md) | DM / sohbet derinliği (receipt, mute, search, realtime) |
| 03 | [03-recommendation-engine.md](03-recommendation-engine.md) | Ortak skor motoru (insan / etkinlik / post) |
| 04 | [04-explore.md](04-explore.md) | Explore yüzeyleri (People / Events / Feed) |
| 05 | [05-badges-depth.md](05-badges-depth.md) | Rozet ilerleme, showcase, yeni kodlar |
| 06 | [06-badge-quests.md](06-badge-quests.md) | Rozet görevleri / quest progress |
| 07 | [07-photo-albums.md](07-photo-albums.md) | Profil + etkinlik fotoğraf albümleri |

---

## V1 → V2 haritası (kritik)

Senin ürün listende bazı maddeler **V1’de zaten baseline** olarak var.
V2 onları yeniden yazmaz; **derinleştirir** veya **yeni alt-sistem** ekler.

| Ürün dili | V1 durumu | V2 anlamı |
| --------- | --------- | --------- |
| Friends | Friendship CRUD, block, counts, `FIRST_FRIEND` | Öneri, mutual friends, arama, privacy ince ayar |
| DM | Direct + Group conversation + mesaj REST (+ SignalR hub hazır) | Read/mute/search/typing + **stranger Direct DM** |
| Explore | `GET /api/feed/explore` (recency) + `DiscoverEvents` (filtre) | **Tab’lı** ranked People / Events / ForYou |
| Badges | Catalog + award hooks + eşikler | Progress API, showcase, yeni badge set |
| Gelişmiş öneri | Yok (basit filtre / recency) | Ortak scoring engine + feature sinyalleri |
| Rozet görevleri | Yok (anlık award) | Quest tanımları + progress; **ödül = badge only** |
| Fotoğraf albümleri | Yok (`PostMedia` / profil avatar ayrı) | Album + AlbumMedia; event’te **katılımcılar yükler** |

---

## Önerilen sıra

```text
00 kurallar
   ↓
01 Friends depth          ← sosyal graf zenginleşir
   ↓
02 DM depth               ← 01 ile kısmen paralel olabilir
   ↓
03 Recommendation engine  ← Explore ve öneriler buna bağımlı
   ↓
04 Explore                ← 03’ü tüketir
   ↓
05 Badges depth           ← progress / showcase
   ↓
06 Badge quests           ← 05 + yeni tablolar
   ↓
07 Photo albums           ← greenfield; diğerlerinden görece bağımsız
```

**Kural:** Bir playbook’un exit criteria’sı dolmadan sonrakine geçmeyiz
(bilinçli istisna konuşulur). 02 ile 01 paralel açılabilir; 04, 03 olmadan
“ranked” diye kodlanmaz.

---

## Bu klasör / diğer docs

| Kaynak | Rol |
| ------ | --- |
| [`docs/features/`](../features/) | V1 modül checklist / endpoint’ler (güncellenir) |
| [`docs/database/`](../database/) | Tablo invariant’ları — V2 yeni tablolar burada spec alır |
| [`docs/roadmap/`](../roadmap/) | V1 sonrası ops / platform playbook’ları |
| [`docs/v2/`](.) | **V2 ürün playbook’ları** |
| [`.cursor/rules/`](../../.cursor/rules) | Mimari standartlar (en yüksek öncelik) |

Conflict: `.cursor/rules` > `docs/database` > `docs/features` > `docs/v2` > `docs/roadmap`.

---

## Bilinçli defer (V2 dışı)

- ML / harici AI recommendation servisi (V2 = heuristic + SQL/EF; “AI-ready” sinyal tablosu opsiyonel)
- Premium / paid events / payments
- Tournament / team
- Admin panel UI (seed + minimal admin API yeterliyse)
- Real FCM/APNs (ops; `ek-notlar.md`)
