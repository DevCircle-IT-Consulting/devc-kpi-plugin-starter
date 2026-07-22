# Agent context - DevC.KPI plugin starter kit

This repo is a kit for authoring **DevC.KPI reporting plugins**. If you are helping someone write or
extend a plugin, use the bundled skill and the docs here.

## Use the skill

The `authoring-kpi-plugin` skill ships inside the `DevC.KPI.Reporting.Sdk` NuGet package (it is not
vendored in this repo - see [docs/06](docs/06-working-with-claude.md) to install it from the package).
Invoke it whenever the task is about a cube, dimension, measure, widget, chart, KPI tile, table, plugin
registration, or the YAML datasource/report binding. Its `references/sdk-surface.md` has the exact
public signatures - prefer it over guessing.

## The model in one paragraph

A plugin is a `net10.0` library that references **only** `DevC.KPI.Reporting.Sdk` (public on
nuget.org). It implements `IReportingPlugin.Register(...)` to add: `DataSourceBuilder<TFact>` cubes,
conformed dimensions, and `Widget`/`StaticWidget` widgets. YAML under `config/<tenant>/` wires a
datasource (`builder:` == the cube's `Key`) and places widgets on report pages. The licensed engine
loads the compiled DLL at runtime; you never reference or run the engine.

## Ground rules (things agents reliably miss)

- **Compile against the SDK only.** Never add a reference to the engine or copy engine internals. If a
  type isn't in the SDK, it isn't part of the contract.
- **One widget = one output kind**: `WidgetOutput.Chart` | `SingleValue` | `Table` | `Text`. A static,
  data-free widget derives from `StaticWidget`; a data-bound one from `Widget` (declare `DataSourceId`,
  `ResultName`, `DateAxis`).
- **`ResultName` must be one of the cube's `ResultNames`**, and `ResultNames` must equal the query names
  `DefineQueries` yields (the base `Build` asserts this).
- **`DataSourceId` is the datasource YAML `id:`**, NOT the cube `Key`. `builder:` in the YAML is the `Key`.
- **Fact streams are lazy** - never `.ToList()` a `LoadFacts` projection; return `IEnumerable` and let
  the engine enumerate once.
- **The tenant slug must equal the provisioned tenant name** - `config/<tenant>/` and
  `ForTenants("<tenant>")` are exact-string matched by the engine. It is not a display label.
- **Merge `context.RawOverrides` last** when building an ECharts option, so per-placement YAML
  `echarts:` overrides win.
- **Unit-test cubes with the SDK kit**: build a `BuildContext` with `Data = new InMemoryBuildDataAccess().Add(rows)`
  (DB cubes) or `EmptyBuildDataAccess.Instance` (custom cubes), call `cube.Build(context)`, assert measures.

## Where to look

- `template/` - the minimal shape to copy (one widget).
- `example/` - a real cube + 3 widget kinds + a filter + tests. The best worked reference.
- `docs/01-plugin-anatomy.md` and `docs/04-widgets-and-charts.md` - the how-to.
- the skill's `references/sdk-surface.md` (installed from the SDK package) - exact signatures.

## Verify

`dotnet test -c Release` in `template/` or `example/`. Both build against the published SDK and must
stay green. There is no engine to run here.
