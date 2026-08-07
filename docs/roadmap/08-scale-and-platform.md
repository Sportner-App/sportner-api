# 08 — Ölçek & platform olgunluğu

**Amaç:** Büyüme, CI, provider ve performans.  
**Bağımlılık:** 01 zorunlu; 04–06 tercihen.

---

## 8.1 PostGIS / advanced discovery

### Karar kapısı

| Soru | Varsayılan |
| ---- | ---------- |
| Radius search birimi | km |
| Index | GIST on event location |

### Nasıl

1. Postgres PostGIS extension (Supabase’de aç).
2. Event location representation (lat/lng mevcut → geography point).
3. `DiscoverEvents` query: optional `lat,lng,radiusKm`.
4. Migration + raw SQL / EF NetTopologySuite.
5. Fallback: radius yoksa bugünkü city/sport filtre.

### Exit

- [ ] Radius filtre doğru sonuç
- [ ] Index explain makul

---

## 8.2 CI pipeline

### Ne

PR’da: restore → build → test.

### Nasıl

1. GitHub Actions (veya mevcut host) workflow.
2. Secret’lar CI secrets.
3. (Opsiyonel) `dotnet ef migrations has-pending-model-changes` check.

### Exit

- [ ] Main korumalı; test zorunlu

---

## 8.3 SMS provider

### Ne

`LoggingSmsSender` → gerçek SMS.

### Nasıl

1. `ISmsSender` impl (Twilio/Netgsm/…).
2. Config secrets env.
3. Rate limit: aynı phone N OTP / saat (Application guard).
4. Dev’de logging fallback flag.

### Exit

- [ ] Staging’de gerçek SMS
- [ ] Rate limit 429/Result

---

## 8.4 Rate limiting & abuse

- OTP request
- Login/verify
- Report create
- ASP.NET rate limiting middleware veya custom

### Exit

- [ ] OTP flood engelli

---

## 8.5 Observability

- Correlation id (traceId zaten ProblemDetails’te)
- Warning/Error alert
- (Opsiyonel) OpenTelemetry

---

## 8.6 Performance smoke

- Feed / discover 1000+ row seed ile
- Cursor pagination regress olmasın
- N+1: AsNoTracking + projection audit

---

## Exit criteria (08 tamam)

- [ ] Seçilen alt maddeler bitti
- [ ] Roadmap status: “platform baseline done”
- [ ] Yeni büyük faz yoksa roadmap “maintenance mode”

---

## Maintenance mode (sonrası)

- Counter reconcile job izleme
- Badge eşik ayarı
- Yeni spor/badge seed
- Güvenlik patch / dependency update
