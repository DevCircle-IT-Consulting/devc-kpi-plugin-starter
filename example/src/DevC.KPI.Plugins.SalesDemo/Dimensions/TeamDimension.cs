namespace DevC.KPI.Plugins.SalesDemo.Dimensions
{
    using System;
    using dasz.LinqCube;
    using DevC.KPI.Reporting.Dimensions;

    /// <summary>The conformed <c>Team</c> dimension - a flat enum of teams (a filterable axis).</summary>
    public sealed class TeamDimension
    {
        public const string Name = "Team";

        public string[] Teams { get; init; } = SalesSampleData.Teams;

        public Dimension<string, TFact> For<TFact>(Func<TFact, string> selector)
            => Dim.Enum(Name, selector, Teams);
    }
}
