# 03 - Datasources, secrets & proxies

A datasource YAML says *where the cube's facts come from* and *how fresh they must be*. It never
contains SQL or credentials.

## `type`

| type | meaning | how the cube gets rows |
|------|---------|------------------------|
| `postgres` | a PostgreSQL database | the engine opens the connection; `LoadFacts` calls `ctx.Sql<T>("select ...")` |
| `mssql` | a Microsoft SQL Server database | same, against MSSQL |
| `custom` | the builder produces its own rows | `LoadFacts` returns them directly (sample data, a REST call, a computation) - no connection is opened for you |
| `text` | line-oriented files (logs, CSV) | `LoadFacts` reads `ctx.Lines()` |

The `example/` plugin uses `custom` (sample data) so it needs no database. A real customer plugin is
usually `postgres` or `mssql`.

## `freshness` - how often the cube rebuilds

```yaml
freshness:
  mode: cached            # cached | live
  refresh: "0 3 * * *"    # cron; when to rebuild a cached cube
```

- `live` rebuilds on **every** widget request (only for tiny/fast or always-changing sources).
- `cached` builds once and refreshes on the schedule - use it for anything non-trivial, so a page
  load does not re-run the whole aggregation.

## `loadWindow` - how much history to load at build time

```yaml
loadWindow:
  from: "-3Y"      # relative (-3Y / -18M / -90D) or an ISO date
  to: "now"
```

`LoadFacts` reads `ctx.LoadWindow.From/.To` and bounds its query to that window.

## `params` - free-form builder inputs

Read via `ctx.Params` in `LoadFacts`. Typically the DB schema, or a REST endpoint/parameters.

```yaml
params: { schema: public }
```

## Secrets (connection strings) - server-owned

A `postgres`/`mssql` datasource references a secret by name; the actual connection string lives in the
server's `secrets/<name>.json`, filled in per install - **never in your repo**. Your bundle may ship a
`secrets/<tenant>.example.json` template.

```yaml
# in the datasource YAML:
secret: sales-db          # -> secrets/sales-db.json on the server (a connection string)
```

## On-prem databases behind a proxy

When the database is not reachable from the engine host (a customer's internal DB), DevCircle installs
the **DevC.KPI proxy** next to the DB and the datasource points at it:

```yaml
proxy: acme-onprem        # -> an entry in the server-owned config/proxies.yaml
secret: sales-db
```

`proxies.yaml` is **deployment infrastructure**, server-owned (`/srv/kpi/config/proxies.yaml`),
maintained by hand like `.env`/`secrets/`. It is not part of your plugin repo. As a plugin author you
just name the proxy your datasource expects and DevCircle wires the actual proxy.

## Cookbook — a commented example for every possibility

`example/config/salesdemo/datasources/` carries a `*.yaml.example` file for each datasource kind
(the `.example` suffix means the engine does not load them — copy one to `<id>.yaml` and adapt):

| File | Covers |
|------|--------|
| `postgres-direct.yaml.example`   | PostgreSQL reachable directly from the engine host (`secret:` only) |
| `postgres-via-proxy.yaml.example`| PostgreSQL on-prem, via `proxy:` + `secret:` |
| `mssql-via-proxy.yaml.example`   | Microsoft SQL Server (direct or via proxy) |
| `custom-restapi.yaml.example`    | `custom` builder fetching from a REST API (+ optional API-key secret) |
| `custom-inprocess.yaml.example`  | `custom` builder with in-process/sample data (no DB) |
| `text-logs.yaml.example`         | `type: text` over rotated/gzipped log files |
| `text-csv.yaml.example`          | `type: text` over CSV file(s) |

Alongside them:
- `example/config/proxies.example.yaml` — what a `proxy: <id>` points at (server-owned; reference only).
- `example/secrets/salesdemo.example.json` — the matching secret entries (connection strings + proxy
  key-hash), with **obvious placeholder** usernames/passwords to replace. Secrets are server-owned and
  never committed with real values.
