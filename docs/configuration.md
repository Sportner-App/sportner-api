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

Outside the Development environment the API **refuses to start** unless these
hold (see `AuthenticationExtension`):

```text
JwtSettings__Issuer      non-empty
JwtSettings__Audience    non-empty
JwtSettings__Secret      at least 32 bytes
```

An empty issuer or audience silently disables that check during token
validation, which would let a token minted by any other service through. The
secret floor matches HMAC-SHA256's 256-bit key size.

## Auth (username + password)

V1 uses username/password (`POST /api/auth/register`, `POST /api/auth/login`). No OTP/SMS/2FA.

Demo users (after seed / password backfill): username `ahmet` / `elif` / `mert` / `zeynep`, password `Demo123!`.

### Email delivery

Verification and password-reset codes go out through Resend. Outside
Development the API **refuses to start** without `Resend__ApiKey`: falling back
to the logging sender there would mean codes silently reach nobody.
`Resend__FromAddress` must use a domain verified in the Resend dashboard
(`sportner.app`).

`EmailVerification__StaticCode` is gone. It pinned every issued code to one
fixed value so friends could test without a verified domain — but password
reset shares the same generator, so one guessable code unlocked every account.
Delete the variable from any environment that still defines it.

JWT settings remain under `JwtSettings`.
