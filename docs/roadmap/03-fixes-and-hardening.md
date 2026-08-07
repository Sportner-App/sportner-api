# 03 — Düzeltmeler & sertleştirme

“Yeni feature” değil; mevcut yüzeyin kalitesi / güvenliği / tutarlılığı.

## Güvenlik

| Konu | Durum | Aksiyon |
| ---- | ----- | ------- |
| Secrets tracked dosyalarda | Risk | User-secrets / env’e taşı; commit edilmiş secret’ları **rotate** et ([configuration](../configuration.md)) |
| Supabase RLS | Doğrulanmalı | Tüm business tablolarda RLS; `anon`/`authenticated` revoke; Data API kapalı tercih |
| `service_role` sadece server | Doğrulanmalı | Client’a asla verilmez |
| OTP / JWT / refresh loglanmıyor | Korunmalı | Serilog enricher / log review |
| Moderator = Guid allow-list | Geçici | Prod ölçeğinde role/claim modeline geç |

## Authorization boşlukları

| Policy | Durum | Eksik |
| ------ | ----- | ----- |
| `ActiveUser` | Var (default `[Authorize]`) | — |
| `CanCreateContent` | Var (seçili write endpoint’ler) | Tüm write yüzeyinin gözden geçirilmesi (edge endpoint’ler) |
| `Moderator` | Var | Allow-list doldurulmalı |
| `Admin` | Yok | Sports / Badges / ReportReasons mutate için gerekli |

## Veri tutarlılığı

| Konu | Not |
| ---- | --- |
| Cached counters | Çoğu mutation’da güncelleniyor; cancel approved → `DecreaseEventsJoined` eklendi |
| ConfirmAttendance idempotency | Tekrar çağrıda `IncreaseCompletedEvents` yan etkisi gözden geçirilmeli |
| Counter drift | Gecelik reconcile job ile kapatılacak (P2) |
| Docs vs kod isimleri | `UserProfiles` tablosu; bazı eski metinlerde “profiles” kalmış olabilir — docs/roadmap ile features senkron tutulmalı |

## API / client

| Konu | Aksiyon |
| ---- | ------- |
| Profile route rename | Client’ta `/api/user-profiles` |
| Swagger nested DTO çakışması | `UpdateCityRequest` / `UpdateEventLocationRequest` ile çözüldü; yeni nested DTO’larda kısa isim tekrarı yapma |
| ProblemDetails / localization | Mevcut global handler; yeni error code’lar Localization’a düşmeli |

## Test & kalite

| Konu | Aksiyon |
| ---- | ------- |
| Handler test coverage | Modül başına happy + 2–3 invariant fail |
| Integration | Auth + bir dikey slice / modül |
| Migration güvenliği | Rename tarzı değişikliklerde Drop+Create üretme; review zorunlu |
| Build | 0 warning hedefi (yeni değişikliklerde) |

## Operasyonel checklist (ortam ayağa kalkarken)

- [ ] `dotnet ef database update` hedef DB’de
- [ ] Seed çalıştı (Sports / Badges / ReportReasons)
- [ ] RLS / Data API kontrolü
- [ ] Swagger Development’ta açılıyor (`/swagger`)
- [ ] OTP/SMS provider gerçek mi yoksa log-only mı (bilinçli)
- [ ] Storage bucket’ları (`avatars`, `intro-videos`, `post-media`, `chat-media`) mevcut
