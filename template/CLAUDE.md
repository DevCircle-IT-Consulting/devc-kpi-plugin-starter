# CLAUDE.md — DevC.KPI reporting plugin

**This is a DevC.KPI reporting plugin** — for DevCircle's KPI/reporting platform. It is **not** Grafana
and not a generic .NET service. It is a `net10.0` class library that references exactly one public NuGet
package, **`DevC.KPI.Reporting.Sdk`**, and adds *cubes* (aggregations) and *widgets* (charts, KPI tiles,
tables) that the licensed engine loads at runtime. **You build and test it here; you never reference or
run the engine.** `dotnet test -c Release` is the whole inner loop.

## Start here: read PLUGIN.md

**[`PLUGIN.md`](PLUGIN.md) is this install's profile** — hosting (self-hosted vs DevCircle-hosted),
tenant name, DNS topology, web + API hosts, proxy/secret, the datasources, and how to deploy. **Read it
first.** It holds the facts you'd otherwise have to ask for every session.

- If it's still the unfilled template (placeholder `<…>` values), **ask the author to fill it — or fill
  it in together** by asking only for the missing values — then proceed.
- Once it's filled, follow it: install the skill (below) → if the DB needs exploring, get the probe from
  the **web host** in PLUGIN.md and scaffold a minimal datasource if none exists yet (see "Exploring a
  customer database") → build cubes/widgets → deploy per PLUGIN.md.
- If the author opens with a greeting or a vague *"how do I start?"*, don't reply with only an
  explanation — **begin**: read PLUGIN.md, install the skill, run `dotnet test -c Release` to confirm the
  inner loop, then either continue per PLUGIN.md or list exactly which PLUGIN.md values / run-time inputs
  (token, open dev-access window) you still need.

## Use the authoring skill

There is a Claude Code skill, **`authoring-kpi-plugin`**, that knows the exact SDK surface and the
authoring workflow — use it for anything about cubes, dimensions, measures, widgets/charts, KPI tiles,
tables, `IReportingPlugin`, or the YAML datasource/report binding. It is **not vendored here** — it
ships inside the `DevC.KPI.Reporting.Sdk` NuGet package. To install it:

1. `dotnet restore` (or `dotnet build`) once — pulls the SDK into your NuGet cache.
2. Run the installer from the cache (substitute the restored `<version>`):
   - Windows: `& "$env:USERPROFILE/.nuget/packages/devc.kpi.reporting.sdk/<version>/skills/authoring-kpi-plugin/install.ps1"`
   - macOS/Linux: `~/.nuget/packages/devc.kpi.reporting.sdk/<version>/skills/authoring-kpi-plugin/install.sh`
3. Then ask normally (e.g. "add a monthly-revenue line chart over my orders table"). Prefer the skill's
   `references/sdk-surface.md` over guessing signatures.

## Docs

Full guide: **https://github.com/DevCircle-IT-Consulting/devc-kpi-plugin-starter** (`docs/`)
- Build: `00-getting-started` · `01-plugin-anatomy` · `02-config-reference` · `03-datasources-and-secrets`
  · `04-widgets-and-charts` · `05-build-and-deploy`
- Explore a customer database: `07-exploring-your-database`
- Operate the server: `08`–`14`

## Ground rules (things agents reliably get wrong)

- **Compile against the SDK only.** Never reference the engine or copy engine internals. If a type isn't
  in the SDK, it isn't part of the contract.
- **The tenant slug must equal the provisioned tenant name.** `config/<tenant>/` and
  `PluginScope.ForTenants("<tenant>")` are exact-string-matched by the engine — not a display label. Ask
  the operator for the exact name.
- **`ResultNames` == the query names `DefineQueries` yields.** **`DataSourceId` == the datasource YAML
  `id:`** — NOT the cube `Key` (that is the YAML `builder:`).
- **Fact streams are lazy** — return `IEnumerable`; never `.ToList()` a `LoadFacts` projection.
- **Merge `context.RawOverrides` last** when building an ECharts option.
- Unit-test cubes with the SDK kit (`InMemoryBuildDataAccess` for DB cubes, `EmptyBuildDataAccess` for
  `custom`).

## Exploring a customer database (the ProxyProbe)

If the task is "explore / understand the source DB", use the **ProxyProbe** — a safe-verb CLI
(schema / profile / validate; it **never returns a row value**). See `docs/07`. What a fresh session
gets wrong:

- **Download the probe from where you open the web app (the web front-end), at `/downloads/` — the web
  root, never under `/api` — and do NOT build it from source.** Two deployments: **single DNS** (API
  under `/api`) → `https://<host>/downloads/DevC.KPI.ProxyProbe-<win-x64.exe|linux-x64>`; **two DNS** (API
  on its own root name) → the **web** name `https://<web-host>/downloads/…`, not the API name. Diagnosing
  a `404`: everything under `/downloads/` 404s → you're on the API path/host, use the web root; the
  `DevC.KPI.Proxy-*` binary downloads but `ProxyProbe-*` 404s → the engine predates the probe download,
  ask the operator to update it.
- The probe's `--url` is the **API base** — the same value the client uses as `ApiUrl`: `https://<host>/api`
  (single DNS) or the API's own root name (two DNS). Auth: an Admin/TenantAdmin bearer token (`--token`)
  or a relay key.
- **A datasource must already exist to probe** (`--ds <id>`). No plugin DLL or real cube is needed (the
  probe reads only the datasource's proxy + secret), **but the YAML must be valid** — the engine requires
  `id`, `builder`, `type`, `secret`/`proxy`, and `loadWindow` even for a probe-only binding. `builder` may
  be a placeholder cube Key (an unknown builder is only rejected when a *report* binds it). `id` is
  explicit, not from the filename. Deploy the YAML to the server's `/srv/kpi/config/<tenant>/`, then probe:
  ```yaml
  id: sales
  builder: SalesCube         # placeholder; unused for probing
  type: postgres
  secret: sales-db
  proxy: acme-onprem1        # omit for a direct DB
  freshness: { mode: cached, refresh: "0 3 * * *" }
  loadWindow: { from: "-3Y", to: "now" }
  ```
- The operator must **open a dev-access window** on the proxy first, or the safe verbs return `403`.

## Deploy (self-hosted / by hand)

**No report or widget appears until the plugin DLL is deployed** — cubes/widgets live in the compiled
assembly, so config YAML alone shows nothing. (Probing the DB is the exception — it needs only the
datasource config, not the DLL.)

`bash deploy/build-bundle.sh <Plugin> <tenant>` → copy `dist/bundles/<Plugin>/plugins/<Plugin>/` to the
server's `/srv/kpi/plugins/<Plugin>/` and `config/<tenant>/` to `/srv/kpi/config/<tenant>/`, then
`docker compose restart api`. Confirm: `docker compose logs api | grep "Reporting plugin"` lists your
plugin. Reports are still only *visible* to a user in that tenant who has been granted them (report
rights); a TenantAdmin has no report list. See `docs/05`.
