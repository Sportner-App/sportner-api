# Roadmap — Planlama playbook’ları

Bu klasör, feature MVP (01–09) bittikten **sonraki** işlerin **nasıl yapılacağını** adım adım anlatır.

Kod yazmaya başlamadan önce buradaki fazı okuyup onaylarız.  
Uygulama başlarken: “`docs/roadmap/0X-….md` ile başla” demen yeterli.

| # | Playbook | Ne zaman |
| - | -------- | -------- |
| — | [status.md](status.md) | Anlık “ne bitti” özeti (referans) |
| 00 | [00-execution-rules.md](00-execution-rules.md) | Her fazdan önce oku |
| 01 | [01-ops-security.md](01-ops-security.md) | **İlk uygulanacak** — güvenlik / ops |
| 02 | [02-admin-catalog.md](02-admin-catalog.md) | Admin policy + Sports CRUD |
| 03 | [03-quality-hardening.md](03-quality-hardening.md) | Test borcu, counter/idempotency |
| 04 | [04-background-jobs.md](04-background-jobs.md) | Job host + cleanup/reminder |
| 05 | [05-signalr-realtime.md](05-signalr-realtime.md) | Event chat realtime |
| 06 | [06-push-email-delivery.md](06-push-email-delivery.md) | Push / email (jobs’a bağlı) |
| 07 | [07-product-depth.md](07-product-depth.md) | İleri badge, moderation yan etki, DM |
| 08 | [08-scale-and-platform.md](08-scale-and-platform.md) | PostGIS, CI, SMS, rate limit |

---

## Önerilen uygulama sırası

```text
00 kurallar
   ↓
01 Ops & güvenlik          ← kapı: prod / paylaşılan ortam
   ↓
02 Admin catalog           ← paralel olabilir 03 ile
   ↓
03 Kalite / hardening
   ↓
04 Background jobs         ← reminder / cleanup / reconcile
   ↓
05 SignalR                 ← REST bozulmadan
   ↓
06 Push / email            ← 04’e bağımlı
   ↓
07 Ürün derinliği          ← eşikler konuşulmadan kodlama
   ↓
08 Ölçek & platform
```

**Kural:** Bir playbook’un “Exit criteria” kutusu dolmadan sonrakine geçmeyiz (bilinçli istisna konuşulur).

---

## Bu klasör / features ilişkisi

| Kaynak | Rol |
| ------ | --- |
| [`docs/features/`](../features/) | Modül checklist, endpoint listeleri |
| [`docs/database/`](../database/) | Tablo invariant’ları |
| [`docs/roadmap/`](.) | **Nasıl ilerleyeceğiz** (playbook) |
| [`.cursor/rules/`](../../.cursor/rules) | Mimari / kod standartları (en yüksek öncelik) |

Conflict olursa: `.cursor/rules` > `docs/database` > `docs/features` > `docs/roadmap`.
