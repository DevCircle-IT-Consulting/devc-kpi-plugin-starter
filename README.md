# DevC.KPI — reporting plugin starter kit

Build your own reports for **[DevC.KPI](https://kpi.devcircle.at/)**, the KPI reporting platform from
**[DevCircle](https://devcircle.at/)**. This repository is the developer kit — a `dotnet new` template,
a fully worked example, the authoring docs, and a Claude Code skill — everything you need to extend
DevC.KPI with your own reporting plugins.

**Links:** [Live product](https://kpi.devcircle.at/) · [DevC.KPI solution overview](https://devcircle.at/loesungen/devc-kpi) · [DevCircle IT Consulting](https://devcircle.at/)

## About DevC.KPI

> **Decide with numbers you can trust.**

DevC.KPI brings the key figures from your existing systems together into clear, always-current reports
that your whole team shares — one trustworthy version of the truth, instead of conflicting spreadsheets
across departments. It reads the databases you already run (no re-keying), turns them into interactive
dashboards, and lets you filter once to re-slice a whole report across every data source at a click.
Reports are shared by link — viewers need no login — every change is versioned for a clean audit trail,
and it's hosted in the EU, GDPR-compliant, with maintenance and support included.

**Plugins are how DevC.KPI is extended** — and building them is what this repo is for. A plugin is a
small .NET library that adds *cubes* (aggregations) and *widgets* (charts, KPI tiles, tables) for your
own data. You compile it against one public NuGet package — **`DevC.KPI.Reporting.Sdk`** — and the
licensed engine loads your compiled DLL at runtime. You never need the engine source.

See it live at **[kpi.devcircle.at](https://kpi.devcircle.at/)**, or read the
**[solution overview](https://devcircle.at/loesungen/devc-kpi)**.

---

## What's in here

| Path | What it is |
|------|------------|
| [`template/`](template) | The **minimal starter** - one static "hello world" widget. Also the `dotnet new` template source. Start new plugins here. |
| [`example/`](example) | The **worked example** - a real cube with sample data, a line chart, a KPI tile, a table, a date filter, and unit tests. Read it to learn. |
| [`docs/`](docs) | The guide, in two tracks: **building reports** (plugin authoring) and **operating the server** (admin). See [Documentation](#documentation) below. |
| [`tools/`](tools) | `kpi-probe.sh` - a convenience wrapper around the ProxyProbe DB-explorer CLI. |
| [`server/`](server) | Run the DevC.KPI engine (Docker Compose + templates). See [docs/08](docs/08-run-the-server.md). |
| [`proxy/`](proxy) | Run the on-prem proxy next to another Docker stack's database. See [docs/09](docs/09-proxy-to-another-stack.md). |
| [`pack/`](pack) | Packs the template into a distributable `dotnet new` nupkg (optional). |

A Claude Code skill (`authoring-kpi-plugin`) that knows the SDK surface is **not vendored here** - it
ships inside the `DevC.KPI.Reporting.Sdk` NuGet package (single source of truth). Install it from there;
see [docs/06](docs/06-working-with-claude.md).

## Documentation

The docs come in two tracks — pick the one that matches your job.

**Building reports** (plugin authors):
[00 Getting started](docs/00-getting-started.md) ·
[01 Plugin anatomy](docs/01-plugin-anatomy.md) ·
[02 Config reference](docs/02-config-reference.md) ·
[03 Datasources & secrets](docs/03-datasources-and-secrets.md) ·
[04 Widgets & charts](docs/04-widgets-and-charts.md) ·
[05 Build & deploy](docs/05-build-and-deploy.md) ·
[06 Working with Claude](docs/06-working-with-claude.md) ·
[07 Exploring your database](docs/07-exploring-your-database.md)

**Operating the server** (administrators):
[08 Run the server](docs/08-run-the-server.md) ·
[10 Operating overview](docs/10-operating-overview.md) ·
[11 Users & report rights](docs/11-users-and-report-rights.md) ·
[12 Sharing reports by link](docs/12-sharing-reports.md) ·
[13 Operations (backup/update)](docs/13-operations.md) ·
[14 Tenants & admin tools](docs/14-tenants-and-admin-tools.md) ·
[09 On-prem proxy](docs/09-proxy-to-another-stack.md)

## Quick start

**Run the engine** (needed to see any report live) — stand up the server on one box with the
[`server/`](server) stack; steps in [docs/08-run-the-server.md](docs/08-run-the-server.md). The engine
images are licence-gated, so DevCircle provides the registry credential + image tag.

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

The projects here pin `DevC.KPI.Reporting.Sdk` **1.3.2**. Bump to the latest published version when
you want newer contracts; older plugins keep working against their pinned version (the SDK's binding
identity is fixed per major, so any `1.x` plugin loads on any `1.x` engine).

---

<sub>A product of **[DevCircle IT Consulting GmbH](https://devcircle.at/)** · [DevC.KPI](https://kpi.devcircle.at/) · [Solution overview](https://devcircle.at/loesungen/devc-kpi)</sub>
