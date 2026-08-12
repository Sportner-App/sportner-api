# Sportner API ÔÇö Agent Guide

> This file is intentionally short. The authoritative engineering rules live in
> [`.cursor/rules/`](.cursor/rules) and the per-table specs live in
> [`docs/database/`](docs/database). Read those before implementing anything.
> If anything here ever conflicts with `.cursor/rules/`, the rules win.

## Source of truth (read these first)

- [`.cursor/rules/BACKEND_STANDARDS.mdc`](.cursor/rules/BACKEND_STANDARDS.mdc) ÔÇö architecture & coding standards (highest priority)
- [`.cursor/rules/DATABASE_STANDARDS.md`](.cursor/rules/DATABASE_STANDARDS.md) ÔÇö database & EF Core mapping standards
- [`.cursor/rules/IMPLEMENTATION_WORKFLOW.md`](.cursor/rules/IMPLEMENTATION_WORKFLOW.md) ÔÇö phase/order workflow
- [`docs/database/*.md`](docs/database) ÔÇö one spec per table (columns, defaults, indexes, FKs, business rules)
- [`docs/database/database-reference.md`](docs/database/database-reference.md) & [`database-erd.md`](docs/database/database-erd.md)
- [`docs/features/`](docs/features) ÔÇö CQRS features & controllers roadmap (implement in listed order)

## Fixed architecture (from the rules ÔÇö do not replace)

Clean Architecture ┬À **CQRS + MediatR** ┬À **Mapster** ┬À **FluentValidation** ┬À **Result Pattern** ┬À
Global Exception Middleware ┬À **EF Core convention-first persistence** ┬À PostgreSQL/Supabase ┬À
UUID PKs (`Guid.NewGuid()`) ┬À `DateTimeOffset`/TIMESTAMPTZ ┬À SMALLINT enums ┬À no soft delete ┬À
`IApplicationDbContext` persistence boundary ┬À no separate Unit of Work ┬À no generic repository
(repositories only for justified domain-specific queries).

Layers: `API ÔåÆ Application ÔåÆ Domain` and `Infrastructure ÔåÆ Application ÔåÆ Domain`. Domain has no external deps.

## Current build state (2026-08)

- **Domain layer:** complete for all 26 tables and audited (see [`docs/audits/019-domain-model-audit.md`](docs/audits/019-domain-model-audit.md)).
- **Architecture foundation:** CQRS contracts, typed Result, localized global exception handling, current-user abstraction, auditing interceptor and Serilog are wired.
- **Persistence:** all 26 entities are registered convention-first. Add explicit Fluent API only when a documented invariant cannot be represented by conventions.
- **Migration:** `InitialCreate` is generated and reviewed; apply to the target DB and harden Supabase RLS before feature work if not already done (see [`docs/audits/020-architecture-and-persistence-readiness.md`](docs/audits/020-architecture-and-persistence-readiness.md) and [`docs/features/00-prerequisites.md`](docs/features/00-prerequisites.md)).
- **Application (CQRS features), Controllers:** not started yet. Follow the step-by-step backlog in [`docs/features/README.md`](docs/features/README.md) ÔÇö prerequisites ÔåÆ Identity ÔåÆ Catalog ÔåÆ Events ÔåÆ Messaging ÔåÆ Reviews ÔåÆ Social ÔåÆ Notifications ÔåÆ Gamification ÔåÆ Moderation.

## Run

```bash
dotnet build Sportner.slnx
dotnet run --project src/API/Sportner.API.csproj
dotnet run --project src/Workers/Identity.Worker/Sportner.Identity.Worker.csproj
dotnet run --project src/Workers/Events.Worker/Sportner.Events.Worker.csproj
dotnet run --project src/Workers/Notifications.Worker/Sportner.Notifications.Worker.csproj
```

Workers are purpose-split deployable hosts (Atmaca-style). Shared cron helpers: `Sportner.Workers.Hosting`.  
Job logic stays in Application (`BackgroundJobs:*` config).  
Config shape matches API for every host: `appsettings.json` + `Development` + `Production` (see [`docs/configuration.md`](docs/configuration.md)).
