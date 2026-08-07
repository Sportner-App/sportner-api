# Local Configuration

Pre-deployment / local development reads database, JWT and Supabase values from
`appsettings.Development.json` / `appsettings.Production.json`.

**Project rule (owner):** Do **not** strip connection strings, JWT secrets, or Supabase
keys from tracked appsettings unless the owner explicitly asks in that moment.
Keep local convenience config in place.

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

## OTP (temporary — until SMS provider)

Auth is phone OTP only (`POST /api/auth/request-otp` → `verify-otp`). There is no email/password.

**Current (dev + Production/Render):** fixed code `000000` when `Otp:ExposeCodeInLogs` is true.

```json
"Otp": {
  "ExposeCodeInLogs": true,
  "FixedCode": "000000"
}
```

UI flow: `request-otp` → `verify-otp` with `000000`.  
Before real launch: disable `ExposeCodeInLogs`, clear `FixedCode`, wire SMS.
