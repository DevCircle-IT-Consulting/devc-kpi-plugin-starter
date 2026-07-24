# 02 - Config reference (`config/<tenant>/`)

Code defines cubes and widgets; **YAML wires and places them**. All config for one tenant lives under
`config/<tenant>/`, hot-reloaded by the engine (report/datasource YAML changes need no restart; a new
plugin DLL does).

## The tenant folder name is not a label

`config/<tenant>/` and `PluginScope.ForTenants("<tenant>")` are matched **by exact string** against
the tenant name the engine resolves for each request (tenant id -> name). There is **no mapping
table and no translation**. Use the exact tenant name DevCircle provisioned for you. The `dotnet new`
template's `--tenant` sets this everywhere at once.

## Layout

```
config/<tenant>/
├── plugins.yaml                 # which SHARED plugins this tenant enables
├── groups.yaml                  # report grouping + order in the nav (presentational only)
├── datasources/*.yaml           # one file per datasource (one cube each)
└── reports/*.yaml               # one file per report (pages + widget placement + filters)
```

## `plugins.yaml`

Only needed for `Shared`-scoped plugins. A `ForTenants(...)` plugin auto-binds and needs no entry.

```yaml
enabled:
  - DataMeans      # the plugin Id of each Shared plugin this tenant turns on
```

## `datasources/<id>.yaml`

One datasource == one cube. No SQL, no credentials here - just the wiring. See
[03-datasources-and-secrets.md](03-datasources-and-secrets.md) for `type`, `freshness`, proxy/secret.

```yaml
id: sales                # the id widgets bind to (Widget.DataSourceId) and pages list
builder: SalesCube       # the cube's Key
type: custom             # postgres | mssql | custom | text
freshness: { mode: cached, refresh: "0 3 * * *" }
loadWindow: { from: "-3Y", to: "now" }
params: { schema: public }
```

## `reports/<id>.yaml`

```yaml
id: sales
title: "Sales demo"
filters:                       # optional; report-root filters render on every page
  - id: periods
    type: datetree             # year/quarter/month hierarchy - the standard date filter
    bindsTo: Date              # the conformed dimension it slices
pages:
  - id: overview
    title: "Overview"
    datasources: [sales]       # datasource ids this page's data-bound widgets read
    layout: { cols: 12 }       # a 12-column grid
    widgets:
      - { widget: RevenueKpi,          pos: { x: 0, y: 0, w: 3, h: 2 } }
      - { widget: RevenueByMonthChart, pos: { x: 3, y: 0, w: 9, h: 4 } }
```

- `widget:` is the widget's `Key`. `pos` is `{x,y,w,h}` in grid cells.
- A **static** widget (no data) needs no `datasources:` entry; a **data-bound** one needs its
  datasource listed on the page.
- **Filters**: `datetree` (date hierarchy) is the default; enum filters bind to an enum dimension. A
  filter at the report root is shared by all pages.

## `groups.yaml`

Presentational grouping/order for the report list. Never grants access (that is per-user rights).
Reports not listed fall into a trailing section.

```yaml
groups:
  - id: examples
    title: "Examples"
    reports: [sales]
```

## Where config comes from at deploy time

Three owners, by precedence (first root that has a `<tenant>/` folder wins — **whole folder, no
merge**):
1. The server's `/srv/kpi/config/<tenant>/` (`Reporting:ConfigPath`; your plugin self-deploys its
   `config/<tenant>/` here). Highest precedence.
2. (local dev only) extra roots (`Reporting:ConfigPaths`).
3. The demo `default` tenant baked into the engine image (`config-builtin/`). Lowest precedence.

> **`ConfigPath` must point at the mounted config** (`/srv/kpi/config` in the standard container). If
> it's empty the engine falls back to a dev-only path that doesn't exist in the container, so your
> mounted config is silently ignored and you get only the baked demo. It's set in
> `api/appsettings.Production.json` (and/or the compose `Reporting__ConfigPath` env).

> **Avoid reusing the `default` tenant name.** `default` is the baked-in Demo/Weather showcase. If you
> name *your* tenant `default`, your `config/default/` (root 1) shadows the demo (root 3) whole-folder —
> fine when it loads, but if that folder is ever missing the engine **silently falls back to the demo
> `default`**, so you'd see demo reports and think yours loaded. Use a **distinct tenant name** (your
> provisioned name, e.g. `acme`) to avoid the clash entirely.

`proxies.yaml` is **server-owned** (never in your repo) - see the next page.
