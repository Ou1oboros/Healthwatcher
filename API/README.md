# HealthwatcherApi

A layered ASP.NET Core Web API skeleton — Domain / Application / Infrastructure / Presentation
inside a single project, with EF Core + SQLite, and a test project covering every layer.

**.NET 10** (LTS, supported to November 2028) · EF Core 10 · SQLite · xUnit + NSubstitute.


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

# SQLite file, created on demand. Migrations are already checked in under Migrations/;
# applying them creates/updates the .db file:
dotnet dotnet-ef database update --project HealthwatcherApi
dotnet run --project HealthwatcherApi       # http://localhost:5056/swagger
```

The EF tool is pinned in the local manifest rather than assumed to be installed globally,
so everyone who clones this gets the version that matches the EF Core packages.

`appsettings.json` ships with a default `Data Source=healthwatcher.db` connection string.
Development uses `appsettings.Development.json`; elsewhere set
`ConnectionStrings__DefaultConnection` in the environment to point at a different file
(or a persistent volume, in Kubernetes).

## Testing

```bash
dotnet test                                   # whole solution
dotnet watch test --project HealthwatcherApi.Tests # red/green loop for TDD
```

xUnit + NSubstitute. No real database needed — anything touching a database uses
in-memory SQLite. Pick the cheapest level that can express the rule you are about to write:

| Test | Layer | Needs |
|---|---|---|
| `Domain/TargetTests` | entity behaviour | nothing — no mocks, no DB |
| `Domain/TargetDomainServiceTests` | rules across entities | substituted repository for `InsertTarget`; nothing for rename/delete |
| `Application/TargetServiceTests` | orchestration: load → delegate → save → map | substituted repositories |
| `Application/PageRequestTests` | query-string normalisation | nothing |
| `Infrastructure/AppDbContextTests` | audit stamping, soft-delete filter | `SqliteAppDbContextFactory` |
| `Integration/TargetEndpointsTests` | HTTP in, JSON out, through the real pipeline | `TestWebApplicationFactory` |

`TestWebApplicationFactory` boots the actual `Program.cs` and replaces only the database
registration, so broken DI or middleware ordering fails a test rather than production.

## Frontend

`../HealthwatcherUi` (sibling to this repo) is a minimal Angular app (standalone
components, template-driven forms with `ngModel`, no state library) that talks to
the API. It expects the API at `http://localhost:5056/api` (see
`src/app/services/target.service.ts`) and the API's CORS policy already allows
`http://localhost:4200`.

```bash
cd ../HealthwatcherUi
npm install
npm start            # http://localhost:4200
```

## Docker

Build context is this directory (`API/`). From here:

```bash
docker build -t healthwatcherapi .
docker build -f Dockerfile.migrator -t healthwatcherapi-migrator .
```

Or from the repository root: `docker build -t healthwatcherapi API`. See the top-level
[README.md](../README.md) for the full Docker + Kubernetes deployment.

The API image runs as non-root and listens on 8080. The migrator applies migrations and
exits — run it as a Kubernetes Job or an init container before the API starts.
