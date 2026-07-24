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

> **Download from where you open the KPI web app in your browser — the web front-end, at `/downloads/`
> (the web root, never under `/api`).** Which URL that is depends on the deployment:
> - **Single DNS** (the API lives under `/api` on the same host): `https://<host>/downloads/…`.
> - **Two DNS** (the API has its own name at the root): the **web** name, `https://<web-host>/downloads/…`
>   — not the API name.
>
> Fetching `/downloads/` from under the API path, or from the API's own hostname, returns `404`.

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

Run it against your engine. `--url` is the **API base** — exactly the value your client uses as `ApiUrl`
(in `client/appsettings.Production.json`): `https://<host>/api` on a single-DNS install, or the API's own
root name (e.g. `https://api.yourco.example`) on a two-DNS install. It needs an Admin/TenantAdmin token
and an open dev-access window on that engine:

```bash
# single-DNS example (API under /api); for two-DNS use the API's own root name with no /api
./DevC.KPI.ProxyProbe-linux-x64 schema  --url https://kpi.yourco.example/api --tenant yourco --ds bmd
./DevC.KPI.ProxyProbe-linux-x64 profile --url https://kpi.yourco.example/api --tenant yourco --ds bmd --table invoices
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

### Authentication

The probe authenticates to the engine one of two ways. **Prefer the relay key** — you mint it once and
forget it; a bearer token has to be re-extracted from the browser every session.

**Option 1 — relay key (recommended).** A long-lived credential minted once per developer. Two-secret
model: the plaintext key stays on your box, the server stores only its SHA-256 hash.

1. **Operator mints it** with the probe's `keygen` (it never talks to a DB — pure key generation):
   ```bash
   ./DevC.KPI.ProxyProbe-linux-x64 keygen --name alice
   ```
   It prints a **key** (for the dev box) and a **hash** (for the server), and reminds you where each goes.
2. **On your (dev) box**, make the key resolvable once — the probe looks, in order, at: `--relay-key` /
   `--relay-key-file` flags, the `KPI_RELAY_KEY` / `KPI_RELAY_KEY_FILE` env vars, a `secrets/relay-key`
   file in the repo (found by walking up from the working dir), then `~/.devc-kpi/relay-key` in your user
   profile. Two easy, safe choices:
   - **In the repo:** save it to `secrets/relay-key` (and keep the downloaded probe in `tools/`). The
     generated plugin's `.gitignore` already excludes `secrets/` and the probe binary, so neither is
     committed — and the probe auto-discovers the key when you run from the repo root. No flag needed.
   - **In your profile:** `~/.devc-kpi/relay-key`, or `export KPI_RELAY_KEY="<the key>"` — shared across
     repos.

   Either way it's set once — no per-session step.
3. **On the server**, the operator adds the **hash** under `Reporting:RelayKeys:<name>` in the app-wide
   secret file `secrets/_app.json`. A plain hash string = a **human** (all safe verbs); an object
   `{ "hash": "<hash>", "agent": true }` = an **agent** (safe verbs only, no ad-hoc query). New secret
   file / entry → `docker compose restart api`.

**Option 2 — TenantAdmin bearer token (quick one-off).** Sign in to the web app as a TenantAdmin, copy a
bearer token from the browser, and pass it as `--token <jwt>` (or `KPI_PROBE_TOKEN` for the wrapper).
Nothing to set up server-side — but tokens are **short-lived**, so you re-extract one each session, which
gets tedious for repeated exploration. Fine for a quick first look; switch to a relay key once you're
iterating.

> **Don't confuse this with the _proxy_ relay key** in [09](09-proxy-to-another-stack.md). That one
> authenticates the **proxy's own** outbound connection to the engine (it must exist for the proxy to be
> Connected at all). *This* one authenticates **you** running the probe. Different credentials, different
> secret slots (`Reporting:RelayKeys:*` here vs `Reporting:Secrets:PROXY_*` there).

### The dev-access window

Either way, the safe verbs only work while an operator has an open **dev-access window** on the target
proxy (Proxies view → Enable 15/30/60 min). A closed window returns `403` — the deliberate gate: from
outside, the data stays unreadable unless someone inside opens the window.

## Summary

| Situation | Tool |
|-----------|------|
| Developers can read the source DB | Their own SQL client (A) |
| Making the cube correct | SDK test kit - `InMemoryBuildDataAccess` (B) - always |
| Developers cannot read the source DB | ProxyProbe against your deployed engine + proxy (C) |
