# 06 — Push & email delivery

**Amaç:** `NotificationSettings` push/email flag’leri gerçekten işe yarasın.  
**Bağımlılık:** [04-background-jobs](04-background-jobs.md). In-app publisher genişletildi.

**Durum:** Done (2026-08) — push outbox + `Notifications.Worker`; email alt-faz sonra.

---

## Karar kapısı

| # | Soru | Varsayılan |
| - | ---- | ---------- |
| 1 | Push provider | Day-1: `LoggingPushSender` (pipeline canlı). FCM/APNs credentials → sonra `IPushSender` swap |
| 2 | Email provider | Sonra; day-1 enqueue yok (Email channel reserved) |
| 3 | Outbox tablosu | **Evet** — `NotificationDeliveryOutbox` |
| 4 | Worker host | `Notifications.Worker` (ayrı deploy) |

---

## 6.1 Outbox modeli

### Exit

- [x] Publisher outbox yazar (`PushEnabled`)
- [x] Worker gönderir / fail’de retry (max 5 + backoff)

---

## 6.2 Push gönderimi

### Exit

- [x] `IPushSender` + Logging sandbox
- [x] Token invalid → `ClearPushToken`
- [x] `PushEnabled=false` → enqueue yok
- [x] No device token → `Cancelled`

---

## 6.3 Email (opsiyonel alt-faz)

- `IEmailSender` + provider — deferred

---

## Exit criteria (06 tamam)

- [x] Outbox + worker
- [x] Push kanalı pipeline canlı (Logging)
- [x] features/07 update
- [x] status.md

## Sonraki

→ [07-product-depth.md](07-product-depth.md)
