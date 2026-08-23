# Healthwatcher — Service Monitoring Dashboard

Monitors a configurable list of URLs on a fixed interval, records each check, and shows
live status on a dashboard that updates itself — built for the take-home task in
[Take-Home-Task-Service-Monitoring-Dashboard.md](Take-Home-Task-Service-Monitoring-Dashboard.md).

- **Backend:** [API/](API/) — ASP.NET Core (.NET 10). See [API/README.md](API/README.md)
  for the layered project structure, testing, and local run instructions.
- **Frontend:** [HealthwatcherUi/](HealthwatcherUi/) — Angular 19, standalone components.
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
   host browser ───▶│  ui pod (nginx)          │
                     │  - serves the Angular    │
                     │    build                 │
                     │  - proxies /api/* ───────┼───▶ ┌─────────────────────────┐
                     └─────────────────────────┘      │  api pod (ASP.NET Core) │
                                                        │  - REST API under /api  │
                                                        │  - BackgroundService    │
                                                        │    probes each target   │
                                                        │    every 30s            │
                                                        │  - SQLite file on the   │
                                                        │    container filesystem│
                                                        └─────────────────────────┘
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
a fresh pod to come up working. `Dockerfile.migrator` exists as an alternative (run
migrations as a one-off Job/init container) but isn't required by these manifests.

Configuration — the target URL list, check interval, per-check timeout — lives in
`k8s/api-configmap.yaml`, mounted into the pod as `appsettings.Production.json`. Change
it and re-apply + restart the deployment; no image rebuild needed.

Project structure follows a layered split on the backend (`Domain` / `Application` /
`Infrastructure` / `Presentation`, dependencies pointing inward — see
[API/README.md](API/README.md#layout)) and a flat, per-feature split on the frontend
(`target-list/`, `target-detail/`, one `services/target.service.ts` for all API calls).
Neither is more architecture than a project this size needs; both keep it obvious where
a given piece of behavior lives.

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
kubectl apply -f k8s/api-deployment.yaml
kubectl apply -f k8s/api-service.yaml
kubectl apply -f k8s/ui-deployment.yaml
kubectl apply -f k8s/ui-service.yaml

# 4. Wait for both pods to be ready
kubectl -n healthwatcher get pods --watch

# 5. Open the dashboard
minikube service ui -n healthwatcher
```

`minikube service` opens the NodePort Service (`ui`, port 30080) in your default browser.
The dashboard should show the seeded targets going from "checking" to their first
UP/DOWN result within a few seconds.

Verified end to end on this machine (Windows, Docker driver): built both images, loaded
them into minikube, applied the manifests, and confirmed the `ui` pod's nginx correctly
proxies `/api/*` to the `api` Service and the dashboard receives live check data through
that path. One driver-specific note from that run: with `--driver=docker` on Windows/Mac,
`minikube service` doesn't just open a browser and exit — it holds an SSH tunnel open in
the foreground and prints `Because you are using a Docker driver on windows, the
terminal needs to be open to run it.` That's expected, not a hang; keep that terminal
open (or run it with `--url` in the background) for as long as you want the dashboard
reachable.

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
[API/README.md](API/README.md#running) and its [Frontend](API/README.md#frontend)
section.

## Bonus items implemented

- **Response-time history chart** — the target detail page plots the last 20 checks'
  response times as an inline SVG line chart (no charting library — a `<polyline>` over
  computed points), oldest to newest, with down checks marked in red. Updates on the same
  15s poll as the rest of the page.
- **Add/remove monitored URLs from the UI** — the dashboard's add form and each row's
  Delete/Rename actions call the same REST endpoints the ConfigMap-seeded targets use.
- **Uptime percentage** — the target detail page shows uptime over a configurable window
  (defaults to 24h) via `GET /api/targets/{id}/uptime`.
- **Kubernetes liveness/readiness probes** — both `api` and `ui` deployments probe
  `/health` and `/` respectively.

Not implemented: a dedicated down-alert banner (status is a colored badge inline, not a
separate alert) and the horizontal-scaling demo — see below for why.

## What I'd do differently for production

- **No persistent storage for the API.** SQLite lives on the pod's container filesystem,
  so a restart or reschedule loses all check history (targets get reseeded from the
  ConfigMap, but their history starts over). For production I'd either move to a
  networked database (Postgres, which is what this project actually started on — see
  `git log`) or put SQLite on a PVC, whichever survival requirement won on cost.
- **API is pinned to 1 replica on purpose.** Multiple pods each running EF migrations
  against the same SQLite file on startup, or writing to it concurrently, isn't something
  SQLite is built for — so the horizontal-scaling bonus wasn't attempted here. A networked
  database is the prerequisite for that, not a Kubernetes change.
- **No Ingress / TLS.** NodePort is enough for a reviewer's minikube; a real deployment
  would put an Ingress (or LoadBalancer) with TLS in front of the `ui` Service.
- **No resource requests/limits or HPA** on either deployment — worth adding once real
  traffic/usage patterns are known.
- **No auth** on the API or dashboard — fine for a local demo, not for anything reachable
  outside a trusted network.
- **Secrets, if any were needed** (a real DB connection string, an API key for a
  monitored target) would move to a Kubernetes `Secret`, not the plaintext ConfigMap used
  for the non-sensitive monitoring config here.
