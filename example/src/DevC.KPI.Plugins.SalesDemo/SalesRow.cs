namespace DevC.KPI.Plugins.SalesDemo
{
    using System;

    /// <summary>One fact row: a single order. This is the shape <see cref="SalesCube"/> aggregates.
    /// For a database-backed cube it is what your <c>ctx.Sql&lt;SalesRow&gt;(...)</c> projection returns.</summary>
    public sealed class SalesRow
    {
        public DateTime OrderDate { get; init; }
        public string Team { get; init; } = "";
        public decimal Amount { get; init; }
        public int Units { get; init; }
    }
}
