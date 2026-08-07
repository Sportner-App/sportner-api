# 01 — Ops & güvenlik (ilk faz)

**Amaç:** Paylaşılan / prod’a yakın ortamda API’nin güvenli ayakta durması.  
**Sıra:** Bu faz bitmeden 04–08’e geçmeyiz.  
**Süre tahmini:** 0.5–1 gün (manuel dashboard + küçük config).

Bağımlılık: [00-execution-rules](00-execution-rules.md) · [configuration](../configuration.md) · [00-prerequisites](../features/00-prerequisites.md)

**Durum (2026-08):** Repo tarafı büyük ölçüde uygulandı. Dashboard (RLS SQL çalıştırma) + secret rotate + moderator Guid senin adımların.

---

## Karar kapısı (uygulamadan önce)

| # | Soru | Varsayılan (konuşulmazsa) | Bu tur |
| - | ---- | ------------------------- | ------ |
| 1 | Secrets’ı şimdi tracked dosyadan çıkarıyor muyuz? | **Evet** — local user-secrets; Production env | Yapıldı |
| 2 | Commit history’deki secret’ları rotate ediyor muyuz? | **Evet** | **Senin aksiyonun** (dashboard) |
| 3 | RLS SQL’i repo’ya `docs/ops/` altına mı koyuyoruz? | **Evet** | Yapıldı — SQL Editor’de çalıştırılacak |
| 4 | Moderator Guid listesini kim verecek? | Sen | Bekleniyor |

---

## 1.1 Database migrate doğrulama

### Exit

- [x] Update hatasız (`No migrations were applied. The database is already up to date.`)
- [x] `UserProfiles` mevcut (migration history’de `RenameProfilesToUserProfiles`)

---

## 1.2 Supabase RLS + Data API

SQL runbook: [`docs/ops/supabase-rls.md`](../ops/supabase-rls.md)

### Exit

- [x] Secretsiz SQL + tablo listesi repo’da
- [ ] RLS SQL Supabase SQL Editor’de çalıştırıldı *(sen)*
- [ ] Data API kapatıldı / sıkılaştırıldı *(sen)*
- [ ] Backend smoke test (RLS sonrası) *(sen)*

---

## 1.3 Secrets hijyeni

### Exit

- [x] Tracked JSON’da password / service_role / JWT secret yok
- [x] Local run OK (user-secrets ile dinliyor)
- [ ] Rotate yapıldı *(sen — Supabase password, JWT secret, service_role)*
- [x] `docs/configuration.md` güncel

---

## 1.4 Moderator allow-list

### Exit

- [ ] En az 1 moderator Guid tanımlı *(Guid’i yaz, config’e ekleriz)*
- [ ] Moderator endpoint smoke test OK
- [ ] Non-moderator 403

Config yeri: `Authorization:ModeratorUserIds` (appsettings veya user-secrets).

---

## 1.5 Client route senkronu

### Exit

- [ ] Client’ta `/api/profiles` → `/api/user-profiles` *(client repo — sen)*

---

## 1.6 Smoke checklist (faz sonu)

```text
[x] API ayağa kalkıyor (Development, user-secrets)
[ ] Swagger /swagger/v1/swagger.json 200 (VS ile doğrula)
[ ] POST /api/auth/request-otp + verify-otp
[ ] GET /api/user-profiles/me (token ile)
[ ] GET /api/sports
[ ] GET /api/reports (moderator token — Guid sonrası)
```

---

## Exit criteria (01 tamam)

- [~] Repo işleri bitti; dashboard + Guid + rotate + client senkronu açık
- [x] `docs/roadmap/status.md` güncellendi
- [ ] Rotate bilinçli tamam / ertelendi notu

## Sonraki (01 kapandıktan sonra)

→ [02-admin-catalog.md](02-admin-catalog.md)

## Senin sıradaki 3 aksiyon

1. Supabase SQL Editor’de [`docs/ops/supabase-rls.md`](../ops/supabase-rls.md) script’ini çalıştır.  
2. Eski commit’lenen secret’ları **rotate** et (DB password, JWT, service_role) → yeni değerleri `dotnet user-secrets set ...`.  
3. Moderator olacak kullanıcının `UserId` (Guid) değerini ver → `ModeratorUserIds`’e ekleyelim.
