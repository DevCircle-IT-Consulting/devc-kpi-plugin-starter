# CLAUDE.md — DevC.KPI reporting plugin

**This is a DevC.KPI reporting plugin** — for DevCircle's KPI/reporting platform. It is **not** Grafana
and not a generic .NET service. It is a `net10.0` class library that references exactly one public NuGet
package, **`DevC.KPI.Reporting.Sdk`**, and adds *cubes* (aggregations) and *widgets* (charts, KPI tiles,
tables) that the licensed engine loads at runtime. **You build and test it here; you never reference or
run the engine.** `dotnet test -c Release` is the whole inner loop.

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

- **Download the probe from the WEB (client) host — NOT the API host — and do NOT build it from source.**
  Both binaries are served at `https://<web-host>/downloads/`: `DevC.KPI.ProxyProbe-win-x64.exe` /
  `-linux-x64` (and the proxy, `DevC.KPI.Proxy-*`). On a split install the web host differs from the API
  host (e.g. `kpi.example.com` vs `kpi-api.example.com`). Diagnosing a `404`:
  - 404 for everything under `/downloads/` → you're on the API host; use the web host.
  - `DevC.KPI.Proxy-*` downloads but `DevC.KPI.ProxyProbe-*` 404s → the engine image predates the probe
    download; ask the operator to update the engine (do not compile it yourself).
- The probe's `--url` is the **API** base (`https://<api-host>/api`). Auth: an Admin/TenantAdmin bearer
  token (`--token`) or a relay key.
- **A datasource must already exist to probe** (`--ds <id>`). If only a proxy + a DB secret exist but no
  datasource yet, first create a minimal `config/<tenant>/datasources/<id>.yaml` that binds `proxy:` +
  `secret:` (no cube needed), get it deployed, then probe that `--ds`.
- The operator must **open a dev-access window** on the proxy first, or the safe verbs return `403`.

## Deploy (self-hosted / by hand)

`bash deploy/build-bundle.sh <Plugin> <tenant>` → copy `dist/bundles/<Plugin>/plugins/<Plugin>/` to the
server's `/srv/kpi/plugins/<Plugin>/` and `config/<tenant>/` to `/srv/kpi/config/<tenant>/`, then
`docker compose restart api`. See `docs/05`.
