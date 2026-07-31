# Sportner API

.NET 10 Clean Architecture API for the Sportner mobile app.

## Solution structure

```
Sportner.slnx
src/
  Domain/           Entities, enums, exceptions, repository contracts
  Application/      Services, DTOs, validators, mappers
  Infrastructure/   EF Core, repositories, UoW, JWT/Expo/Supabase clients
  API/              Controllers, GlobalExceptionHandler, Program
  Localization/     ValidationResource (en / tr)
tests/
  Domain.UnitTests/
  Application.UnitTests/
  API.IntegrationTests/
```

## Prerequisites

- .NET 10 SDK
- PostgreSQL (Supabase) connection
- Supabase Storage service role key (avatars)

## Configure secrets

Do **not** commit real secrets. Use user-secrets or environment variables:

```bash
cd src/API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SupabaseConnection" "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "JwtSettings:Secret" "<long-random-secret>"
dotnet user-secrets set "Supabase:Url" "https://YOUR_PROJECT.supabase.co"
dotnet user-secrets set "Supabase:ServiceRoleKey" "<service-role-key>"
```

If secrets were previously committed, rotate DB password, JWT secret, and Supabase service role key.

## Run

```bash
dotnet run --project src/API/Sportner.API.csproj --launch-profile Development
dotnet run --project src/API/Sportner.API.csproj --launch-profile Production
```

- Development: `http://localhost:5139` + Swagger (`/swagger`) with **Authorize** (Bearer JWT). Browser auto-open kapalı; projeyi durdurunca ölü sekme kalmasın diye manuel aç: `http://localhost:5139/swagger`
- Production profile: same port, no Swagger UI, uses `appsettings.Production.json` (+ env overrides)
- Live health: `GET /health/live`
- Ready health: `GET /health/ready`

## Docker

```bash
cp .env.example .env
# fill secrets in .env

docker compose up --build
```

API: `http://localhost:5139` (container listens on `8080`, mapped to host `5139`).

Build image only:

```bash
docker build -t sportner-api:local .
```

## Migrations

Schema is owned by EF Core migrations under `src/Infrastructure/Persistence/Migrations`.

```bash
dotnet ef database update --project src/Infrastructure/Sportner.Infrastructure.csproj --startup-project src/API/Sportner.API.csproj
```

Against an existing Supabase schema, review the migration before applying (unique indexes may fail if duplicate rows exist).

## Localization

Send `Accept-Language: tr-TR` or `en-US`. Default UI culture is `tr-TR`; data culture stays `en-US`. Messages come from strongly-typed `ValidationResource` (`.resx` + `.Designer.cs`).

## Tests

```bash
dotnet test Sportner.slnx
```

## API contract

Mobile JSON shapes and routes are documented in [API_DOCUMENTATION.md](API_DOCUMENTATION.md). Success payloads stay unwrapped; errors use `{ "message": "..." }`.
