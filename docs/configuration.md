# Local Configuration

Tracked `appsettings.Development.json` / `appsettings.Production.json` hold **non-secret** defaults only
(issuer/audience, logging, CORS, moderator allow-list placeholders, bucket name).

## Local (user-secrets)

```powershell
dotnet user-secrets set "ConnectionStrings:SupabaseConnection" "<connection-string>" --project src/API
dotnet user-secrets set "Supabase:Url" "<supabase-url>" --project src/API
dotnet user-secrets set "Supabase:ServiceRoleKey" "<service-role-key>" --project src/API
dotnet user-secrets set "JwtSettings:Secret" "<jwt-secret>" --project src/API
```

Optional (also supported via tracked appsettings):

```powershell
dotnet user-secrets set "JwtSettings:Issuer" "SportnerApi" --project src/API
dotnet user-secrets set "JwtSettings:Audience" "SportnerMobile" --project src/API
dotnet user-secrets set "JwtSettings:ExpirationDays" "7" --project src/API
dotnet user-secrets set "Supabase:AvatarsBucket" "avatars" --project src/API
```

List:

```powershell
dotnet user-secrets list --project src/API
```

## CI / production (environment variables)

```text
ConnectionStrings__SupabaseConnection
Supabase__Url
Supabase__ServiceRoleKey
JwtSettings__Secret
```

## Rotate

Credentials that were previously committed to Git must be **rotated** in Supabase / JWT config.
Removing them from the working tree does not remove them from git history.

See also: [docs/roadmap/01-ops-security.md](roadmap/01-ops-security.md), [docs/ops/supabase-rls.md](ops/supabase-rls.md).
