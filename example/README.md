# DevC.KPI.Plugins.SalesDemo (the worked example)

A **rich, browsable** reference plugin - read it to see the full data path end to end. Unlike the
[template](../template) starter (one static widget), this one has a real cube with sample data and
three widget kinds, so you can see how charts, KPI tiles and tables are built.

It uses a `custom` datasource whose builder owns in-process sample data, so it needs **no database**
and runs anywhere.

```
example/
├── DevC.KPI.Plugins.SalesDemo.slnx
├── src/
│   ├── DevC.KPI.Plugins.SalesDemo/
│   │   ├── SalesRow.cs / SalesSampleData.cs      # the fact row + deterministic sample data
│   │   ├── SalesCube.cs                          # the cube: 3 named results, 3 measures
│   │   ├── Dimensions/{DateDimension,TeamDimension}.cs
│   │   ├── Widgets/
│   │   │   ├── RevenueByMonthChart.cs            # chart output  (ECharts line)
│   │   │   ├── RevenueKpi.cs                      # value output  (KPI tile)
│   │   │   └── RevenueByTeamTable.cs             # table output
│   │   └── SalesDemoPlugin.cs                     # registers cube + dimensions + widgets
│   └── DevC.KPI.Plugins.SalesDemo.Tests/
│       └── SalesCubeTests.cs                      # builds the cube, asserts aggregates
├── config/salesdemo/
│   ├── datasources/sales.yaml                     # active: builder SalesCube, type custom
│   ├── datasources/*.yaml.example                 # cookbook: postgres/mssql/custom/text, direct & via-proxy
│   ├── reports/sales.yaml                         # datetree filter + a page with all 3 widgets
│   ├── groups.yaml / plugins.yaml
├── config/proxies.example.yaml                    # what `proxy:` points at (server-owned; reference)
├── secrets/salesdemo.example.json                 # matching connection strings (placeholder creds)
└── deploy/build-bundle.sh
```

## Build & test

```bash
dotnet test -c Release
```

## What to read, in order

1. `SalesRow.cs` -> `SalesCube.cs` - the fact shape and how one LinqCube produces named results.
2. `Widgets/*.cs` - the three output kinds and how each reads the `QueryResult`.
3. `config/salesdemo/*.yaml` - how YAML wires the datasource + places widgets + adds the date filter.
4. `Tests/SalesCubeTests.cs` - the database-free unit-test loop.

Then follow the [docs](../docs) and start your own from the [template](../template).
