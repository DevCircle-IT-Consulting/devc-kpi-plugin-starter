namespace DevC.KPI.Plugins.SalesDemo
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Deterministic in-process sample data - the reason this example needs no database and runs
    /// anywhere. It yields orders for the last ~two years across four teams. A real plugin replaces
    /// this with a DB projection in <see cref="SalesCube.LoadFacts"/> (see the docs).
    /// </summary>
    public static class SalesSampleData
    {
        public static readonly string[] Teams = ["West", "East", "North", "South"];

        /// <summary>Lazily enumerated (never materialize a large fact stream to a list).</summary>
        public static IEnumerable<SalesRow> Rows
        {
            get
            {
                var start = new DateTime(DateTime.Today.Year - 2, 1, 1);
                for (var month = start; month <= DateTime.Today; month = month.AddMonths(1))
                {
                    for (var t = 0; t < Teams.Length; t++)
                    {
                        var orders = 3 + ((month.Month + t) % 4);          // 3..6 orders / team / month
                        for (var o = 0; o < orders; o++)
                        {
                            yield return new SalesRow
                            {
                                OrderDate = month.AddDays(o * 2),
                                Team = Teams[t],
                                Amount = 500m + (t * 150m) + (month.Month * 40m) + (o * 60m),
                                Units = 1 + ((o + t) % 5),
                            };
                        }
                    }
                }
            }
        }
    }
}
