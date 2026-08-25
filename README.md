# Healthwatcher — Service Monitoring Dashboard

Monitors a configurable list of URLs on a fixed interval, records each check, and shows
live status on a dashboard that updates itself — built for the take-home task in
[Take-Home-Task-Service-Monitoring-Dashboard.md](Take-Home-Task-Service-Monitoring-Dashboard.md).

- **Backend:** [API/](API/) — ASP.NET Core (.NET 10). See [API/README.md](API/README.md)
  for the layered project structure, testing, and local run instructions.
- **Frontend:** [HealthwatcherUi/](HealthwatcherUi/) — Angular 19, standalone components.
  See [HealthwatcherUi/README.md](HealthwatcherUi/README.md) for its layout and dev server.
- **Deployment:** Docker + Kubernetes manifests in [k8s/](k8s/), targeting minikube.

## Language/framework choices, and why

**The honest main reason is familiarity.** This was a one-week, ~8-12 hour task, and the
fastest way to spend that time on the actual problem (monitoring, not the tools) was to
build it in the stack I already know well end to end: ASP.NET Core on the backend,
Angular on the frontend. Picking something new here would have spent the timebox on
learning instead of on the app.

That said, both are also genuinely good fits for what this task needs, not just what I
know:

- **ASP.NET Core / .NET 10 (backend)**
  - A `BackgroundService` running a `PeriodicTimer` is the standard, built-in way to run
    "check N things on an interval" — no extra scheduling library needed.
  - `IHttpClientFactory` gives pooled connections, per-client timeouts, and handler
    lifetime management for free — exactly what probing several URLs concurrently needs.
  - EF Core gives a repeatable, version-controlled schema (`Migrations/`) instead of
    hand-written SQL, and the same `DbContext` works against an in-memory SQLite provider
    in tests with zero mocking of the data layer.
  - Strong typing end to end (DTOs, `Options` binding with validation) turns a lot of
    "wrong field name in a config file" or "typo in a JSON payload" mistakes into compile
    errors or a startup failure, rather than a silent runtime bug.
- **Angular (frontend)**
  - It's "batteries included" — router, `HttpClient`, forms, and a CLI with a dev server,
    build pipeline, and test runner all ship together. For a dashboard this size (a
    handful of views over one data model), that meant not having to pick and wire up a
    router and an HTTP client separately.
  - TypeScript interfaces (`models/target.model.ts`) mirror the backend DTOs directly, so
    a shape mismatch between API and UI is a compile-time error in the frontend too.
  - Standalone components (no `NgModule` boilerplate) and template-driven forms
    (`ngModel`) kept the amount of framework ceremony proportional to how small this app
    actually is.

Two smaller choices worth calling out:

- **SQLite, not SQL Server/Postgres, for storage** — one file, no separate server
  process, so the whole backend fits in a single small pod. `git log` shows this project
  actually started on Postgres and was deliberately switched (`0568810`) once the
  Kubernetes deployment made the tradeoff concrete: a second stateful service is more
  infrastructure than a take-home dashboard needs. The real cost of that choice is under
  [What I'd do differently for production](#what-id-do-differently-for-production) below.
- **Polling, not SSE/WebSockets, for live updates** — the dashboard re-fetches on a
  `setInterval` (`target-list.component.ts` / `target-detail.component.ts`). Simpler to
  implement and reason about than a push channel, and at this scale (a handful of
  targets, a few clients) the extra round trips don't matter.

## Architecture overview

```
                    ┌─────────────────────────┐
   host browser ───▶│  ui pod (nginx)         │
                    │  - serves the Angular   │
                    │    build                │
                    │  - proxies /api/* ──────┼───▶ ┌───────────────────────────┐
                    └─────────────────────────┘     │  api pods × N (ASP.NET)   │
                                                    │  - REST API under /api    │
                                                    │  - the replica holding    │
                                                    │    the lease probes every │
                                                    │    target every 30s       │
                                                    └─────────────┬─────────────┘
                                                                  │
                                                    ┌─────────────▼─────────────┐
                                                    │  api-data PVC             │
                                                    │  - one SQLite file for    │
                                                    │    every replica, lease   │
                                                    │    row included           │
                                                    └───────────────────────────┘
```

The browser only ever talks to the `ui` pod's origin. `nginx.conf` reverse-proxies
`/api/*` to the `api` Service (`http://api:8080`) — the same Service name is used in
`docker-compose.yml`, so the identical nginx config works unmodified in both. This also
means the browser and the API are same-origin, so there's no CORS involved in the
deployed path (CORS is still configured API-side for `http://localhost:4200`, used only
when running the Angular dev server directly against a local `dotnet run`).

Locally without Docker, the same relative `/api` base URL in `target.service.ts` is
forwarded to `http://localhost:5056` by the Angular CLI's dev-server proxy
(`proxy.conf.json`, wired into `npm start`). The frontend code never hardcodes a backend
host — same relative path in dev, Docker Compose, and Kubernetes.

On startup, the API (`DatabaseInitializer`) applies EF Core migrations and seeds the
`Monitoring:Targets` list itself — no separate migration Job or manual step is needed for
a fresh pod to come up working. Every replica does this at once against the same file, so
it first takes an exclusive lock on a `.lock` file next to the database: the first pod
migrates and seeds, the rest wait, then find the migration applied and every target
already there. `Dockerfile.migrator` exists as an alternative (run migrations as a one-off
Job/init container) but isn't required by these manifests.

Configuration — the target URL list, check interval, per-check timeout — lives in
`k8s/api-configmap.yaml`, mounted into the pod as `appsettings.Production.json`. Change
it and re-apply + restart the deployment; no image rebuild needed.

Project structure follows a layered split on the backend (`Domain` / `Application` /
`Infrastructure` / `Presentation`, dependencies pointing inward — see
[API/README.md](API/README.md#layout)) and a flat, per-feature split on the frontend
(`target-list/`, `target-detail/`, one `services/target.service.ts` for all API calls).
Neither is more architecture than a project this size needs; both keep it obvious where
a given piece of behavior lives.

## Horizontal scaling

`kubectl -n healthwatcher scale deployment/api --replicas=5` is safe. Every replica serves
API traffic, but exactly one of them runs the check timer, so five pods still probe each
target once per interval instead of five times — no duplicate history rows, no five times
the load on the monitored services.

Which pod that is comes down to one row in the database (`monitor_leases`), not to anything
Kubernetes-specific, so the same mechanism works under `docker-compose` and would survive a
move to Postgres:

- Every pod ticks on the same `PeriodicTimer`, and each cycle starts by trying to claim or
  renew the lease. The claim is `UPDATE ... WHERE id = 1 AND token = <the token I just
  read>` — `token` is an EF Core concurrency token, rewritten on every grant. Of two pods
  reaching for the same free lease, one updates a row and leads; the other matches nothing,
  gets a `DbUpdateConcurrencyException`, and stands by for that cycle.
- The holder renews on every cycle, and again *during* a cycle, at a third of the TTL. That
  second part matters: a cycle takes up to `ceil(targets / MaxConcurrentChecks) × TimeoutSeconds`,
  so with enough targets it can outlast the TTL, and without mid-cycle renewal the lease
  would lapse under a leader that is still working — leaving a standby free to probe the same
  targets at the same time. If a renewal ever finds the lease gone, that pod abandons its
  cycle instead of writing results it no longer owns.
- `Monitoring:LeaseTtlSeconds` (90s, validated to be at least twice the interval so a single
  missed renewal can't cost it the lease) is how long a claim survives unrenewed. If the
  leader is killed outright, checks stall for that TTL *plus up to one interval* — the lease
  has to expire, and a standby only notices on its next tick.
- On a graceful shutdown — a rolling update, `kubectl delete pod` — the leader expires its
  own lease on the way out and a survivor picks the checks up on its next tick, rather than
  waiting out the TTL.

Watch it happen:

```bash
kubectl -n healthwatcher scale deployment/api --replicas=5
kubectl -n healthwatcher logs -l app=api --prefix --tail=20 | grep lease
#   one pod:  api-... holds the monitor lease and is now running the checks
#   the rest: api-... does not hold the monitor lease and is standing by

# kill the leader and watch another pod take over
kubectl -n healthwatcher delete pod <the leader>
```

What this leans on is the shared SQLite file (`api-data` PVC): `ReadWriteOnce` restricts the
volume to one *node*, not one pod, and minikube is a single node, so every replica mounts
the same claim. WAL mode (set on startup) keeps the writing replica from blocking the
reading ones. A second node is where that stops working — see below.

One wrinkle worth knowing about: the container runs as non-root (UID 1654) and a freshly
provisioned volume arrives owned by root. minikube's hostPath provisioner happens to create
it mode `0777`, so it works there either way — but on storage that doesn't (a plain Docker
named volume is `0755`, as are many CSI defaults) the app can't create the database file and
dies with `Access to the path '/data/healthwatcher.db.lock' is denied`. `fsGroup` is the tidy
fix on CSI-backed storage but a hostPath-backed PV ignores it, so the deployment runs a
one-line init container — the same image, as root — to `chown` the volume first. Insurance
against the storage backend rather than a fix for anything minikube does.

## Setup and run instructions

Assumes Docker, minikube, and kubectl are installed and `minikube start` has already
been run once.

```bash
# 1. Build the images
docker build -t healthwatcherapi:local API
docker build -t healthwatcherui:local HealthwatcherUi

# 2. Load them into minikube's image store (no registry needed)
minikube image load healthwatcherapi:local
minikube image load healthwatcherui:local

# 3. Apply the manifests
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/api-configmap.yaml
kubectl apply -f k8s/api-pvc.yaml
kubectl apply -f k8s/api-deployment.yaml
kubectl apply -f k8s/api-service.yaml
kubectl apply -f k8s/ui-deployment.yaml
kubectl apply -f k8s/ui-service.yaml

# 4. Wait for both pods to be ready
kubectl -n healthwatcher get pods --watch

# 5. Open the dashboard (holds a tunnel open in the foreground - see the note below)
minikube service ui -n healthwatcher
```

`minikube service` opens the NodePort Service (`ui`, port 30080) in your default browser.
The dashboard should show the seeded targets going from "checking" to their first
UP/DOWN result within a few seconds.

Verified end to end on this machine (macOS, Docker driver): built both images, loaded
them into minikube, applied the manifests, and confirmed the `ui` pod's nginx correctly
proxies `/api/*` to the `api` Service and the dashboard receives live check data through
that path. One driver-specific note from that run: with `--driver=docker`,
`minikube service` doesn't just open a browser and exit — it holds an SSH tunnel open in
the foreground and prints `Because you are using a Docker driver on darwin, the
terminal needs to be open to run it.` That's expected, not a hang; keep that terminal
open for as long as you want the dashboard reachable. `--url` blocks the same way, so to
avoid tying up a terminal, port-forward instead:

```bash
kubectl -n healthwatcher port-forward svc/ui 18080:80   # then http://localhost:18080
```

To change the monitored URLs or interval, edit `k8s/api-configmap.yaml`, then:

```bash
kubectl apply -f k8s/api-configmap.yaml
kubectl -n healthwatcher rollout restart deployment/api
```

To tear everything down:

```bash
kubectl delete namespace healthwatcher
```

For running the two apps locally without Docker/minikube (day-to-day development), see
[API/README.md](API/README.md#running) and
[HealthwatcherUi/README.md](HealthwatcherUi/README.md#development-server).

## Bonus items implemented

- **Response-time history chart** — the target detail page plots the last 20 checks'
  response times as an inline SVG line chart (no charting library — a `<polyline>` over
  computed points), oldest to newest, with down checks marked in red. Updates on the same
  15s poll as the rest of the page.
- **Add/remove monitored URLs from the UI** — the dashboard's add form and each row's
  Rename/Delete actions call the same REST endpoints the ConfigMap-seeded targets use.
  Delete goes through a confirmation dialog, since it takes the target's history with it.
- **Uptime percentage** — the target detail page shows uptime over a configurable window
  (defaults to 24h) via `GET /api/targets/{id}/uptime`.
- **Down alerting indicator** — the dashboard shows a banner naming every target currently
  down, above the table; the detail page shows one for that target, carrying the last
  check's error. Both are on top of the per-row status badge, not instead of it.
- **Kubernetes liveness/readiness probes** — both `api` and `ui` deployments probe
  `/health` and `/` respectively.
- **Horizontal scaling** — the `api` deployment ships at 2 replicas and scales to any
  number, with a database-backed lease keeping exactly one of them on the check timer. See
  [Horizontal scaling](#horizontal-scaling).

That covers every bonus item on the task list.

## What I'd do differently for production

- **A shared SQLite file, not a networked database.** Every replica reads and writes one
  file on a `ReadWriteOnce` PVC, which only holds because minikube is a single node — add
  a second node and the pods can't share the claim. Postgres is where this project
  actually started (see `git log`) and where I'd take it back for anything real; the lease
  and the startup lock are plain EF Core and a file handle, so the lease would carry over
  unchanged and the lock would be replaced by an advisory lock or a migration Job.
- **Nothing prunes or backs up that volume.** History grows by one row per target per
  interval forever, and the claim has no snapshot or retention policy behind it.
- **No Ingress / TLS.** NodePort is enough for a reviewer's minikube; a real deployment
  would put an Ingress (or LoadBalancer) with TLS in front of the `ui` Service.
- **No resource requests/limits or HPA** on either deployment — worth adding once real
  traffic/usage patterns are known.
- **No auth** on the API or dashboard — fine for a local demo, not for anything reachable
  outside a trusted network.
- **Secrets, if any were needed** (a real DB connection string, an API key for a
  monitored target) would move to a Kubernetes `Secret`, not the plaintext ConfigMap used
  for the non-sensitive monitoring config here.
