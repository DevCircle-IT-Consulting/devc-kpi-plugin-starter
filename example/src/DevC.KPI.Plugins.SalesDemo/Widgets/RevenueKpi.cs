namespace DevC.KPI.Plugins.SalesDemo.Widgets
{
    using System.Linq;
    using dasz.LinqCube;
    using DevC.KPI.Reporting.Cubes;
    using DevC.KPI.Reporting.Plugins;

    /// <summary>Single-value (KPI tile) output: the grand-total revenue in the selected period.
    /// Revenue is additive, so summing the in-range month leaves equals the exact total.</summary>
    public sealed class RevenueKpi : Widget
    {
        public override string Title => "Total revenue";
        public override string Key => "RevenueKpi";
        public override string DataSourceId => "sales";
        public override string ResultName => SalesCube.TotalRevenue;
        public override DateAxisMode DateAxis => DateAxisMode.UntilToday;

        public override WidgetOutput Render(QueryResult result, WidgetContext context)
        {
            var total = result.Dimension("Date").Leaves()
                .WithinAnyDateRange(context.DateFilter)
                .Sum(l => (double)l.Measure("Revenue"));

            return WidgetOutput.SingleValue(total, unit: "€", decimals: 0);
        }
    }
}
