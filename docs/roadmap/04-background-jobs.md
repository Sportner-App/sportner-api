# 04 — Background jobs

**Amaç:** Periyodik işler (cleanup, reminder, reconcile) için host + ilk job seti.  
**Bağımlılık:** 01 önerilir. Application katmanı host kütüphanesinden bağımsız kalır.

Kaynak: [10-cross-cutting](../features/10-cross-cutting.md)

---

## Karar kapısı (zorunlu konuşma)

| # | Soru | Seçenekler | Öneri |
| - | ---- | ---------- | ----- |
| 1 | Host | A) Hangfire in-process API · B) Quartz in-process · C) Ayrı Worker projesi | **C** uzun vadede temiz; MVP hızı için **A** kabul edilebilir |
| 2 | Storage | Hangfire Postgres / memory (dev) | Dev memory OK; prod Postgres |
| 3 | İlk job seti | Hangisi day-1? | Session cleanup + OTP cleanup + Event reminder |

**Bu faz kodlanmadan host seçimi netleşmeli.**

---

## 4.1 Application contracts

### Ne

```text
ISessionCleanupJob / IOtpCleanupJob / IEventReminderJob
```

veya tek `IBackgroundJob` değil — **use-case servisleri**:

```csharp
// Application
public interface IExpiredSessionCleaner {
  Task<int> CleanupAsync(CancellationToken ct);
}
```

Infrastructure/Worker sadece çağırır + schedule eder.

### Nasıl

1. Application’da cleaner/reminder servisleri (DbContext kullanır, domain kurallarına uyar).
2. Hiçbir Hangfire/Quartz using’i Application’da olmasın.
3. DI: Application registration + Infrastructure schedule wiring.

### Exit

- [ ] Contract’lar Application’da
- [ ] Host referansı sadece Infrastructure / Worker

---

## 4.2 Job: Expired session cleanup

### Ne

Revoked/expired session’ları retention (~90 gün) sonrası sil.

### Nasıl

1. Spec: `UserSessions` — `ExpiresAt` / `RevokedAt` + CreatedAt kuralını database doc’tan doğrula.
2. Batch delete (LIMIT’li döngü — tek seferde milyon satır yok).
3. Schedule: günde 1 (03:00 UTC öneri).
4. Log: silinen adet (PII yok).

### Exit

- [ ] Manuel trigger + scheduled run
- [ ] Test: expired satır silinir, aktif kalır

---

## 4.3 Job: OTP cleanup

### Ne

Kullanılmamış / expired OTP kayıtlarını temizle.

### Nasıl

1. OTP store nerede? (EF table / cache — mevcut `IOtpService` impl’e bak).
2. Retention: expire + 24h buffer (karar).
3. Schedule: saatlik veya günlük.

### Exit

- [ ] Cleanup çalışıyor + test

---

## 4.4 Job: Event reminder

### Ne

Yaklaşan Published event’ler için `NotificationType.EventReminder` (veya mevcut enum).

### Nasıl

1. Pencere: örn. start − 24h ve start − 1h (karar kapısı).
2. Katılımcılar: Approved (+ Attended değil henüz).
3. `INotificationPublisher.PublishAsync` — in-app; push 06’da.
4. **Idempotency:** aynı event+user+window için tekrar gönderme (outbox / “ReminderSent” flag / ayrı tablo).
5. Schedule: her 15 dk.

### Karar kapısı ek

| Soru | Varsayılan |
| ---- | ---------- |
| Reminder pencereleri | 24h + 1h |
| Organizer’a da mı? | Hayır (sadece participants) |

### Exit

- [ ] Reminder bir kez gider
- [ ] Settings `InAppEnabled=false` ise skip (publisher zaten bakıyor)

---

## 4.5 (İsteğe bağlı bu fazda) Counter reconcile + storage GC

Küçük bırakılabilir → 08’e de kayabilir.

- Counter reconcile: source COUNT vs `UserStatistics` / Post counts — drift log + fix mode flag.
- Storage GC: DB’de referansı olmayan path’leri bucket’tan sil (dikkatli allow-list).

---

## 4.6 Ops

- Health: job host ayakta mı (Hangfire dashboard / log heartbeat).
- Dev’de job’lar default kapalı veya sık interval — config flag `BackgroundJobs:Enabled`.

### Dokunulacak (host’a göre)

- Yeni `src/Worker/` veya `Infrastructure/Jobs/*`
- `Program.cs` / DI
- `appsettings` → `BackgroundJobs` section (secret yok)

---

## Exit criteria (04 tamam)

- [ ] Host seçimi uygulandı
- [ ] Session + OTP + Event reminder live
- [ ] Application host’tan izole
- [ ] status.md + features/10 checkbox kısmi update

## Sonraki

→ [05-signalr-realtime.md](05-signalr-realtime.md)
