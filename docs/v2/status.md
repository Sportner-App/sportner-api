# V2 status

Son güncelleme: 2026-08-14 (`07` Photo albums Done — V2 ürün listesi kapandı).

## Owner kilitleri

### Tur 1

| Konu | Karar |
| ---- | ----- |
| Explore UX | Tab’lı (People / Events / ForYou) |
| Stranger DM | Açık — Direct’te friendship zorunlu değil; Group aynı |
| Quest ödülü | Sadece badge (XP yok) |
| Event albüm upload | Accepted / Attended (+ organizer) |

### Tur 2 (öneri onayı)

| Madde | Karar |
| ----- | ----- |
| A1–A3 Friends | Private gizli; reject 30g cooldown; ignore V2.1 |
| B1–B4 DM | seen-by V2.1; mute tüm push; edit receipt değişmez; **mesaj limiti yok** |
| C1–C3 Reco | appsettings; reasons client’ta yok; dismiss V2.1 |
| D2–D3 Explore | guest yok; not-interested V2.1 |
| E1–E3 Badges | UserBadge showcase; secret V2.1; +4 yeni kod |
| F1/F3/F4 Quests | evergreen; auto-complete; non-repeatable |
| G1/G3/G4 Albums | EventParticipants; image only; Report Album |

## Fazlar

| Faz | Durum | Not |
| --- | ----- | --- |
| 00 Execution rules | Ready | |
| 01 Friends depth | **Done** | |
| 02 DM depth | **Done** | |
| 03 Recommendation engine | **Done** | |
| 04 Explore | **Done** | |
| 05 Badges depth | **Done** | |
| 06 Badge quests | **Done** | |
| 07 Photo albums | **Done** | Albums/AlbumMedia + migration |

## Kod

`01`–`07` tamam. V2 planlanan kapsam kapandı.  
Ops defer’ler: `docs/ops/ek-notlar.md` (RLS, real push, vb.).
