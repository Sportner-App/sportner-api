# Render → Railway migration

**Decision:** hosting (API + 3 workers) moves from Render to Railway. Database stays on
Supabase — nothing changes on the DB side, only where the containers run.

This is prep for a manual cutover; nothing here changes local dev or the current Render
deployment. `docker-compose.yml` and the Dockerfiles are untouched.

---

## Services to create (one Railway project, 4 services)

**Config as Code (`railway.json`/`railway.toml`) is deprecated on Railway** — services that
have never used it can no longer opt in as of 2026-08-28, and it's replaced by a CLI-driven
Infrastructure-as-Code flow (`.railway/*.ts` + `railway config plan`). That's more machinery
than this migration needs, so every service below is configured directly in its **Settings**
tab instead — no config file in the repo.

| Service | Root directory | Dockerfile path (Settings → Build) | Public domain? | Healthcheck (Settings → Deploy) |
| ------- | --------------- | ---------------------------------- | --------------- | -------------------------------- |
| API | `/` (repo root) | `Dockerfile` | Yes | `/health/live` |
| Events.Worker | `/` (repo root) | `src/Workers/Events.Worker/Dockerfile` | No | — |
| Identity.Worker | `/` (repo root) | `src/Workers/Identity.Worker/Dockerfile` | No | — |
| Notifications.Worker | `/` (repo root) | `src/Workers/Notifications.Worker/Dockerfile` | No | — |

Root directory must stay the repo root for **every** service — each Dockerfile's `COPY`
instructions assume a repo-root build context (`COPY src/Domain/ ...`, etc.), exactly like
the existing Render setup. Only the Dockerfile Path changes per service (set it under
Settings → Build → Builder = Dockerfile → Dockerfile Path).

---

## Environment variables

`ASPNETCORE_ENVIRONMENT=Production` (API) / `DOTNET_ENVIRONMENT=Production` +
`ASPNETCORE_ENVIRONMENT=Production` (workers) are already baked into each Dockerfile's
`ENV`, so `appsettings.Production.json` loads automatically — per the existing project rule
([configuration.md](../configuration.md)), that file already carries the real Supabase
connection string, Supabase keys, and JWT secret, so **no env vars need to be added in
Railway at all** for a like-for-like migration. Confirmed live: all 4 services run with
zero custom variables.

**Do NOT set `ASPNETCORE_URLS` on the API service.** The Dockerfile already `EXPOSE`s
8080, Railway auto-detects that and points the public domain at container port 8080 —
this service never gets a dynamic `PORT` injected. Setting
`ASPNETCORE_URLS=http://+:${{PORT}}` resolves `${{PORT}}` to empty (no such variable
exists on this service), which made Kestrel fall back to port 80 while the domain still
proxied to 8080 — 100% healthcheck failure. If you ever see this mismatch, the fix is to
delete the variable, not to chase the port number.

**Watch for auto-suggested/auto-added placeholder variables.** Railway sometimes surfaces
variables like `Supabase__ServiceRoleKey`, `Supabase__Url`, `ConnectionStrings__SupabaseConnection`,
`JwtSettings__Secret` with obviously-fake values (`YOUR_HOST`, `YOUR_SERVICE_ROLE_KEY`,
`YOUR_JWT_SECRET_AT_LEAST_32_CHARS`, ...). If any of these get **added** (not just
suggested) to a service, they silently override the real values already committed in
`appsettings.Production.json` (env vars always win) and break the DB connection at
startup — the app crashes during EF Core migrations before Kestrel ever binds, so it
shows up as a healthcheck timeout with no obvious cause. Delete any such variable if
found; the "Suggested Variables" panel Railway shows when opening a fresh service's
Variables tab (matching the `ENV` lines already in the Dockerfile, e.g.
`DOTNET_EnableDiagnostics=0`) is harmless to leave un-added — it's redundant with the
Dockerfile, not required.

---

## Cutover checklist (completed 2026-09-14)

1. ✅ Created Railway project `zonal-warmth` (environment `production`), added all 4
   services from `Sportner-App/sportner-api` (GitHub-connected, root directory `/` for
   every service).
2. ✅ Set each service's **Dockerfile Path** under Settings → Build (Builder switched
   from the Railpack default to Dockerfile first): `Dockerfile` (api),
   `src/Workers/Events.Worker/Dockerfile`, `src/Workers/Identity.Worker/Dockerfile`,
   `src/Workers/Notifications.Worker/Dockerfile`.
3. ✅ Set API's Healthcheck Path to `/health/live` (Settings → Deploy). Workers have none
   (they don't serve HTTP).
4. ✅ No custom variables added on any service — deleted placeholder Supabase/JWT vars
   that had been auto-added on the API service, and did not add `ASPNETCORE_URLS`.
5. ✅ Verified: `https://api-production-9004e.up.railway.app/health/ready` and
   `/health/live` both return `200 Healthy`; each worker's deploy logs show its startup
   line (Events: auto-complete + recurring series + reminders + marathon badge sweep +
   attendance auto-confirm; Identity: session cleanup; Notifications: push delivery
   outbox) with no DB/migration errors.

Remaining (not yet done):

- Point the mobile app's API base URL at `https://api-production-9004e.up.railway.app`.
- Decommission the Render services after a burn-in period.

No changes to Supabase, connection strings, or the mobile app's data layer — only the compute host moved.
