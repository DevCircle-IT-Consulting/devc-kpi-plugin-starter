namespace DevC.KPI.Plugins.SalesDemo.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using dasz.LinqCube;                // CubeResult
    using DevC.KPI.Reporting.Cubes;     // QueryResult navigation (Query/Dimension/Leaves)
    using DevC.KPI.Reporting.Filters;   // DateRange
    using DevC.KPI.Reporting.Plugins;   // BuildContext / EmptyBuildDataAccess
    using Xunit;

    /// <summary>
    /// Builds <see cref="SalesCube"/> end-to-end and asserts the aggregates. This is the fast,
    /// database-free unit-test loop for a cube. Because this cube is `custom` (it owns its rows),
    /// the build reads <see cref="SalesSampleData"/>; a DB cube would instead be fed rows via
    /// <c>new InMemoryBuildDataAccess().Add(rows)</c> on <c>BuildContext.Data</c>.
    /// </summary>
    public class SalesCubeTests
    {
        private static CubeResult Build()
        {
            var context = new BuildContext
            {
                DataSourceId = "sales",
                Params = new Dictionary<string, string>(),
                LoadWindow = new DateRange(DateTime.MinValue, DateTime.MaxValue),
                Data = EmptyBuildDataAccess.Instance,   // custom cube ignores it
            };
            return new SalesCube().Build(context);
        }

        [Fact]
        public void RevenueByMonth_HasMonths_AndPositiveTotal()
        {
            var months = Build().Query(SalesCube.RevenueByMonth).Dimension("Date").Leaves().ToList();

            Assert.NotEmpty(months);
            Assert.True(months.Sum(m => (double)m.Measure("Revenue")) > 0);
        }

        [Fact]
        public void RevenueByTeam_CoversAllFourTeams()
        {
            var teams = Build().Query(SalesCube.RevenueByTeam).Dimension("Team").Leaves()
                .Select(t => t.DimensionEntry.Label)
                .ToList();

            Assert.Equal(4, teams.Count);
            Assert.Contains("North", teams);
        }
    }
}
