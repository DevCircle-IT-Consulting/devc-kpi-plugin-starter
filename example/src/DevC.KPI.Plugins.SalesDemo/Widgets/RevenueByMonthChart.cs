namespace DevC.KPI.Plugins.SalesDemo.Widgets
{
    using System.Globalization;
    using System.Linq;
    using dasz.LinqCube;
    using DevC.KPI.Reporting.Cubes;
    using DevC.KPI.Reporting.ECharts;
    using DevC.KPI.Reporting.Plugins;

    /// <summary>Chart output: revenue per month as a line, following the report's date filter.</summary>
    public sealed class RevenueByMonthChart : Widget
    {
        public override string Title => "Revenue per month";
        public override string Key => "RevenueByMonthChart";                    // report YAML `widget:`
        public override string DataSourceId => "sales";                         // datasource YAML `id:`
        public override string ResultName => SalesCube.RevenueByMonth;          // one of the cube's ResultNames
        public override DateAxisMode DateAxis => DateAxisMode.UntilToday;       // normal time series

        public override WidgetOutput Render(QueryResult result, WidgetContext context)
        {
            var months = result.Dimension("Date").Leaves()
                .WithinAnyDateRange(context.DateFilter)                         // empty filter = all months
                .ToList();
            var labels = months.Select(l => l.GetDateTimeEntry()!.Min.ToString("yyyy-MM", CultureInfo.InvariantCulture));
            var values = months.Select(l => (double)l.Measure("Revenue"));

            var option = EChart.Line().Category(labels).Series("Revenue", values).Tooltip().Build()
                .Merge(context.RawOverrides);                                   // honor per-placement echarts: overrides - always last
            return WidgetOutput.Chart(option);
        }
    }
}
