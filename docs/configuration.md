# Local Configuration

Tracked configuration files never contain database credentials, JWT secrets, or Supabase
keys. Use local user-secrets for development and environment variables for every deployed host.

## Host config shape (API + all Workers — keep in sync)

Every deployable host uses the same file layout:

```text
appsettings.json                 # shared defaults (no secrets)
appsettings.Development.json     # non-secret local defaults + host extras
appsettings.Production.json      # non-secret production defaults
```

| Host | Extra section |
| ---- | ------------- |
| API | `Cors`, `AllowedHosts`, `Recommendation` (heuristic weights; secret değil) |
| Identity.Worker | `BackgroundJobs` (session cleanup) |
| Events.Worker | `BackgroundJobs` (auto-complete + reminders + marathon badge) |
| Notifications.Worker | `BackgroundJobs` (push outbox) |

`Recommendation` defaults live in API `appsettings.json` (`People` / `Events` / `Posts` weights + candidate caps). Override via env (`Recommendation__People__MutualFriends=…`) if needed; no secrets.

Environment names:

- API Docker: `ASPNETCORE_ENVIRONMENT=Production`
- Workers Docker: `ASPNETCORE_ENVIRONMENT=Production` **and** `DOTNET_ENVIRONMENT=Production`

New worker / API host: copy this trio from an existing host; only change `BackgroundJobs` (or API-only keys). Do not invent a one-off config style.

The API project also supports .NET user-secrets (optional overlay):

```powershell
dotnet user-secrets set "ConnectionStrings:SupabaseConnection" "<connection-string>" --project src/API
dotnet user-secrets set "Supabase:Url" "<supabase-url>" --project src/API
dotnet user-secrets set "Supabase:ServiceRoleKey" "<service-role-key>" --project src/API
dotnet user-secrets set "JwtSettings:Secret" "<jwt-secret>" --project src/API
```

CI / production *may* override via environment variables:

```text
ConnectionStrings__SupabaseConnection
Supabase__ServiceRoleKey
JwtSettings__Secret
```

## Auth (username + password)

V1 uses username/password (`POST /api/auth/register`, `POST /api/auth/login`). No OTP/SMS/2FA.

Demo users (after seed / password backfill): username `ahmet` / `elif` / `mert` / `zeynep`, password `Demo123!`.

JWT settings remain under `JwtSettings`.
