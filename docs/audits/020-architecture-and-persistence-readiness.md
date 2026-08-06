# Architecture and Persistence Readiness

Date: 2026-08-05

## Locked architecture

- Clean Architecture: API → Application → Domain; Infrastructure → Application → Domain.
- CQRS through MediatR.
- Commands use tracked entities and persist once through `IApplicationDbContext.SaveChangesAsync`.
- Queries use projection and `AsNoTracking`; collection queries must be paginated.
- FluentValidation runs in the MediatR validation pipeline.
- Expected business outcomes use typed `Result` / `Result<T>` errors.
- `ApiException`, `DomainException`, validation failures and unexpected failures are handled centrally.
- User-facing error messages come from `ValidationResource` (`en-US` / `tr-TR`).
- Mapster mappings remain in Application.
- EF Core is convention-first with plural `DbSet<TEntity>` registrations.
- Aggregate-owned relationships use navigation properties. Cross-aggregate references use
  identifiers plus explicit database FKs and are read through CQRS projections/joins.
- `AppDbContext` is the Unit of Work. Do not add a separate `IUnitOfWork`.
- Do not add a generic repository. Add a domain-specific repository only for a justified,
  reusable domain query that is clearer behind a dedicated contract.
- Auditing is applied by `AuditableEntityInterceptor` using `TimeProvider` and `ICurrentUser`.
- Serilog provides structured application and HTTP request logging without logging secrets.

## Completed foundation

- All 26 concrete Domain entities are registered on `AppDbContext`.
- Automated tests verify every Domain entity has a `DbSet` and can enter the EF model.
- `IApplicationDbContext` is owned by Application and implemented by Infrastructure.
- Command/query and handler contracts are in place.
- Result errors carry stable code, localized message and semantic error type.
- Global exception handling returns RFC 7807 `ProblemDetails` with `traceId`.
- `ICurrentUser` resolves `NameIdentifier` or JWT `sub`.
- The obsolete recurrence fields were removed from `Event` to match `09-events.md`.
- `BadgeCodes` and `ReportReasonCodes` provide permanent typed identifiers.
- All documented unique constraints are configured centrally with minimal Fluent API.
- Documented relationships and delete behaviors are configured without adding cross-aggregate
  navigation properties to the Domain model.
- `Profile.UsernameChangedAt` is persisted so the 30-day username rule survives rehydration.
- Documented query indexes, decimal precision, constrained text lengths, database defaults and
  SMALLINT columns are configured centrally.
- Automated persistence tests verify entity registration, unique constraints and property facets.
- `dotnet-ef` and EF runtime are aligned at 10.0.10.

## Initial migration readiness

Convention-first does not mean “accept any generated schema.” Review the generated EF model
against every `docs/database/NN-*.md` specification and add only the minimal Fluent API needed
for invariants conventions cannot express.

That model review is complete. The next persistence step is to generate the initial migration,
inspect its SQL/model snapshot, and only then apply it to a database.

## Initial migration review

`InitialCreate` was generated on 2026-08-05 and has not been applied to a database.

- 26 tables
- 99 indexes
- 22 unique indexes
- 41 foreign keys
- 15 cascade delete actions
- 26 restrict delete actions

The migration contains the documented VARCHAR limits, numeric precision, SMALLINT columns,
cached-counter defaults and persisted `Profile.UsernameChangedAt`. Build, tests and design-time
model creation pass after generation.

## Security action required

Development and Production configuration currently contain database, JWT and Supabase secrets
in tracked files by an explicit pre-deployment decision. Before any deployment or repository
sharing, move them to environment variables/user-secrets and rotate every exposed credential.
