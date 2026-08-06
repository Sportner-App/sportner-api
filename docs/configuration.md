# Local Configuration

Pre-deployment development currently reads database, JWT and Supabase values from
`appsettings.Development.json` / `appsettings.Production.json`.

Before deployment or repository sharing, do not keep database passwords, JWT secrets or
Supabase service-role keys in tracked files. Move them to the mechanisms below and rotate
the currently exposed values.

The API project is configured for .NET user-secrets. Set local values with:

```powershell
dotnet user-secrets set "ConnectionStrings:SupabaseConnection" "<connection-string>" --project src/API
dotnet user-secrets set "Supabase:Url" "<supabase-url>" --project src/API
dotnet user-secrets set "Supabase:ServiceRoleKey" "<service-role-key>" --project src/API
dotnet user-secrets set "JwtSettings:Secret" "<jwt-secret>" --project src/API
```

CI and production must provide the same keys through environment variables or a managed
secret store. Use double underscores for nested environment keys, for example:

```text
ConnectionStrings__SupabaseConnection
Supabase__ServiceRoleKey
JwtSettings__Secret
```

Credentials previously committed to Git must be rotated; replacing the current file does
not remove secrets from repository history.
