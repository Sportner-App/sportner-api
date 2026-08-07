# Status — Ne bitti? (anlık özet)

Son güncelleme: 2026-08 (01 ops başladı).

## Tamam

| Alan | Not |
| ---- | --- |
| Domain + Persistence | 26 entity; `InitialCreate` + `RenameProfilesToUserProfiles` |
| Features 01–09 | Identity → Moderation |
| In-app notifications / FIRST_* badges | Publisher + BadgeAwarder |
| Auth policies | ActiveUser, CanCreateContent, Moderator (allow-list) |
| Storage cleanup | StorageCleanup |
| API rename | `/api/user-profiles`, tablo `UserProfiles` |

## 01 Ops & güvenlik

| Madde | Durum |
| ----- | ----- |
| DB migrate verify | Done |
| RLS SQL runbook (`docs/ops/supabase-rls.md`) | Done (SQL Editor’de çalıştırma bekleniyor) |
| Secrets → user-secrets; tracked JSON temiz | Done |
| Secret **rotate** | Pending (sen) |
| ModeratorUserIds | Pending (Guid bekleniyor) |
| Client `/api/user-profiles` | Pending (client repo) |

## Sıradaki

1. Senin 01 aksiyonların (RLS çalıştır + rotate + moderator Guid)  
2. Sonra → [02-admin-catalog.md](02-admin-catalog.md)
