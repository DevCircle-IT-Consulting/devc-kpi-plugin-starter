namespace DevC.KPI.Plugins.SalesDemo.Widgets
{
    using System.Collections.Generic;
    using System.Linq;
    using dasz.LinqCube;
    using DevC.KPI.Reporting.Cubes;
    using DevC.KPI.Reporting.Plugins;

    /// <summary>Table output: one row per team with its revenue and order count.</summary>
    public sealed class RevenueByTeamTable : Widget
    {
        public override string Title => "Revenue by team";
        public override string Key => "RevenueByTeamTable";
        public override string DataSourceId => "sales";
        public override string ResultName => SalesCube.RevenueByTeam;
        public override DateAxisMode DateAxis => DateAxisMode.UntilToday;

        public override WidgetOutput Render(QueryResult result, WidgetContext context)
        {
            var rows = result.Dimension("Team").Leaves()
                .Select(leaf => (IReadOnlyList<object?>)
                    [leaf.DimensionEntry.Label, leaf.Measure("Revenue"), leaf.Measure("Orders")])
                .ToList();

            // Column decimals: team = none, revenue = 2, orders = 0.
            return WidgetOutput.Table(["Team", "Revenue", "Orders"], rows, [null, 2, 0]);
        }
    }
}
