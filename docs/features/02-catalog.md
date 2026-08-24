# 02 — Catalog (Sports)

Table: `Sports`. Domain: `src/Domain/Sports/Sport.cs`. Spec: `docs/database/03-sports.md`.

Depends on: seed from [00-prerequisites.md](00-prerequisites.md). Admin policy for mutations ([02-admin-catalog](../roadmap/02-admin-catalog.md)).

---

## Progress

- [x] List active sports (client) + get by slug
- [x] List active sports supports `q` search + offset pagination (`page` / `pageSize`)
- [x] Admin create / rename / reorder / activate / deactivate

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `SportsController` | `/api/sports` |

---

## Features

| Status | Use case | Type | Endpoint | Auth | Domain / notes |
| ------ | -------- | ---- | -------- | ---- | -------------- |
| [x] | `ListActiveSports` | Query | `GET /api/sports` | `[Authorize]` | `IsActive` only; order by `DisplayOrder`. Optional `q` (min 2 chars, name/slug contains), `page`, `pageSize` → `PagedResult`. |
| [x] | `GetSportBySlug` | Query | `GET /api/sports/{slug}` | `[Authorize]` | Active only; slug lookup is case-insensitive. 404 when missing/inactive. |
| [x] | `CreateSport` | Command | `POST /api/sports` | Admin | Unique name + slug; optional icon. |
| [x] | `RenameSport` | Command | `PUT /api/sports/{id}` | Admin | Rename; optional slug / icon update. |
| [x] | `ChangeSportDisplayOrder` | Command | `PUT /api/sports/{id}/display-order` | Admin | Non-negative order. |
| [x] | `DeactivateSport` | Command | `POST /api/sports/{id}/deactivate` | Admin | Soft deactivate — never hard-delete; events keep FK Restrict. |
| [x] | `ActivateSport` | Command | `POST /api/sports/{id}/activate` | Admin | Re-enable selection. |

---

## Rules

- Clients pick sports only when `Sport.CanBeUsed()` / `IsActive`.
- Do not delete sports that are referenced by events or user sports.

---

## Exit criteria

- [x] Active sports list available to the app
- [x] Seed data covers launch catalog
- [x] Admin mutations live behind `Authorization:AdminUserIds`
