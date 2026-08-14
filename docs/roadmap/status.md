# Status — Ne bitti? (anlık özet)

Son güncelleme: 2026-08 (V1 auth → username/password).

## Tamam (V1 MVP)

| Alan | Not |
| ---- | --- |
| Auth | **Username + password** (register/login); OTP/SMS kaldırıldı |
| Users / Sports / Events / Applications / Participants | Done |
| Event Messages | REST + SignalR |
| Reviews / Notifications / Reports / Saved Locations | Done |
| Workers / Push outbox / Badges / Direct+Group | Done |
| Platform baseline | CI, rate limit, correlation, discover bbox |

## Owner / sonraya — [`docs/ops/ek-notlar.md`](../ops/ek-notlar.md)

| Madde | Durum |
| ----- | ----- |
| RLS SQL | Ertelendi |
| Client `/api/user-profiles` | ✅ Done |
| Real FCM/APNs | Pending |
| Admin panel | Yok — seed ile |
| PostGIS / perf smoke | Optional |

## Demo login

- username: `ahmet` (veya elif/mert/zeynep)
- password: `Demo123!`
- moderator Guid: `88f544fe-ff4a-4f10-8272-64a5dae757e4` (ahmet)

## Sıradaki

V1 backend feature set tamam. Ops notları `ek-notlar.md`.  
V2 planlama: [`docs/v2/`](../v2/) (kod yok — karar kapıları önce).
