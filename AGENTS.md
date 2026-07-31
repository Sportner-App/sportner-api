# Sportner API — Agent & Development Guide

This document is the source of truth for how we build and change Sportner API after the Clean Architecture modernization. **Read it before implementing features or fixes.**

## Stack

- .NET 10 (`net10.0`)
- Solution: `Sportner.slnx`
- PostgreSQL (Supabase) via EF Core + Npgsql
- JWT Bearer auth, Expo push, Supabase Storage
- FluentValidation, ValidationResource (en/tr), Swashbuckle JWT Authorize

## Solution layout

```
src/
  Domain/           Entities (User, UserEvent, Event, …), enums, ApiException, IUnitOfWork / repo contracts, ICurrentUser
  Application/      Services, DTOs, validators, mappers, helpers, abstractions (IToken/INotification/IStorage)
  Infrastructure/   SportnerDbContext, Persistence/Configurations, repositories, UoW, transformers, migrations, AddInfrastructure
  API/              Controllers, Program, GlobalExceptionHandler, Extensions (Collection/Auth/Cors/RateLimiting/Health/Swagger/Localization)
  Localization/     ValidationResource.resx + .en.resx + .tr.resx + Designer.cs
tests/
  Domain.UnitTests / Application.UnitTests / API.IntegrationTests
```

**Dependency rule:** API → Application, Infrastructure, Localization · Infrastructure → Application, Domain · Application → Domain, Localization · Domain has no EF package.

## Patterns (required)

| Area | Rule |
|------|------|
| Controllers | Thin: call Application services only. No `DbContext`, no business rules, no try/catch |
| Business logic | Application `*Service` classes |
| Data access | `IUnitOfWork` + repositories. **SaveChanges only on UoW**. Do not call `UpdateOne` on already-tracked entities after `Find*` — mutate properties then `SaveChangesAsync` |
| Errors | Throw `ApiException(HttpStatusCode, ValidationResource....)`. `GlobalExceptionHandler` maps to `{ "message": "..." }` |
| Validation | FluentValidation in Application; filter in API. Messages from `ValidationResource` |
| Localization | TMS-style `ValidationResource` + `Accept-Language` (`tr-TR` default UI culture, `en-US` supported; data culture `en-US`). No hardcoded user-facing strings |
| Mapping | Manual static mappers (no AutoMapper) |
| Auth identity | JWT `sub` + `NameIdentifier`; resolve via `ICurrentUser` only |
| Routes | Kebab-case via `KebabCaseParameterTransformer` on `[controller]` tokens (e.g. `UsersController` → `/api/users`). Prefer lowercase hardcoded route segments |
| JSON | camelCase property names (`JsonNamingPolicy.CamelCase` + Newtonsoft `CamelCasePropertyNamesContractResolver`) |
| API contract | Keep mobile routes/DTO shapes stable unless explicitly versioning. Errors stay `{ "message" }` |
| Naming | Domain: `User` / `UserEvent` / `UserEventStatus`. Physical tables stay `profiles` / `event_participants` |
| Privacy | `pushToken` only on `GET /api/users/me`, never on other users’ profiles |
| Swagger | Development only; Bearer Authorize via `AddCustomSwagger` (wired from `AddCustomCollection`) |
| Config | Secrets in `appsettings.Development.json` / user-secrets / env. Production placeholders + env overrides |
| New endpoints | Controller → service interface/impl → repo/UoW if needed → validator + ValidationResource keys (en+tr) |

## Do not use (unless team explicitly decides later)

- MediatR / CQRS
- AutoMapper
- Fat controllers
- Per-action try/catch (use GlobalExceptionHandler)
- DbContext injected into API/Application services (only Infrastructure)
- Committing Production secrets

## Error & status conventions

- Business/validation failures → `ApiException` with proper HTTP status (400/401/403/404…)
- Login invalid credentials → **400** (mobile contract)
- Unhandled exceptions → 500; in Development response may include exception message for debugging
- Auth login/register → rate limit policy `auth`

## Adding a feature (checklist)

1. Domain entity/enum/repo interface if needed
2. Application DTO + FluentValidation + ValidationResource keys (`.resx`, `.en.resx`, `.tr.resx`, Designer property)
3. Application service method
4. Thin API controller action + ProducesResponseType
5. Unit test for rules where meaningful
6. Update `API_DOCUMENTATION.md` if contract visible to mobile

## Run

```bash
dotnet run --project src/API/Sportner.API.csproj --launch-profile Development
# Swagger: http://localhost:5139/swagger
```
