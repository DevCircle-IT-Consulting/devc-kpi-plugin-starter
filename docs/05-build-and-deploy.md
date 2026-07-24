# 05 - Build, test & deploy

> **A plugin's cubes and widgets live in its compiled DLL — so you see *nothing* in the app until that
> DLL is deployed.** Reports and widgets only appear once the plugin assembly is in the engine's plugins
> volume (`/srv/kpi/plugins/<PluginId>/`) and the API has restarted; having the config YAML present is
> not enough. Confirm with `docker compose logs api | grep "Reporting plugin"` — your plugin must be
> listed. *(Exploring the source DB with the ProxyProbe is the exception: it needs only the datasource
> config, not the DLL — see [07](07-exploring-your-database.md).)*

## Build & test (your whole CI)

```bash
dotnet test -c Release      # builds the plugin + runs the cube/widget tests
```

A plugin repo's CI needs nothing more: green here proves your plugin compiles against the published
SDK and its cube math holds. There is no engine to run in CI.

## Run it in a real engine (local dev)

You do not run the engine from this repo. Point a **running** engine at your build output:

- `Reporting:PluginsPaths` -> your plugin's build folder, e.g.
  `src/DevC.KPI.Plugins.Acme/bin/Debug/net10.0`. It is additive to the engine's built-in example
  plugins.
- `Reporting:ConfigPath` / `Reporting:ConfigPaths` -> your `config/` folder so the engine finds your
  tenant's reports.

Then edit -> `dotnet build` -> restart the engine to pick up the new DLL (report/datasource YAML
hot-reloads; a changed DLL needs a restart).

## Package a deploy bundle - `deploy/build-bundle.sh`

```bash
bash deploy/build-bundle.sh Acme acme      # <Plugin> <tenant>
```

It `dotnet publish`es the plugin and assembles `dist/bundles/Acme/`:

```
plugins/Acme/DevC.KPI.Plugins.Acme.dll (+ .deps.json + any PRIVATE deps)
config/acme/            (your datasources + reports + plugins.yaml)
secrets/acme.example.json   (if present - a template; real secrets filled per install)
```

The bundle ships **only your plugin assembly and its genuinely-private dependencies**. The engine,
the SDK, LinqCube, DB drivers and the framework are provided by the base image and are filtered out
(the runtime load context unifies them to the host's copy).

## Deploy (self-deploy model)

Each plugin repo deploys **itself** into the engine's plugins volume - it does not go through the
engine repo. A plugin repo's CI `deploy` job, in effect:

```bash
bash deploy/build-bundle.sh Acme acme
rsync -az --delete dist/bundles/Acme/plugins/Acme/   deploy@<host>:/srv/kpi/plugins/Acme/
rsync -az --delete dist/bundles/Acme/config/acme/    deploy@<host>:/srv/kpi/config/acme/
ssh deploy@<host> "cd /srv/kpi && docker compose restart api"
```

- The `--delete` is scoped to **your** `plugins/Acme/` and `config/acme/` - it never touches other
  plugins.
- The DLL needs the `restart api`; config alone hot-reloads.
- Confirm it loaded: `docker compose logs api | grep "Reporting plugin"` shows each plugin and the
  path it loaded from; `grep "failed to load"` catches a bad DLL (skipped, not fatal).

> If you self-host your own engine, `<host>` and the deploy key are yours; if DevCircle hosts you,
> they provide the target. Either way the shape is identical.

## Versioning the SDK

Your plugin pins a specific `DevC.KPI.Reporting.Sdk` version. Bump it when you want newer contracts;
rebuild and redeploy. A plugin compiled against an older SDK keeps working as long as the engine
image is compatible - if a deploy crosses a compatibility boundary, redeploy the plugin against the
matching SDK.

## Troubleshooting: reading config errors

The engine logs each tenant's config result at startup and on every hot-reload. **Check the API log
first** — `docker compose logs api | grep -Ei "Reporting config|Reporting plugin"`. A clean load reads
`Reporting config [<tenant>]: loaded N datasource(s), M report(s).`; problems are `WRN` lines naming the
file and field. Common ones:

| Log message / symptom | Cause | Fix |
|---|---|---|
| `Required value 'id' / 'builder' / 'type' / 'secret' / 'loadWindow' is missing` | The datasource YAML is incomplete — every one of these is required (even for a probe-only binding; `id` is explicit, not the filename) | Add the missing field ([03](03-datasources-and-secrets.md), [07](07-exploring-your-database.md)) |
| `builder 'X' … no loaded plugin provides` / `Widget 'X' is not provided by any loaded plugin` | The plugin that defines it isn't deployed | Build + copy the DLL to `/srv/kpi/plugins/<id>/`, `restart api` (top of this doc) |
| The DLL **is** in `/srv/kpi/plugins/<id>/` but the startup log lists only the built-in plugins (not yours), often with a `failed to load … : {Error}` or `(type load)` WARN | The assembly loaded but its plugin type couldn't be created — usually the plugin's `DevC.KPI.Reporting.Sdk` version differs from the engine's (a binary mismatch), a missing dependency, or the plugin class isn't `public` with a parameterless constructor | Read the `(type load)` WARN — it names the failing type/method. Align the SDK version to the engine's and rebuild (see *Versioning the SDK* above); ensure the plugin class is `public sealed class …Plugin : IReportingPlugin` with no constructor |
| `… loaded but not enabled for this tenant — add its plugin id to plugins.yaml` | A **Shared** plugin the tenant hasn't opted into | Add its id to `config/<tenant>/plugins.yaml` ([02](02-config-reference.md)) |
| `uses proxy 'X', which is not declared … or not scoped to this tenant` | `proxies.yaml` (server-owned) lacks the entry or its scope excludes this tenant | Add/scope the proxy in `/srv/kpi/config/proxies.yaml` ([09](09-proxy-to-another-stack.md)) |
| No datasources/reports at all; only the demo shows | `Reporting:ConfigPath` empty → the mounted `/srv/kpi/config` isn't scanned | Set `ConfigPath` to `/srv/kpi/config` ([02](02-config-reference.md)) |
| Plugin loaded, config clean, but a user sees **no report** | Report access is a per-user grant (no admin bypass), and a **TenantAdmin has no report list** | Grant the report (Users → Report rights) to a User/Admin in that tenant ([11](11-users-and-report-rights.md)) |

*(The proxy and datasource-connection secrets live server-side; a widget/report needs the plugin DLL, but
**probing** the DB needs only the datasource config — see [07](07-exploring-your-database.md).)*
