# 06 - Working with Claude (the authoring skill)

There is a Claude Code skill, **`authoring-kpi-plugin`**, that knows the SDK surface and the
plugin-authoring workflow. It makes Claude far more reliable at writing cubes, widgets and the YAML
binding.

The skill is **not vendored in this repo** - it ships inside the **`DevC.KPI.Reporting.Sdk`** NuGet
package (single source of truth, so it can never drift from a stale copy). You already restore that
package to build a plugin, so the skill is in your NuGet cache.

## Install it

After restoring the SDK (any `dotnet build`/`dotnet restore` here does this), run the installer that
ships in the package:

```bash
# macOS/Linux
~/.nuget/packages/devc.kpi.reporting.sdk/<version>/skills/authoring-kpi-plugin/install.sh
# Windows (PowerShell)
& "$env:USERPROFILE/.nuget/packages/devc.kpi.reporting.sdk/<version>/skills/authoring-kpi-plugin/install.ps1"
```

The installer copies the skill into `~/.claude/skills/` (global) or a repo's `.claude/skills/`, where
Claude Code picks it up. Re-run it after upgrading the SDK to get the matching skill version.

## Using it

Once installed, just ask - e.g. "add a KPI tile showing average order value" or "add a cube over my
`orders` table with revenue per month". Claude invokes the skill, which pulls in the exact SDK
signatures (`references/sdk-surface.md`) and the authoring procedure so it does not guess.

## Good prompts

- "Add a `postgres` datasource for my `sales` schema and a monthly-revenue line chart."
- "Write a cube unit test that feeds three sample rows and asserts January's revenue."
- "Convert this plugin from `ForTenants` to `Shared` and update the config."

## What Claude should always do here (also in [../CLAUDE.md](../CLAUDE.md))

- Compile against the SDK only; never reference the engine.
- Keep `ResultNames` in sync with the queries; `DataSourceId` = datasource `id`, `builder:` = cube `Key`.
- Merge `context.RawOverrides` last when building a chart.
- Remember the tenant slug must equal your provisioned tenant name.
- Verify with `dotnet test -c Release`.
