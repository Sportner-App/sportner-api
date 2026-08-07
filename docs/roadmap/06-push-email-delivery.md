# 06 — Push & email delivery

**Amaç:** `NotificationSettings` push/email flag’leri gerçekten işe yarasın.  
**Bağımlılık:** [04-background-jobs](04-background-jobs.md) (outbox worker). In-app publisher olduğu gibi kalır.

---

## Karar kapısı

| # | Soru | Varsayılan |
| - | ---- | ---------- |
| 1 | Push provider | FCM (Android) + APNs (iOS) — device `PushToken` üzerinden |
| 2 | Email provider | Sonra; day-1 sadece push olabilir |
| 3 | Outbox tablosu yeni migration mı? | **Evet** — `NotificationDeliveryOutbox` veya benzeri |

---

## 6.1 Outbox modeli

### Ne

In-app `Notifications` satırından bağımsız veya ona bağlı delivery kuyruğu:

| Alan | Anlam |
| ---- | ----- |
| NotificationId / payload | Ne gönderilecek |
| Channel | Push / Email |
| Status | Pending / Sent / Failed |
| AttemptCount / NextAttemptAt | Retry |

### Nasıl

1. Domain/Persistence entity + migration.
2. `INotificationPublisher` genişlemesi:
   - In-app: bugünkü gibi
   - Push/Email enabled ise outbox’a enqueue (**aynı UoW**, SaveChanges caller’da)
3. Job: pending outbox’ı batch çek → provider → status update.

### Exit

- [ ] Publisher outbox yazar
- [ ] Worker gönderir / fail’de retry

---

## 6.2 Push gönderimi

### Ne

`UserDevices.PushToken` + platform.

### Nasıl

1. `IPushSender` Application abstract.
2. Infrastructure: FCM HTTP v1 (service account — **secret env**).
3. Token invalid → device token clear / disable (domain method).
4. Settings: `PushEnabled=false` → enqueue yok.

### Exit

- [ ] Gerçek veya sandbox device’a test push
- [ ] Disabled setting skip

---

## 6.3 Email (opsiyonel alt-faz)

- `IEmailSender` + provider (SendGrid/SES…)
- Sadece `EmailEnabled` tipler (NotificationSetting defaults)

---

## Exit criteria (06 tamam)

- [ ] Outbox + worker
- [ ] En az push kanalı canlı
- [ ] features/07 push checkbox update
- [ ] status.md

## Sonraki

→ [07-product-depth.md](07-product-depth.md)
