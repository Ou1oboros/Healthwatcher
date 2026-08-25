# HealthwatcherUi

The Healthwatcher dashboard: an Angular 19 app — standalone components, template-driven
forms, no state library — that lists every monitored target with its current status, and
drills into one target's uptime and check history.

Generated with [Angular CLI](https://github.com/angular/angular-cli) 19.2.12.

## Layout

| Where | What |
|---|---|
| `src/app/target-list/` | dashboard: status per target, add form, rename/delete, down-alert banner |
| `src/app/target-detail/` | one target: last check, uptime, response-time chart, recent history |
| `src/app/confirm-dialog/` | the confirmation shown before a delete |
| `src/app/services/target.service.ts` | every API call, against the relative base `/api` |
| `src/app/models/target.model.ts` | TypeScript mirrors of the backend DTOs |

Both pages re-fetch on a 15s `setInterval` — polling rather than SSE or WebSockets, for
the reasons in the top-level [README](../README.md).

## Development server

```bash
npm install
npm start            # http://localhost:4200
```

`npm start` is `ng serve --proxy-config proxy.conf.json`, and the proxy forwards `/api` to
the API at `http://localhost:5056` — so run the backend alongside it (see
[API/README.md](../API/README.md#running)). A plain `ng serve` skips the proxy, and every
API call 404s.

The API is never addressed by an absolute host, only by the relative `/api`: the dev
proxy, nginx in the container, and the Kubernetes deployment each resolve that path their
own way, so the same frontend build works in all three.

## Building

```bash
npm run build        # production build into dist/healthwatcher-ui/
```

The Docker image builds that and serves it from nginx, which also reverse-proxies `/api/`
to the `api` Service — see `nginx.conf` and the top-level [README](../README.md) for the
full deployment.

## Running unit tests

```bash
npm test             # ng test — Karma + Jasmine, needs a local Chrome
```

`app.component.spec.ts` is the only spec here; the behaviour worth covering lives in the
backend, which has the [full test suite](../API/README.md#testing).

## Additional resources

[Angular CLI Overview and Command Reference](https://angular.dev/tools/cli).
