# DevC.KPI plugin examples & template

Everything you need to build your own **DevC.KPI reporting plugin**: a minimal starter you can
generate from, a fully worked example to learn from, the authoring docs, and the Claude Code skill
that helps you (or an agent) write plugins.

A plugin is a small .NET library that adds **cubes** (aggregations) and **widgets** (charts, KPI
tiles, tables) to the DevC.KPI reporting engine. You compile it against one public NuGet package -
**`DevC.KPI.Reporting.Sdk`** - and the licensed engine loads your compiled DLL at runtime. You never
need the engine source.

## What's in here

| Path | What it is |
|------|------------|
| [`template/`](template) | The **minimal starter** - one static "hello world" widget. Also the `dotnet new` template source. Start new plugins here. |
| [`example/`](example) | The **worked example** - a real cube with sample data, a line chart, a KPI tile, a table, a date filter, and unit tests. Read it to learn. |
| [`docs/`](docs) | The authoring guide (getting started, plugin anatomy, config, datasources, widgets/charts, deploy, exploring your data, working with Claude). |
| [`tools/`](tools) | `kpi-probe.sh` - a convenience wrapper around the ProxyProbe DB-explorer CLI. |
| [`pack/`](pack) | Packs the template into a distributable `dotnet new` nupkg (optional). |

A Claude Code skill (`authoring-kpi-plugin`) that knows the SDK surface is **not vendored here** - it
ships inside the `DevC.KPI.Reporting.Sdk` NuGet package (single source of truth). Install it from there;
see [docs/06](docs/06-working-with-claude.md).

## Quick start

**Look at the example** (needs the .NET 10 SDK; the DevC SDK restores from nuget.org):

```bash
cd example
dotnet test -c Release      # builds + runs the cube tests
```

**Start your own plugin** from the template:

```bash
dotnet new install ./template
dotnet new devckpi-plugin -n Acme                     # plugin DevC.KPI.Plugins.Acme, tenant "acme"
dotnet new devckpi-plugin -n SalesReports --tenant contoso   # decoupled name / tenant
```

Then read [docs/00-getting-started.md](docs/00-getting-started.md).

## Two things people get wrong

- **The tenant slug is not a label - it must equal your provisioned tenant name.** `config/<tenant>/`
  and `PluginScope.ForTenants("<tenant>")` are matched by exact string against the tenant the engine
  knows. Ask DevCircle for your tenant name; it is not derived or translated. (`--tenant` lets you set
  it independently of the plugin name.)
- **You build the plugin; you don't run the engine.** Your CI just needs `dotnet build`/`dotnet test`
  to pass. Running it means dropping the DLL into a running engine's plugins volume - see
  [docs/05-build-and-deploy.md](docs/05-build-and-deploy.md).

## Using Claude Code here

The `authoring-kpi-plugin` skill ships inside the `DevC.KPI.Reporting.Sdk` NuGet package. See
[docs/06-working-with-claude.md](docs/06-working-with-claude.md) to install and use it.

## SDK version

The projects here pin `DevC.KPI.Reporting.Sdk` **1.3.1**. Bump to the latest published version when
you want newer contracts; older plugins keep working against their pinned version.
