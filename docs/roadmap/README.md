# Roadmap & Backlog

Bu klasör, Sportner API’nin **şu anki durumunu**, **kalan işleri**, **düzeltilmesi gerekenleri** ve **ileri seviye adımları** tek yerde toplar.

Detaylı feature checklist’ler hâlâ [`docs/features/`](../features/) altında yaşar.  
Tablo / domain kuralları [`docs/database/`](../database/) altında kalır.  
Bu klasör “ne bitti / ne kaldı / sırada ne var” özetidir.

| Dosya | İçerik |
| ----- | ------ |
| [01-current-state.md](01-current-state.md) | Neler tamamlandı (MVP feature yüzeyi) |
| [02-remaining-work.md](02-remaining-work.md) | Yapılması gereken / eksik kalan işler |
| [03-fixes-and-hardening.md](03-fixes-and-hardening.md) | Düzeltilmeli / sertleştirilmeli konular |
| [04-advanced-next.md](04-advanced-next.md) | İleri seviye: jobs, SignalR, admin, geo, test |

---

## Kısa durum (2026-08)

| Alan | Durum |
| ---- | ----- |
| Domain + Persistence | Tamam |
| Feature modülleri 01–09 | Tamam (Identity → Moderation) |
| Cross-cutting (policy, storage cleanup, counters) | Kısmen |
| Jobs / SignalR / push-email | Beklemede |
| Prod güvenlik (secrets, RLS) | Manuel / beklemede |
| Admin CRUD | Beklemede |

**Önerilen sıra:** önce [03](03-fixes-and-hardening.md) (prod öncesi hijyen) → sonra [02](02-remaining-work.md) içindeki yakın MVP boşlukları → en sonda [04](04-advanced-next.md).
