# 05 - Build, test & deploy

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
