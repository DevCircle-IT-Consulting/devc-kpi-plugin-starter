# 00 - Getting started

## What you are building

A **reporting plugin**: a small .NET class library that adds cubes and widgets to the DevC.KPI
engine. You compile it against one public NuGet package, `DevC.KPI.Reporting.Sdk`. The licensed
engine (a Docker image) loads your compiled DLL at runtime from a plugins volume. **You never
reference or run the engine to build a plugin** - `dotnet build` is the whole loop.

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` >= 10).
- Network access to **nuget.org** (that is where the SDK and its dependencies come from - no private
  feed, no auth).
- Your **tenant name**, provisioned by DevCircle (e.g. `contoso`). You need it for the config folder
  and `ForTenants(...)`. See [02-config-reference.md](02-config-reference.md).

## Path A - generate a new plugin from the template

```bash
dotnet new install ./template          # once per machine (or install the packed nupkg from pack/)
dotnet new devckpi-plugin -n Acme --tenant contoso
cd Acme
dotnet test -c Release
```

- `-n Acme` -> the plugin project `DevC.KPI.Plugins.Acme`.
- `--tenant contoso` -> `config/contoso/` + `PluginScope.ForTenants("contoso")`. Omit it and the
  tenant defaults to the lowercased project name.

You now have a plugin with one hello-world widget, a test project, and a commented reference showing
how to add data. Build on it following [01-plugin-anatomy.md](01-plugin-anatomy.md).

## Path B - learn from the worked example first

```bash
cd example
dotnet test -c Release
```

Read `example/` top to bottom (its README lists the reading order). It has a real cube with sample
data and three widget kinds, so it shows the full path the template deliberately leaves out.

## The inner loop

1. Edit your plugin (add a cube / widget) - compile against the SDK.
2. `dotnet test -c Release` - unit-test the cube math with the SDK test kit (no DB, no engine).
3. To see it live in a running engine, drop the built DLL into the engine's plugins volume and point
   its config at your `config/` - see [05-build-and-deploy.md](05-build-and-deploy.md).

## Where things live

- Code (cubes, dimensions, widgets, the `IReportingPlugin`): `src/DevC.KPI.Plugins.<name>/`.
- YAML (datasources, reports, groups, plugin opt-in): `config/<tenant>/`.
- Tests: `src/DevC.KPI.Plugins.<name>.Tests/`.
- Deploy bundling: `deploy/build-bundle.sh`.
