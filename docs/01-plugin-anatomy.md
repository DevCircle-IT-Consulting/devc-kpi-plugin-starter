# 01 - Plugin anatomy

A plugin has five kinds of piece. The `example/` plugin has all of them; the `template/` starter has
only the plugin + one widget.

## 1. The plugin entry point - `IReportingPlugin`

The engine scans loaded assemblies for `IReportingPlugin` and calls `Register(...)` once at startup.

```csharp
public sealed class SalesDemoPlugin : IReportingPlugin
{
    public string Name  => "DevC.KPI.Plugins.SalesDemo";   // diagnostics
    public string Id    => "SalesDemo";                    // stable id (Shared plugins list it in plugins.yaml)
    public PluginScope Scope => PluginScope.ForTenants("salesdemo");

    public void Register(PluginRegistration r) =>
        r.AddBuilder(new SalesCube())
         .AddWidget(new Widgets.RevenueByMonthChart())
         .AddDimension(ConformedDimensionInfo.Date("Date", 2015, DateTime.Today.Year));
}
```

**Scope** decides who gets the plugin:
- `ForTenants("acme")` - auto-on for the named tenant(s). Best for a customer-specific plugin.
- `Shared` - a cross-customer feature; each tenant opts in via `plugins.yaml` (`enabled: [Id]`).
- `Global` - always on, content-free (rare).

## 2. The fact row

One record as your data source returns it - a plain class:

```csharp
public sealed class SalesRow
{
    public DateTime OrderDate { get; init; }
    public string   Team      { get; init; } = "";
    public decimal  Amount    { get; init; }
}
```

## 3. The cube - `DataSourceBuilder<TFact>`

Fetches facts once and defines named query results (aggregations). LinqCube runs a **single pass**
over the facts.

```csharp
public sealed class SalesCube : DataSourceBuilder<SalesRow>
{
    public const string RevenueByMonth = "revenueByMonth";
    public override string Key => "SalesCube";                       // == datasource YAML `builder:`
    public override IReadOnlyCollection<string> ResultNames { get; } = [RevenueByMonth];

    protected override IEnumerable<SalesRow> LoadFacts(BuildContext ctx)
        => ctx.Sql<SalesRow>("select ... where order_date >= @from", new { from = ctx.LoadWindow.From });
        // (a `custom` cube returns its own rows here instead)

    protected override IEnumerable<Query<SalesRow>> DefineQueries(BuildContext ctx)
    {
        var date = new DateDimension();
        yield return new Query<SalesRow>(RevenueByMonth)
            .WithChainedDimension(date.For<SalesRow>(r => r.OrderDate))
            .WithMeasure(Measure.Sum<SalesRow>("Revenue", r => r.Amount));
    }
}
```

Rules:
- `ResultNames` **must equal** the query names `DefineQueries` yields (the base `Build` asserts it).
- `LoadFacts` is **lazy** - return `IEnumerable`, never `.ToList()` a large projection.
- Keep the sparse default (`BuildSparse => true`) and zero-fill from your domain in the widget, unless
  the cube is tiny.

## 4. Dimensions & measures

**Dimensions** (`Dim.*`) are the axes you slice by; a conformed dimension is a small wrapper so many
cubes share the same axis name (a report filter bound to `Date` slices every cube stamped with it):

```csharp
Dim.YearMonth<TFact>(name, sel, fromYear, toYear)   // the Date axis
Dim.Enum<TFact>(name, sel, params values)           // a fixed enum (Team, Channel, ...)
Dim.Bool / Dim.Years / Dim.YearQuarterMonth / Dim.Partition ...
```

**Measures** (`Measure.*`) are what you aggregate:

```csharp
Measure.Sum / SumInt / SumDouble / Count / Where(...)
// no average - compute it at render time from a sum and a count.
```

Register each dimension's filter domain so reports may bind filters to it:

```csharp
.AddDimension(ConformedDimensionInfo.Date("Date", 2015, DateTime.Today.Year))
.AddDimension(ConformedDimensionInfo.Enum("Team", "West", "East", "North", "South"))
.AddDimension(ConformedDimensionInfo.DynamicEnum("Customer"))   // domain filled from live data
```

## 5. Widgets

One widget produces one output. See [04-widgets-and-charts.md](04-widgets-and-charts.md). The
smallest is a static text card (`StaticWidget`); a data-bound widget derives from `Widget` and reads
the cube's `QueryResult`.

## Putting it together

`example/src/DevC.KPI.Plugins.SalesDemo/` is exactly this anatomy at minimum useful size. Read it
next to this page.
