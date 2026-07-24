# DevC.KPI.Plugins.HelloWorld (the starter template)

The minimal DevC.KPI reporting plugin **and** the `dotnet new` template source. It registers
**one** widget - a static "Hello, world!" text card - so it builds and loads with no database.
Generate a fresh, renamed plugin from it instead of copying by hand (see the repo
[README](https://github.com/DevCircle-IT-Consulting/devc-kpi-plugin-starter)).

```
template/                                   # a complete, buildable plugin repo (+ .template.config)
├── DevC.KPI.Plugins.HelloWorld.slnx
├── NuGet.config                            # nuget.org only (the SDK is public there)
├── .template.config/template.json          # makes this a `dotnet new` template
├── src/
│   ├── DevC.KPI.Plugins.HelloWorld/
│   │   ├── DevC.KPI.Plugins.HelloWorld.csproj   # net10.0 lib, one ref: DevC.KPI.Reporting.Sdk
│   │   ├── HelloWorldPlugin.cs                   # IReportingPlugin: registers the widget
│   │   ├── Widgets/HelloWorldWidget.cs           # the widget (StaticWidget -> WidgetOutput.Text)
│   │   └── Reference/DataExample.cs.txt          # inert reference: cube + dimension + data chart
│   └── DevC.KPI.Plugins.HelloWorld.Tests/
│       ├── HelloWorldWidgetTests.cs              # a widget smoke test (runs with `dotnet test`)
│       └── Reference/CubeTestExample.cs.txt      # inert reference: cube unit test (InMemory kit)
├── config/helloworld/                      # this tenant's YAML
│   ├── plugins.yaml                        #   Shared-plugin opt-in (no-op for a ForTenants plugin)
│   ├── groups.yaml                         #   report grouping / order
│   ├── reports/hello.yaml                  #   the report: one page placing the widget
│   └── datasources/*.yaml.example          #   commented datasource reference (NOT loaded)
└── deploy/build-bundle.sh                  # packages plugin DLL + config into a deploy bundle
```

## Build & test

```bash
dotnet test -c Release      # builds the plugin and runs the widget smoke test
```

That is exactly what a plugin repo's CI runs to prove it compiles against the published SDK.

## Use it as a template

```bash
dotnet new install .                              # register (once)
dotnet new devckpi-plugin -n Acme                 # tenant defaults to "acme"
dotnet new devckpi-plugin -n SalesReports --tenant contoso   # decoupled plugin name / tenant
```

`-n` sets the plugin name (`DevC.KPI.Plugins.<name>`); `--tenant` sets the engine tenant the
plugin targets (`config/<tenant>/` + `PluginScope.ForTenants(...)`). See the
[repo README](https://github.com/DevCircle-IT-Consulting/devc-kpi-plugin-starter) and
[docs/00-getting-started.md](https://github.com/DevCircle-IT-Consulting/devc-kpi-plugin-starter/blob/main/docs/00-getting-started.md).

## Next steps

- **Add data**: follow `src/.../Reference/DataExample.cs.txt` and the commented block in
  `HelloWorldPlugin.cs`, then add a datasource YAML and place the widget on a page.
- **See a fuller example**: the
  [`example/`](https://github.com/DevCircle-IT-Consulting/devc-kpi-plugin-starter/tree/main/example) in
  the starter repo - a real cube with sample data, a chart, a KPI tile, a table, a date filter, and cube
  unit tests.

Full walkthrough:
[docs/01-plugin-anatomy.md](https://github.com/DevCircle-IT-Consulting/devc-kpi-plugin-starter/blob/main/docs/01-plugin-anatomy.md).
