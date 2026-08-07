# 02 — Admin policy + Catalog CRUD

**Amaç:** Spor kataloğunu seed’e mahkûm etmeden yönetebilmek.  
**Bağımlılık:** 01’deki güvenlik hijyeni tercihen bitmiş olsun (Admin Guid’leri secretsiz config’te tutulabilir).

Kaynak: [02-catalog.md](../features/02-catalog.md) · [10-cross-cutting](../features/10-cross-cutting.md)

---

## Karar kapısı

| # | Soru | Varsayılan |
| - | ---- | ---------- |
| 1 | Admin modeli? | **Allow-list** `Authorization:AdminUserIds` (Moderator ile aynı pattern) |
| 2 | Admin ≠ Moderator mi? | **Evet** ayrı listeler; aynı kişi her iki listede olabilir |
| 3 | Badge / ReportReason admin bu fazda mı? | **Hayır** — sadece Sports; diğerleri 07 |

---

## 2.1 `Admin` authorization policy

### Ne

Moderator’a benzer `Admin` policy.

### Nasıl

1. `AuthorizationPolicies.Admin = "Admin"`
2. Options: `AdminUserIds: List<Guid>` (`ModeratorAuthorizationOptions` genişlet veya `AdminAuthorizationOptions`)
3. `AdminAuthorizationHandler` → current user id listede mi
4. Policy: `RequireAuthenticatedUser` + `ActiveUser` + `AdminRequirement`
5. DI + `appsettings` / user-secrets bağlama

### Dokunulacak

- `src/API/Authorization/*`
- `AuthenticationExtension.cs`
- `appsettings.*` → `Authorization:AdminUserIds`

### Exit

- [ ] Admin user → 200; diğer → 403
- [ ] Logout hâlâ `Authenticated` only

---

## 2.2 Application — Sports commands

### Ne (deferred’den açılacak)

| Use case | Endpoint | Domain |
| -------- | -------- | ------ |
| `CreateSport` | `POST /api/sports` | `Sport.Create` |
| `RenameSport` | `PUT /api/sports/{id}` | rename |
| `ChangeSportDisplayOrder` | `PUT /api/sports/{id}/display-order` | order |
| `DeactivateSport` | `POST /api/sports/{id}/deactivate` | soft deactivate — **hard delete yok** |
| `ActivateSport` | `POST /api/sports/{id}/activate` | activate |

### Nasıl (her use case)

1. Command + FluentValidation (slug ASCII, name length — database spec)
2. Handler: load → domain method → `SaveChanges` → Result
3. Errors: NotFound, Conflict (slug unique), Validation, Forbidden (policy API’de)
4. Unit test: validator + 1–2 handler invariant (slug conflict)

### Dokunulacak

- `src/Application/Features/Catalog/...` (veya mevcut Sports feature klasörü)
- `src/Domain/Sports/Sport.cs` (method yoksa ekle)
- `src/API/Controllers/SportsController.cs` → `[Authorize(Policy = Admin)]` mutate’lerde; list/get anonymous/authenticated kalır

### Exit

- [ ] 5 mutate endpoint çalışıyor
- [ ] Deactivate sonrası `ListActiveSports` göstermiyor; mevcut Event FK bozulmuyor
- [ ] `docs/features/02-catalog.md` checkbox’ları `[x]`

---

## 2.3 API sözleşmesi notları

- Public read: `GET /api/sports`, `GET /api/sports/{slug}` — Admin gerekmez.
- Mutate: sadece Admin.
- Response DTO’lar mevcut list projection ile uyumlu olsun.

---

## Exit criteria (02 tamam)

- [ ] Admin policy wired + en az 1 AdminUserId
- [ ] Sports CRUD (activate/deactivate dahil) live
- [ ] Tests yeşil
- [ ] status.md güncellendi

## Sonraki

→ [03-quality-hardening.md](03-quality-hardening.md)  
(Paralel: 02 ile 03 birlikte yürüyebilir.)
