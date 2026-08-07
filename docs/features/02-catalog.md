# 02 — Catalog (Sports)

Table: `Sports`. Domain: `src/Domain/Sports/Sport.cs`. Spec: `docs/database/03-sports.md`.

Depends on: seed from [00-prerequisites.md](00-prerequisites.md). Identity auth for protected writes.

---

## Progress

- [x] List active sports (client) + get by slug
- [~] Admin activate/deactivate/reorder — **deferred** (no admin authorization yet; seed covers the v1 catalog)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `SportsController` | `/api/sports` |

---

## Features

| Status | Use case | Type | Endpoint | Auth | Domain / notes |
| ------ | -------- | ---- | -------- | ---- | -------------- |
| [x] | `ListActiveSports` | Query | `GET /api/sports` | `[Authorize]` | `IsActive` only; order by `DisplayOrder`. |
| [x] | `GetSportBySlug` | Query | `GET /api/sports/{slug}` | `[Authorize]` | Active only; slug lookup is case-insensitive. 404 when missing/inactive. |
| [~] | `CreateSport` | Command | `POST /api/sports` | Admin policy | **Deferred** — needs admin authorization. Seed covers v1. |
| [~] | `RenameSport` | Command | `PUT /api/sports/{id}` | Admin | **Deferred.** |
| [~] | `ChangeSportDisplayOrder` | Command | `PUT /api/sports/{id}/display-order` | Admin | **Deferred.** |
| [~] | `DeactivateSport` | Command | `POST /api/sports/{id}/deactivate` | Admin | **Deferred.** Never hard-delete; events keep FK Restrict. |
| [~] | `ActivateSport` | Command | `POST /api/sports/{id}/activate` | Admin | **Deferred.** |

---

## Rules

- Clients pick sports only when `Sport.CanBeUsed()` / `IsActive`.
- Do not delete sports that are referenced by events or user sports.

---

## Exit criteria

- [x] Active sports list available to the app
- [x] Seed data covers launch catalog
- [x] Admin mutations optional for MVP — deferred until an admin authorization module exists
