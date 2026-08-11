# 08 — Ölçek & platform olgunluğu

**Amaç:** Büyüme, CI, provider ve performans.  
**Bağımlılık:** 01 zorunlu; 04–06 tercihen.

---

## 8.1 PostGIS / advanced discovery

### Karar kapısı

| Soru | Varsayılan |
| ---- | ---------- |
| Radius search birimi | km |
| Index | Day-1: composite `(Latitude, Longitude)`; PostGIS GIST later |

### Nasıl

1. ~~Postgres PostGIS extension~~ — deferred.
2. Event lat/lng + bounding-box filter (approx radius).
3. `DiscoverEvents` query: optional `lat,lng,radiusKm`.
4. Composite index migration `AddEventLocationIndex`.
5. Fallback: radius yoksa city/sport filtre.

### Exit

- [x] Radius filtre (bounding box) Discover’da
- [x] Lat/lng index
- [ ] PostGIS GIST + exact distance (follow-up)

---

## 8.2 CI pipeline

### Ne

PR’da: restore → build → test.

### Nasıl

1. `.github/workflows/ci.yml` — GitHub Actions.
2. Secret’lar CI secrets (gerekirse).
3. (Opsiyonel) `dotnet ef migrations has-pending-model-changes` check — not yet.

### Exit

- [x] Workflow: restore / build / test on PR + push
- [ ] Branch protection “required check” (repo settings — owner)

---

## 8.3 SMS provider

### Ne

`LoggingSmsSender` → gerçek SMS (Http bridge).

### Nasıl

1. `ISmsSender`: `LoggingSmsSender` | `HttpSmsSender` (`Sms:Provider`).
2. Config: `Sms:HttpEndpoint`, `Sms:HttpBearerToken`.
3. Rate limit: `IOtpRateLimiter` / phone window (`Otp:MaxRequestsPerWindow`).
4. Dev’de Logging default.

### Exit

- [x] Rate limit → `ErrorType.TooManyRequests` / 429
- [x] Logging fallback + Http provider switch
- [ ] Staging’de gerçek SMS (ops: Provider=Http + endpoint)

---

## 8.4 Rate limiting & abuse

- OTP request (Application + ASP.NET auth policy)
- Login/verify / refresh (auth policy)
- Report create (reports policy)

### Exit

- [x] OTP flood engelli (per-phone + IP middleware)

---

## 8.5 Observability

- [x] Correlation id middleware (`X-Correlation-Id`) + ProblemDetails `correlationId`
- [ ] Warning/Error alert (ops)
- [ ] (Opsiyonel) OpenTelemetry

---

## 8.6 Performance smoke

- [ ] Feed / discover 1000+ row seed
- [ ] Cursor pagination regress
- [ ] N+1 audit

---

## Exit criteria (08 tamam)

- [x] Seçilen alt maddeler (CI, SMS bridge, rate limit, correlation, discover bbox) bitti
- [x] Roadmap status: “platform baseline done”
- [ ] PostGIS / perf smoke / branch protection — optional follow-ups

---

## Maintenance mode (sonrası)

- Counter reconcile job izleme
- Badge eşik ayarı
- Yeni spor/badge seed
- Güvenlik patch / dependency update
- PostGIS exact radius
- Real FCM/APNs + real SMS staging
