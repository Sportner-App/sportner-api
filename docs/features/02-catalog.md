# 02 — Catalog (Sports)

Table: `Sports`. Domain: `src/Domain/Sports/Sport.cs`. Spec: `docs/database/03-sports.md`.

Depends on: seed from [00-prerequisites.md](00-prerequisites.md). Identity auth for protected writes.

---

## Progress

- [ ] List active sports (client)
- [ ] Admin activate/deactivate/reorder (minimal; seed covers v1 content)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `SportsController` | `/api/sports` |

---

## Features

| Status | Use case | Type | Endpoint | Auth | Domain / notes |
| ------ | -------- | ---- | -------- | ---- | -------------- |
| [ ] | `ListActiveSports` | Query | `GET /api/sports` | Anonymous or Authorize (product choice: prefer Authorize after login) | `IsActive` only; order by `DisplayOrder`. |
| [ ] | `GetSportBySlug` | Query | `GET /api/sports/{slug}` | Same | Optional. |
| [ ] | `CreateSport` | Command | `POST /api/sports` | Admin policy | `Sport.Create`. Seed usually enough for v1. |
| [ ] | `RenameSport` | Command | `PUT /api/sports/{id}` | Admin | |
| [ ] | `ChangeSportDisplayOrder` | Command | `PUT /api/sports/{id}/display-order` | Admin | |
| [ ] | `DeactivateSport` | Command | `POST /api/sports/{id}/deactivate` | Admin | Never hard-delete; events keep FK Restrict. |
| [ ] | `ActivateSport` | Command | `POST /api/sports/{id}/activate` | Admin | |

---

## Rules

- Clients pick sports only when `Sport.CanBeUsed()` / `IsActive`.
- Do not delete sports that are referenced by events or user sports.

---

## Exit criteria

- [ ] Active sports list available to the app
- [ ] Seed data covers launch catalog
- [ ] Admin mutations optional for MVP if seed is sufficient (mark admin rows deferred explicitly if skipped)
