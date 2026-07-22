namespace DevC.KPI.Plugins.SalesDemo.Dimensions
{
    using System;
    using dasz.LinqCube;
    using DevC.KPI.Reporting.Dimensions;

    /// <summary>
    /// The conformed <c>Date</c> dimension (year -> month). A report filter bound to <c>Date</c>
    /// (the datetree picker) then slices every cube stamped with this dimension.
    /// </summary>
    public sealed class DateDimension
    {
        public const string Name = "Date";

        public int FromYear { get; init; } = DateTime.Today.Year - 2;
        public int ToYear { get; init; } = DateTime.Today.Year;

        public Dimension<DateTime, TFact> For<TFact>(Func<TFact, DateTime> selector)
            => Dim.YearMonth(Name, selector, FromYear, ToYear);
    }
}
