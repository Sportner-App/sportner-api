# 03 — Kalite & tutarlılık sertleştirme

**Amaç:** Mevcut MVP yüzeyinde sessiz bug / drift / test boşluklarını kapatmak.  
**Bağımlılık:** Yok (01/02 ile paralel olabilir).

---

## Karar kapısı

| # | Soru | Varsayılan |
| - | ---- | ---------- |
| 1 | Bu fazda yeni feature yok, sadece hardening? | **Evet** |
| 2 | Integration testlere Testcontainers mi? | **Hayır şimdilik** — mevcut WebApplicationFactory pattern |

---

## 3.1 ConfirmAttendance idempotency

### Problem

`ConfirmAttendance` domain’de Attended ise no-op; handler yine `IncreaseCompletedEvents` çağırabilir → counter şişmesi.

### Nasıl

1. Confirm öncesi participant status oku.
2. Sadece `Approved → Attended` geçişinde:
   - `IncreaseCompletedEvents`
   - attendance rate refresh
   - `FIRST_EVENT` award
3. Test: ikinci confirm → EventsCompleted artmaz.

### Dokunulacak

- `ConfirmAttendanceCommandHandler`
- Application/Domain unit test

### Exit

- [ ] Çift confirm counter’ı bozmuyor
- [ ] Test var

---

## 3.2 Counter matrisi audit

### Ne

`docs/features/10-cross-cutting.md` tablosundaki her alan için “kim artırıyor / azaltıyor” checklist.

### Nasıl

1. Tabloyu markdown checklist’e çevir (bu fazda `docs/roadmap/artifacts/counter-matrix.md` veya features 10 içine).
2. Eksik Decrease/Increase varsa handler’a ekle.
3. Bilinen tamamlar: friends ±, posts ±, badges +, events joined ± (cancel approved), reviews +, rating sync.

### Exit

- [ ] Audit dokümanı var
- [ ] Bulunan gap’ler kapatıldı veya “bilinçli defer (job reconcile)” işaretli

---

## 3.3 Test borcu (minimum paket)

Modül başına hedef:

| Modül | En az |
| ----- | ----- |
| Events | Apply + CancelParticipation + ConfirmAttendance idempotency |
| Social | CreatePost + Like + DeletePost counter |
| Moderation | CreateReport duplicate Conflict + Resolve/Reject review flag |
| Identity | CreateProfile username conflict |

### Nasıl

- Application.UnitTests: handler + InMemory veya mevcut test altyapısı
- Pattern: mevcut `*ValidatorTests` yanına handler testleri
- Integration: auth + 1 profile + 1 sports list (zaten var; bozma)

### Exit

- [ ] Yukarıdaki minimum paket yeşil
- [ ] Toplam test sayısı artmış; CI lokal `dotnet test` OK

---

## 3.4 Docs senkron

### Ne

Eski `Profiles` / `/api/profiles` referansları.

### Nasıl

```powershell
rg -n "ProfilesController|/api/profiles|\bProfiles\b" docs src --glob "*.md"
```

Kalanları `UserProfiles` / `/api/user-profiles` yap (kolon `ProfileImageUrl` dokunma).

### Exit

- [ ] docs’ta stale path yok

---

## 3.5 Swagger DTO isim disiplini

### Ne

Nested request type kısa adları unique olsun (`UpdateLocationRequest` faciası).

### Nasıl

Yeni controller nested DTO: `Update{Resource}{Action}Request` veya resource prefix.  
PR checklist maddesi olarak 00-rules’a zaten işlendi; burada kod review bilinci.

### Exit

- [ ] `/swagger/v1/swagger.json` 200 (manuel)

---

## Exit criteria (03 tamam)

- [ ] 3.1–3.4 tamam
- [ ] status.md güncellendi

## Sonraki

→ [04-background-jobs.md](04-background-jobs.md)
