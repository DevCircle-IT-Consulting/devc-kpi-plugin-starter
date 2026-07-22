namespace DevC.KPI.Plugins.SalesDemo
{
    using System.Collections.Generic;
    using dasz.LinqCube;
    using DevC.KPI.Reporting.Measures;
    using DevC.KPI.Reporting.Plugins;

    /// <summary>
    /// The example cube: one LinqCube over <see cref="SalesRow"/> with a <c>Date</c> and a <c>Team</c>
    /// axis and the measures <c>Revenue</c>, <c>Units</c> and <c>Orders</c>. It exposes three named
    /// results - revenue per month, revenue per team, and a grand total - one per widget.
    /// </summary>
    public sealed class SalesCube : DataSourceBuilder<SalesRow>
    {
        public const string RevenueByMonth = "revenueByMonth";
        public const string RevenueByTeam = "revenueByTeam";
        public const string TotalRevenue = "totalRevenue";

        // Dense so every team coordinate exists for the table (small sample data). Real cubes keep
        // the sparse default and zero-fill from their known domain instead.
        protected override bool BuildSparse => false;

        public override string Key => "SalesCube";                              // == datasource YAML `builder:`

        public override IReadOnlyCollection<string> ResultNames { get; }
            = [RevenueByMonth, RevenueByTeam, TotalRevenue];

        // A `custom` datasource: the cube owns its rows in-process, so it ignores the context.
        // For a DB cube this would be: ctx.Sql<SalesRow>(sql, new { from = ctx.LoadWindow.From, ... }).
        protected override IEnumerable<SalesRow> LoadFacts(BuildContext context) => SalesSampleData.Rows;

        protected override IEnumerable<Query<SalesRow>> DefineQueries(BuildContext context)
        {
            var date = new Dimensions.DateDimension();
            var team = new Dimensions.TeamDimension();

            var revenue = Measure.Sum<SalesRow>("Revenue", r => r.Amount);
            var units = Measure.SumInt<SalesRow>("Units", r => r.Units);
            var orders = Measure.Count<SalesRow>("Orders");

            yield return new Query<SalesRow>(RevenueByMonth)
                .WithChainedDimension(date.For<SalesRow>(r => r.OrderDate))
                .WithMeasure(revenue).WithMeasure(units).WithMeasure(orders);

            yield return new Query<SalesRow>(RevenueByTeam)
                .WithChainedDimension(team.For<SalesRow>(r => r.Team))
                .WithMeasure(revenue).WithMeasure(orders);

            yield return new Query<SalesRow>(TotalRevenue)
                .WithChainedDimension(date.For<SalesRow>(r => r.OrderDate))
                .WithMeasure(revenue);
        }
    }
}
