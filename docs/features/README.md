# Features & Controllers Roadmap

This folder is the implementation backlog for Application (CQRS) and API (controllers)
after Domain + persistence are complete.

Authoritative engineering rules remain in [`.cursor/rules/`](../../.cursor/rules).
Per-table invariants remain in [`docs/database/`](../database/).
If anything here conflicts with those sources, the rules and database specs win.

---

## Current status (2026-08)

| Layer | Status |
| ----- | ------ |
| Domain (26 entities) | Done |
| Persistence + `InitialCreate` | Done (apply DB + RLS hardening if not yet) |
| Application foundation (CQRS contracts, Result, validation pipeline) | Done |
| Feature folders / use cases | Done for 01–09 (Identity → Moderation) |
| Controllers | Done for 01–09 |
| Realtime (SignalR) / background jobs | Deferred (see [10-cross-cutting.md](10-cross-cutting.md)) |

MVP feature backlog through Moderation is implemented. Remaining work is [10-cross-cutting.md](10-cross-cutting.md) (jobs, SignalR, policies hardening, etc.).

---

## Locked decisions

- **Auth:** Phone + OTP + JWT + Refresh Token. Refresh tokens live only in `UserSessions` (hashed). Application owns `IOtpService` / `IJwtService` abstractions; Infrastructure implements them.
- **Supabase:** Postgres + Storage. Clients do not use the Supabase Data API against business tables (RLS enabled, no public policies; prefer Data API off for backend-only).
- **Messaging v1:** Event conversations only. Direct / Group remain schema-reserved.
- **No generic repository / no separate Unit of Work.** Handlers use `IApplicationDbContext`.
- **Controllers stay thin:** receive request → MediatR → map `Result` to HTTP. No business logic, no validators in controllers, no direct DbContext.

---

## Slice shape (every use case)

```text
src/Application/Features/{Module}/{UseCase}/
  {Name}Command.cs | {Name}Query.cs
  {Name}CommandHandler.cs | {Name}QueryHandler.cs
  {Name}CommandValidator.cs   (FluentValidation)
  {Name}Response.cs           (when needed)
```

```text
src/API/Controllers/{Resource}Controller.cs
  → ISender.Send(...)
  → ProblemDetails / typed response from Result
```

Order inside a slice:

1. Domain method already exists (or extend Domain only if the invariant is missing).
2. Command / Query + Validator + Handler.
3. Thin controller endpoint.
4. Tests for the handler (and critical domain path if new).

Never ship a controller before its Application handler.

---

## Implementation order (do not skip)

```text
00 Prerequisites (seed, auth/storage infra, API conventions)
        ↓
01 Identity (auth, profile, devices, sessions, sports prefs, locations)
        ↓
02 Catalog (active sports list; admin write is minimal / seed-first)
        ↓
03 Events (lifecycle, participants, waitlist, attendance)
        ↓
04 Messaging (event chat REST)
        ↓
05 Reviews
        ↓
06 Social (friendships, posts, feed)
        ↓
07 Notifications (inbox + settings; most creates are side effects)
        ↓
08 Gamification (badge reads + award hooks)
        ↓
09 Moderation (reports)
        ↓
10 Cross-cutting backlog (jobs, SignalR, counters reconciliation)
```

Finish a module file’s checkboxes before starting the next, unless a dependency forces a tiny prerequisite slice (documented in that module).

---

## Module index

| Doc | Controllers (planned) |
| --- | --------------------- |
| [00-prerequisites.md](00-prerequisites.md) | — (infra + seed) |
| [01-identity.md](01-identity.md) | `Auth`, `Profiles`, devices/sessions/locations/sports |
| [02-catalog.md](02-catalog.md) | `Sports` |
| [03-events.md](03-events.md) | `Events` (+ nested participant/waitlist actions) |
| [04-messaging.md](04-messaging.md) | `Conversations`, `Messages` |
| [05-reviews.md](05-reviews.md) | `Reviews` |
| [06-social.md](06-social.md) | `Friendships`, `Posts`, `Comments` (+ feed queries) |
| [07-notifications.md](07-notifications.md) | `Notifications`, `NotificationSettings` |
| [08-gamification.md](08-gamification.md) | `Badges` |
| [09-moderation.md](09-moderation.md) | `Reports` |
| [10-cross-cutting.md](10-cross-cutting.md) | — (policies, jobs, realtime) |

---

## Progress checklist

- [ ] 00 Prerequisites
- [x] 01 Identity
- [x] 02 Catalog
- [x] 03 Events
- [x] 04 Messaging
- [x] 05 Reviews
- [x] 06 Social
- [x] 07 Notifications
- [x] 08 Gamification
- [x] 09 Moderation
- [x] 10 Cross-cutting (partial: policies, counters, storage cleanup; jobs/SignalR/secrets deferred)

---

## Definition of Done (one slice)

A feature slice is done only when:

- [ ] Builds with no new warnings tied to the change
- [ ] Follows Clean Architecture dependency direction
- [ ] Uses MediatR + FluentValidation + Result
- [ ] Mapster mappings stay in Application
- [ ] Expected failures return typed `Result` errors (not thrown for business cases)
- [ ] Authorization applied where required (`[Authorize]` / policies)
- [ ] Cached counters updated in the same unit of work when the slice mutates them
- [ ] No secrets logged (OTP, JWT, refresh tokens)
- [ ] Handler tests cover happy path + key invariant failures

---

## Explicitly deferred (not MVP blockers)

- SignalR hubs (chat push, typing, presence)
- Push / email delivery workers
- Direct / Group DM factories
- `MessageType.Location` factory
- PostGIS / advanced geo search
- Full admin CRUD UIs for Sports / Badges / ReportReasons (seed first)
- Phase 10 deep performance / security test suites
