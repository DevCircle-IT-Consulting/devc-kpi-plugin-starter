# 07 - Exploring your data & testing your cube

To write a cube you need two things: to know your data's shape (tables, columns, distributions), and
to prove your aggregation is correct. How you do the first depends on whether your developers can see
the source data at all - often they cannot, because the source system (e.g. an ERP or an applicant
database) is separate and reached only through a proxy.

## A. If your developers have direct database access

Use whatever you already use - `psql`, SSMS, DBeaver. Look at the tables you will project into fact
rows, and write the `select` that produces one fact per row; that becomes your `LoadFacts` projection
(`ctx.Sql<TFact>(sql, ...)`). Nothing DevC-specific is needed for this.

## B. Prove the cube with unit tests (always - the main dev loop)

Regardless of data access, this is how you make a cube correct: the public SDK test kit
(`InMemoryBuildDataAccess` / `EmptyBuildDataAccess`) lets you feed sample rows, build the cube, and
assert the measures - no database, no engine, milliseconds per test.

```csharp
var context = new BuildContext
{
    DataSourceId = "sales",
    Params       = new Dictionary<string, string>(),
    LoadWindow   = new DateRange(DateTime.MinValue, DateTime.MaxValue),
    Data         = new InMemoryBuildDataAccess().Add(rows),   // DB cube: inject rows here
};
var result = new SalesCube().Build(context).Query(SalesCube.RevenueByMonth);
```

- **DB-backed cube** (`ctx.Sql<T>`): `InMemoryBuildDataAccess` returns the seeded rows by type, so your
  projection logic runs without a database.
- **`custom` cube**: build with `EmptyBuildDataAccess.Instance`.

Templates: `template/.../Tests/Reference/CubeTestExample.cs.txt` (DB pattern) and
`example/.../Tests/SalesCubeTests.cs` (custom pattern, runnable).

## C. Explore the schema through your deployed engine - the ProxyProbe

When developers **cannot** read the source database directly (the common case - the DB is a separate
system behind a proxy), use the **ProxyProbe**. It is a safe-verb tool: it returns a database's
*schema*, column *profiles*, and *validates* a query, and **never returns a row value** - so it is safe
to run against production-like data.

Crucially, the probe works against a **deployed engine**, not the database directly. The topology is:

```
probe (dev machine)  --HTTPS-->  your KPI engine /adhoc-query  --relay-->  your proxy  -->  the DB
```

So for your own install you:
1. Deploy the KPI engine and point its proxy at your source DB (the normal onboarding).
2. Point the probe at **your** engine's API URL, with an Admin/TenantAdmin token, and have an operator
   open a **dev-access window** on that engine.
3. Run `schema` / `profile` / `validate` to learn the shape and design your cube.

There is **no probe "local mode" and no need for one** - the probe already reaches live data the right
way (through your engine + proxy); only the URL differs from DevCircle's own instance.

### Getting the probe

The probe is a **pre-compiled, self-contained CLI** - one download per OS, no .NET SDK and no source
needed (about 37 MB), served by a running engine from `/downloads/` (alongside the on-prem proxy).

> **Download it from the engine's WEB host - not the API host.** The binaries are served by the
> Blazor/web front-end, which on a split deployment is a *different* host than the API (e.g.
> `kpi.example.com` vs `kpi-api.example.com`; on a single-host setup they're the same). Hitting the API
> host returns `404`.

```bash
# Linux - replace the host with your engine's WEB host
curl -LO https://kpi.yourco.example/downloads/DevC.KPI.ProxyProbe-linux-x64
chmod +x DevC.KPI.ProxyProbe-linux-x64

# Windows: download https://kpi.yourco.example/downloads/DevC.KPI.ProxyProbe-win-x64.exe
```

Diagnosing a `404`: if the proxy download (`…/downloads/DevC.KPI.Proxy-linux-x64`) works but the
`ProxyProbe` one 404s, the engine image predates the probe download - ask the operator to update the
engine (**do not build the probe from source**). You can also grab it from DevCircle's public instance
once that is on a current build.

Run it against your engine. Here `--url` is the **API** base (`…/api`) — which may differ from the web
host you downloaded from. It needs an Admin/TenantAdmin token and an open dev-access window on that engine:

```bash
./DevC.KPI.ProxyProbe-linux-x64 schema  --url https://kpi-api.yourco.example/api --tenant yourco --ds bmd
./DevC.KPI.ProxyProbe-linux-x64 profile --url https://kpi-api.yourco.example/api --tenant yourco --ds bmd --table invoices
```

> **A datasource must already exist to probe** — `--ds <id>` names one. If the source DB has a proxy + a
> secret on the server but no datasource YAML yet, first add a minimal
> `config/<tenant>/datasources/<id>.yaml` binding `proxy:` + `secret:` (no cube needed — see
> [03](03-datasources-and-secrets.md)), deploy it, then probe that `--ds`.

The binary is a thin HTTPS client - it holds no credentials and can only reach data through your
engine + proxy while a dev-access window is open.

### A wrapper so you don't retype flags

`tools/kpi-probe.sh` in this repo wraps the binary. Set four env vars once, then just call the verbs:

```bash
export KPI_PROBE_BIN="$HOME/bin/devc-kpi-probe"          # the downloaded binary
export KPI_PROBE_URL="https://kpi.yourco.example/api"
export KPI_PROBE_TENANT="yourco"
export KPI_PROBE_TOKEN="<bearer>"                        # or set KPI_RELAY_KEY instead

./tools/kpi-probe.sh schema  --ds bmd
./tools/kpi-probe.sh profile --ds bmd --table invoices
```

### Authentication & the dev-access window

- **Auth** - one of: a **TenantAdmin bearer token** (`--token`, or `KPI_PROBE_TOKEN` for the wrapper),
  or a **relay key** your operator minted (`KPI_RELAY_KEY`). Your engine checks it.
- **Dev-access window** - the safe verbs only work while an operator has opened a dev-access window on
  the target proxy (Proxies view -> Enable 15/30/60 min). A closed window returns `403`. That is the
  deliberate gate: from outside, the data stays unreadable unless someone inside opens the window.

## Summary

| Situation | Tool |
|-----------|------|
| Developers can read the source DB | Their own SQL client (A) |
| Making the cube correct | SDK test kit - `InMemoryBuildDataAccess` (B) - always |
| Developers cannot read the source DB | ProxyProbe against your deployed engine + proxy (C) |
