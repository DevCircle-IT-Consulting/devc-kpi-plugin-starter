# 04 - Widgets & charts

A widget produces exactly **one** output. Pick the base class by whether it reads data.

## Static (no data) - `StaticWidget`

For help text, notes, intros. The engine renders it with no cube build, so it works even if a
database is down.

```csharp
public sealed class HelloWorldWidget : StaticWidget
{
    public override string Key => "HelloWorld";
    public override WidgetOutput Render(WidgetContext context)
        => WidgetOutput.Text("Hello, world!\n\nA static card.");
}
```

## Data-bound - `Widget`

Declares which datasource + query result it reads, then builds its output from the `QueryResult`.

```csharp
public sealed class RevenueByMonthChart : Widget
{
    public override string Title        => "Revenue per month";
    public override string Key          => "RevenueByMonthChart";   // report YAML `widget:`
    public override string DataSourceId => "sales";                 // datasource YAML `id:` (NOT the cube Key)
    public override string ResultName   => SalesCube.RevenueByMonth;// one of the cube's ResultNames
    public override DateAxisMode DateAxis => DateAxisMode.UntilToday;// UntilToday (normal) | FullRange

    public override WidgetOutput Render(QueryResult result, WidgetContext context) { ... }
}
```

## The four output kinds

```csharp
WidgetOutput.Chart(EChartsOption option)                              // any ECharts chart
WidgetOutput.SingleValue(value, label, unit, deltaPercent, spark, ...) // a KPI tile
WidgetOutput.Table(columns, rows, decimals)                          // a table
WidgetOutput.Text(text, heading)                                     // a text card
```

## Reading the `QueryResult`

```csharp
result.Dimension("Date")                       // root node of a dimension
      .Leaves()                                // deepest entries (e.g. months)
      .WithinAnyDateRange(context.DateFilter)  // follow the report's date picker (empty = all)
// per entry:
leaf.GetDateTimeEntry()!.Min                   // the period start (date leaves)
leaf.DimensionEntry.Label                      // the entry label (enum entries, e.g. a team)
leaf.Measure("Revenue")                        // read a measure at that node (decimal)
// enum slice:  .WithinEntries(context.EnumSelections["Team"])   // empty = all
```

The three example widgets show all three patterns: a Date-axis chart, a summed KPI, and a per-Team
table (`example/src/DevC.KPI.Plugins.SalesDemo/Widgets/`).

## Building a chart - the `EChart` fluent builder

```csharp
var option = EChart.Line()                     // Line | Bar | Pie | Funnel | Treemap
    .Category(labels)                          // x-axis labels
    .Series("Revenue", values)                 // one series (repeatable)
    .Tooltip()                                 // "axis" default; "item" for pies
    .Build()
    .Merge(context.RawOverrides);              // ALWAYS last - lets YAML `echarts:` overrides win
return WidgetOutput.Chart(option);
```

For pies use `.Slices(...)`, funnels `.Stages(...)`, treemaps `.Tree(...)`. Anything the fluent
builder does not cover, set via a raw `echarts:` block in the report YAML placement - it merges last.

## Honouring filters

- **Date**: filter your Date leaves with `.WithinAnyDateRange(context.DateFilter)`. An empty filter
  means "all", so this is always safe to call.
- **Enum** (e.g. Team): `.WithinEntries(context.EnumSelections["Team"])`.
- **Raw overrides**: always `.Merge(context.RawOverrides)` as the final step of building an option.

## The output-kind reference

The engine's Demo plugin ships a full "widget catalogue" report that renders every ECharts type with
an explanation card each. If you have access to a running demo tenant, open it - it is the most
complete visual reference. The three kinds here (chart / value / table) cover the vast majority of
real reports.
