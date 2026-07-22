namespace DevC.KPI.Plugins.SalesDemo
{
    using DevC.KPI.Reporting.Plugins;

    /// <summary>
    /// The example plugin. Registers one cube (<see cref="SalesCube"/>), the conformed
    /// <c>Date</c> + <c>Team</c> dimensions, and three widgets that show the three common output
    /// kinds - a line chart, a KPI value tile, and a table. Everything binds to the <c>sales</c>
    /// datasource (see config/salesdemo/datasources/sales.yaml).
    /// </summary>
    public sealed class SalesDemoPlugin : IReportingPlugin
    {
        public string Name => "DevC.KPI.Plugins.SalesDemo";

        public string Id => "SalesDemo";

        // Auto-on for the "salesdemo" tenant. Point this at whatever tenant you provisioned.
        public PluginScope Scope => PluginScope.ForTenants("salesdemo");

        public void Register(PluginRegistration registration)
        {
            var date = new Dimensions.DateDimension();
            var team = new Dimensions.TeamDimension();

            registration
                .AddBuilder(new SalesCube())
                .AddWidget(new Widgets.RevenueByMonthChart())
                .AddWidget(new Widgets.RevenueKpi())
                .AddWidget(new Widgets.RevenueByTeamTable())
                // Register each dimension's filter domain so reports may bind filters to it.
                .AddDimension(ConformedDimensionInfo.Date(Dimensions.DateDimension.Name, date.FromYear, date.ToYear))
                .AddDimension(ConformedDimensionInfo.Enum(Dimensions.TeamDimension.Name, team.Teams));
        }
    }
}
