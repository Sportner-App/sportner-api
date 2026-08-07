# 00 — Prerequisites

Complete these before Identity controllers. They unblock every later module.

Related rules: [IMPLEMENTATION_WORKFLOW.md](../../.cursor/rules/IMPLEMENTATION_WORKFLOW.md) Phase 4–5,
[BACKEND_STANDARDS.mdc](../../.cursor/rules/BACKEND_STANDARDS.mdc) §12 / §19,
[docs/configuration.md](../configuration.md).

---

## Progress

- [ ] Database applied (`Update-Database` / `dotnet ef database update`) — your action
- [ ] Supabase RLS + Data API hardening — your action (dashboard)
- [x] Seed: Sports, Badges, ReportReasons — idempotent `DatabaseSeeder`, runs at startup
- [x] Auth infrastructure contracts + JWT wiring — `IJwtService`, `IOtpService`, `ITokenHasher`, `ISmsSender` + JWT bearer
- [x] Storage abstraction — `IFileStorage` + `SupabaseFileStorage`
- [x] API conventions (pagination, Result → HTTP) — `PagedResult`/`CursorPagedResult`, `ApiResults`, `ApiControllerBase`

---

## 1. Database apply

Migration: `src/Infrastructure/Persistence/Migrations/20260805142434_InitialCreate.cs`

PMC (Default project = Infrastructure, Startup = API):

```powershell
Update-Database -Context AppDbContext
```

CLI:

```powershell
dotnet ef database update --project src/Infrastructure/Sportner.Infrastructure.csproj --startup-project src/API/Sportner.API.csproj --context AppDbContext
```

Confirm connection string targets the intended Supabase project before running.

---

## 2. Supabase security (backend-only)

Business tables must not be writable via the public Data API.

Recommended (SQL Editor):

1. `ENABLE ROW LEVEL SECURITY` on every `public` table (no policies for `anon` / `authenticated`).
2. `REVOKE ALL ... FROM anon, authenticated` on those tables.
3. Optionally disable Data API entirely under Integrations → Data API.

Backend continues via Npgsql connection string (`postgres` / pooler user). Table owner bypasses RLS.

Document any project-specific SQL under ops notes if you keep a runbook; do not embed secrets.

---

## 3. Seed data

Seed once after migration. Prefer an idempotent Infrastructure seeder (upsert by unique `Code` / `Slug` / `Name`).

### Sports (`docs/database/03-sports.md`)

At least: Basketball, Football, Volleyball, Tennis, Table Tennis, Running, Cycling, Swimming, Fitness, Hiking, Boxing, Pilates, Yoga, CrossFit, Badminton.

Each row: `Name`, URL-safe `Slug`, `DisplayOrder`, `IsActive = true`, optional `IconUrl`.

### Badges (`BadgeCodes` + `docs/database/23-badges.md`)

| Code | Purpose |
| ---- | ------- |
| `FIRST_EVENT` | First completed/attended event milestone |
| `FIRST_POST` | First published post |
| `FIRST_FRIEND` | First accepted friendship |
| `FIRST_REVIEW` | First review written |
| `COMMUNITY_HELPER` | Community contribution rule (define threshold in award hook) |
| `SPORTS_EXPLORER` | Multiple sports explored |
| `EVENT_MASTER` | High event participation |
| `MARATHON_RUNNER` | Streak / volume rule |

Fields: name, description, icon path, category, rarity, XP, display order, `IsActive`.

### Report reasons (`ReportReasonCodes` + `docs/database/26-report-reasons.md`)

`SPAM`, `HARASSMENT`, `HATE_SPEECH`, `INAPPROPRIATE_CONTENT`, `VIOLENCE`, `NUDITY`, `FAKE_INFORMATION`, `IMPERSONATION`, `SCAM`, `OTHER`.

---

## 4. Auth infrastructure (Application contracts)

Add abstractions under `src/Application/Abstractions/Authentication/` (or adjacent):

| Contract | Responsibility |
| -------- | -------------- |
| `IOtpService` | Generate, store (hashed/short-TTL), send, verify OTP for a phone number. Never log OTP. |
| `IJwtService` | Issue access token (claims: `sub` = user id). |
| `IRefreshTokenService` or helpers on session flow | Generate refresh token plaintext once; persist **hash only** on `UserSession`. |
| `IPasswordOrTokenHasher` (name as fits) | Hash OTP / refresh tokens consistently. |

Infrastructure:

- OTP delivery: SMS provider behind `IOtpService` (dev can use a fixed/logged-at-debug-disabled stub that still never writes OTP to Serilog).
- JWT options from `JwtSettings` (user-secrets / env — see [configuration.md](../configuration.md)).
- Register JWT bearer authentication in API so `[Authorize]` works with `ICurrentUser`.

Domain already owns: `User.Create`, `VerifyPhoneNumber`, `Activate`, `CreateSession`, `RevokeSession`, `RegisterDevice`, `CanAuthenticate`.

---

## 5. Storage infrastructure

| Contract | Responsibility |
| -------- | -------------- |
| `IFileStorage` | Upload / delete / get public or signed URL for paths stored on Profile, PostMedia, Message, Badge icon, etc. |

Implementation: Supabase Storage using `Supabase:Url` + service role (server-only). Database stores **paths only**.

---

## 6. API conventions (shared)

### Routing

- Controllers: kebab-case routes (project already has `KebabCaseParameterTransformer`).
- Prefer resource-oriented URLs: `/api/auth/...`, `/api/profiles/me`, `/api/events/{id}/...`.

### Pagination

- Collection queries: cursor or page+size as documented per endpoint; never unbounded lists.
- Events discovery, feeds, notifications, messages: cursor (`createdAt` + `id`) preferred.

### Result → HTTP

Map `ErrorType` consistently in a shared filter/extension (align with existing `GlobalExceptionHandler` / ProblemDetails):

| ErrorType (concept) | HTTP |
| ------------------- | ---- |
| Validation | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| Failure / unexpected | 500 (prefer exceptions for unexpected) |

Business outcomes use `Result` / `Result<T>`; do not throw for expected domain refusals.

### Localization

User-facing messages from `ValidationResource` (`en-US` / `tr-TR`). Stable error **codes** for clients.

---

## 7. Feature folder convention reminder

```text
Application/Features/Identity/RequestOtp/
Application/Features/Identity/VerifyOtp/
...
```

Module names in folders should match this roadmap (`Identity`, `Events`, `Messaging`, …).

---

## Exit criteria

Prerequisites are done when:

- [ ] Schema exists on the target database
- [ ] RLS / revoke (or Data API off) verified in dashboard
- [ ] Seed rows present and idempotent re-run safe
- [ ] OTP + JWT + refresh round-trip works in a smoke test (even before full profile UI)
- [ ] `IFileStorage` registered (upload can wait for Profile avatar slice)
- [ ] Team agrees Result→HTTP mapping is centralized
