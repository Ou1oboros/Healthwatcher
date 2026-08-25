# HealthwatcherApi

The Healthwatcher backend: it probes every monitored URL on a fixed interval, records each
check, and serves the results over REST. Layered — Domain / Application / Infrastructure /
Presentation inside a single project — with EF Core + SQLite, and a test project covering
every layer.

**.NET 10** (LTS, supported to November 2028) · EF Core 10 · SQLite · xUnit + NSubstitute.

For the deployment, the architecture overview, and why this stack, see the top-level
[README.md](../README.md).

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

## Monitoring

`HealthMonitorBackgroundService` owns a `PeriodicTimer` on `Monitoring:IntervalSeconds`;
`HealthMonitorService` runs one cycle, probing targets through `IHealthProbe` behind a
`SemaphoreSlim` capped at `Monitoring:MaxConcurrentChecks`, so the checks overlap instead
of queueing and one slow target can't hold up the rest. A timeout, an unreachable host, or
a bad URL is recorded as a Down result, never an exception that ends the cycle.

Every replica runs that timer, but a cycle only goes ahead on whichever one holds the row
in `monitor_leases` — an EF Core concurrency token settles the race, so nothing
Kubernetes-specific is involved. `DatabaseInitializer` migrates, switches SQLite to WAL,
and seeds `Monitoring:Targets` at startup behind a lock file, so a fresh pod needs no
manual migration step. The top-level [README](../README.md#horizontal-scaling) covers the
leasing in full.

## Cross-cutting plumbing

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
| `Infrastructure/TargetRepositoryTests` | uptime counts, aggregated in SQL rather than in memory | `SqliteAppDbContextFactory` |
| `Infrastructure/MonitorLeaseStoreTests` | leader election for the check timer, including two replicas racing | `SqliteAppDbContextFactory` |
| `Infrastructure/HealthMonitorBackgroundServiceTests` | holding the lease across a cycle that outlasts its TTL | `SqliteAppDbContextFactory` |
| `Integration/TargetEndpointsTests` | HTTP in, JSON out, through the real pipeline | `TestWebApplicationFactory` |

`TestWebApplicationFactory` boots the actual `Program.cs` and replaces only the database
registration, so broken DI or middleware ordering fails a test rather than production.

## Frontend

`../HealthwatcherUi`, the sibling directory in this repository, is a minimal Angular
app (standalone components, template-driven forms with `ngModel`, no state library) that
talks to the API. It calls the API at the relative path `/api` (see
`src/app/services/target.service.ts`) — never a hardcoded backend host, so the same
frontend code works in dev, Docker Compose, and Kubernetes. Locally, `npm start` runs
`ng serve --proxy-config proxy.conf.json`, which forwards `/api` to
`http://localhost:5056`; in the deployed path nginx reverse-proxies it to the `api`
Service. That makes the browser and the API same-origin, so CORS isn't involved — the
API's CORS policy still allows `http://localhost:4200` for hitting a local `dotnet run`
directly, bypassing the proxy.

```bash
cd ../HealthwatcherUi
npm install
npm start            # http://localhost:4200
```

See [HealthwatcherUi/README.md](../HealthwatcherUi/README.md) for its layout.

## Docker

Build context is this directory (`API/`). From here:

```bash
docker build -t healthwatcherapi .
docker build -f Dockerfile.migrator -t healthwatcherapi-migrator .
```

Or from the repository root: `docker build -t healthwatcherapi API`. See the top-level
[README.md](../README.md) for the full Docker + Kubernetes deployment.

The API image runs as non-root (UID 1654) and listens on 8080. The migrator applies
migrations and exits. It is optional: the API migrates itself on startup, so the
Kubernetes manifests don't use it. It's there for a deployment that would rather run
migrations as a separate Job or init container.
