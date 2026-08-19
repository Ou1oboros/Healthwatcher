# HealthwatcherApi

A layered ASP.NET Core Web API skeleton — Domain / Application / Infrastructure / Presentation
inside a single project, with EF Core + PostgreSQL, and a test project covering every layer.

**.NET 10** (LTS, supported to November 2028) · EF Core 10 · Npgsql 10 · xUnit + NSubstitute.


## Layout

| Concern | Where |
|---|---|
| Entities, domain services, repository interfaces | `HealthwatcherApi/Domain/` |
| DTOs, application services, mappings, app exceptions | `HealthwatcherApi/Application/` |
| `DbContext`, repository implementations, middleware | `HealthwatcherApi/Infrastructure/` |
| Controllers, DI/Swagger/middleware wiring, `Program.cs` | `HealthwatcherApi/Presentation/` |
| Validation constants, request context, error response | `HealthwatcherApi/Shared/` |

Dependencies point inwards. `Domain` references nothing above it — domain services take
primitives, not DTOs, so the rules stay independent of how they are transported.

## What is already wired

- **Soft delete** — `BaseEntity.IsDeleted` plus a global query filter applied to every entity.
- **Audit fields** — `CreatedAt/UpdatedAt/CreatedBy/UpdatedBy` stamped in `SaveChangesAsync`.
- **Unit of work** — `IUnitOfWork` implemented by `AppDbContext`; services choose when to commit.
- **Exception middleware** — registered outermost, so it also covers auth and CORS. Maps
  `AppLayerException`/`BusinessException` to status codes; unexpected exceptions return a
  generic message and a `traceId` rather than leaking internals.
- **Pagination** — `PageRequest` binds from the query string and clamps itself; repositories
  do `Skip`/`Take` with a separate count. `PagedResult` reports `TotalPages`/`HasNextPage`.
- **Mapping** — plain extension methods (`target.ToDto()`). No mapping library: a missed
  property is a compile error instead of a null at runtime.
- **snake_case naming** via `EFCore.NamingConventions`; **Swagger** in Development only;
  **CORS** driven by `Cors:AllowedOrigins`; **health endpoint** at `/health`.

Zero build warnings and zero vulnerable packages — keep it that way.

## Running

```bash
dotnet tool restore                    # once per clone: pins dotnet-ef via .config/dotnet-tools.json

# Postgres on localhost:5432, database app_db.
# There are no migrations yet — generate one once the entities are settled:
dotnet dotnet-ef migrations add Init --project HealthwatcherApi
dotnet dotnet-ef database update --project HealthwatcherApi
dotnet run --project HealthwatcherApi       # http://localhost:5056/swagger
```

The EF tool is pinned in the local manifest rather than assumed to be installed globally,
so everyone who clones this gets the version that matches the EF Core packages.

`appsettings.json` ships with an empty connection string on purpose. Development uses
`appsettings.Development.json`; elsewhere set `ConnectionStrings__DefaultConnection` in
the environment.

## Testing

```bash
dotnet test                                   # whole solution
dotnet watch test --project HealthwatcherApi.Tests # red/green loop for TDD
```

xUnit + NSubstitute. No Postgres needed — anything touching a database uses in-memory
SQLite. Pick the cheapest level that can express the rule you are about to write:

| Test | Layer | Needs |
|---|---|---|
| `Domain/TargetTests` | entity behaviour | nothing — no mocks, no DB |
| `Domain/TargetDomainServiceTests` | rules across entities | nothing — it takes primitives |
| `Application/TargetServiceTests` | orchestration: load → delegate → save → map | substituted repositories |
| `Application/PageRequestTests` | query-string normalisation | nothing |
| `Infrastructure/AppDbContextTests` | audit stamping, soft-delete filter | `SqliteAppDbContextFactory` |
| `Integration/TargetEndpointsTests` | HTTP in, JSON out, through the real pipeline | `TestWebApplicationFactory` |

`TestWebApplicationFactory` boots the actual `Program.cs` and replaces only the database
registration, so broken DI or middleware ordering fails a test rather than production.

## Docker

Build context is the repository root.

```bash
docker build -t healthwatcherapi .
docker build -f Dockerfile.migrator -t healthwatcherapi-migrator .
```

The API image runs as non-root and listens on 8080. The migrator applies migrations and
exits — run it as a Kubernetes Job or an init container before the API starts.
