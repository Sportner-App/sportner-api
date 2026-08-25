# 04 — Background jobs

**Amaç:** Periyodik işler (cleanup, reminder, reconcile) için host + ilk job seti.  
**Bağımlılık:** 01 önerilir. Application katmanı host kütüphanesinden bağımsız kalır.

Kaynak: [10-cross-cutting](../features/10-cross-cutting.md)

---

## Karar kapısı (zorunlu konuşma)

| # | Soru | Seçenekler | Öneri | Karar |
| - | ---- | ---------- | ----- | ----- |
| 1 | Host | A) Hangfire in-process API · B) Quartz in-process · C) Ayrı Worker projesi | **C** | **C** — amaçlı ayrı deploy: Identity + Events |
| 2 | Storage | Hangfire Postgres / memory (dev) | — | Cronos + `IHostedService` (Hangfire yok) |
| 3 | İlk job seti | Hangisi day-1? | Session + OTP + Event reminder | Aynı |

---

## 4.1 Application contracts

### Ne

```csharp
IExpiredSessionCleaner / IOtpCleaner / IEventReminderDispatcher
```

Infrastructure/Worker sadece çağırır + schedule eder.

### Exit

- [x] Contract’lar Application’da
- [x] Host referansı sadece worker process’lerinde (`Cronos` via `Sportner.Workers.Hosting`)

---

## 4.2 Job: Expired session cleanup

- Retention: `BackgroundJobs:SessionRetentionDays` (default 90)
- Batch delete via `SessionCleanupBatchSize`
- Cron: `0 3 * * *` (UTC)

### Exit

- [x] Scheduled + `RunOnStartup` ile manuel/dev trigger
- [x] Test: expired silinir, aktif kalır

---

## 4.3 Job: OTP cleanup

OTP challenges: `IOtpChallengeStore` (process-local `InMemoryOtpChallengeStore`).  
`IOtpCleaner` expired entry’leri siler. Distributed store scaling öncesi ayrı iş.

### Exit

- [x] Cleanup + test

---

## 4.4 Job: Event reminder

- Windows: 24h + 1h (`EventReminderWindowsMinutes`)
- Grace: threshold sonrası ~20 dk (15 dk cron’a uyum)
- Participants: Approved, organizer hariç
- Idempotency: `EventReminderDispatches` UNIQUE(event, user, window)
- Cron: `*/15 * * * *`
- Settings: `InAppEnabled=false` → publisher skip

## 4.4b Job: Event auto-complete

- Published/Full events whose `eventDate + durationMinutes` has passed → `Completed`
- Same side effects as `CompleteEvent` (close conversation, organizer badges/quests)
- Also runs lazily on `GET /api/events/{id}` so the API does not depend on the worker being up
- Cron: `*/5 * * * *`

### Exit

- [x] Reminder bir kez gider
- [x] Settings skip publisher’da

---

## 4.5 (İsteğe bağlı bu fazda) Counter reconcile + storage GC

Defer → 08 / sonraki job seti.

---

## 4.6 Ops

Amaçlı worker’lar (ayrı deploy / scale):

| Process | Jobs |
| ------- | ---- |
| `Sportner.Identity.Worker` | session cleanup, OTP cleanup |
| `Sportner.Events.Worker` | event reminders |

```bash
dotnet run --project src/Workers/Identity.Worker/Sportner.Identity.Worker.csproj
dotnet run --project src/Workers/Events.Worker/Sportner.Events.Worker.csproj
```

Shared schedule helpers: `src/Workers/Hosting`.  
Config: `BackgroundJobs:Enabled`, `RunOnStartup`, cron (UTC). Dev’de `RunOnStartup: true`.

Migration: `AddEventReminderDispatches`. RLS listesine tablo eklendi — SQL Editor’de yeniden çalıştır.

---

## Exit criteria (04 tamam)

- [x] Host seçimi uygulandı (Identity.Worker + Events.Worker)
- [x] Session + OTP + Event reminder live
- [x] Application host’tan izole
- [x] status.md + features/10 checkbox kısmi update

## Sonraki

→ [05-signalr-realtime.md](05-signalr-realtime.md)
