// A DevC.KPI reporting plugin: the single entry point the engine discovers.
//
// The engine scans loaded assemblies for IReportingPlugin implementations, calls
// Register(...) once at startup, and uses the resulting registry to (a) validate every
// tenant's YAML ("can this report bind widget X / dimension Y?") and (b) build + render
// widgets on request. One plugin can register many widgets, cubes and dimensions; this
// starter registers exactly one widget.
namespace DevC.KPI.Plugins.HelloWorld
{
    using DevC.KPI.Reporting.Plugins;

    /// <summary>
    /// The Hello-World starter plugin. Registers a single static text widget and nothing else.
    /// Grow it by uncommenting the data section below and following <c>Reference/DataExample.cs.txt</c>.
    /// </summary>
    public sealed class HelloWorldPlugin : IReportingPlugin
    {
        /// <summary>Human-readable name, used only in diagnostics / startup logs.</summary>
        public string Name => "DevC.KPI.Plugins.HelloWorld";

        /// <summary>
        /// Short, stable id. For a <see cref="PluginScope.Shared"/> plugin this is what a tenant lists
        /// under <c>enabled:</c> in its <c>plugins.yaml</c>. Keep it stable across releases.
        /// </summary>
        public string Id => "HelloWorld";

        /// <summary>
        /// Who gets this plugin. Three choices:
        /// <list type="bullet">
        /// <item><c>ForTenants("helloworld")</c> - auto-on for the named tenant(s). Best for a
        ///   customer-specific plugin (no opt-in step). This is what this starter uses.</item>
        /// <item><c>Shared</c> - a cross-customer feature; each tenant opts in via <c>plugins.yaml</c>.</item>
        /// <item><c>Global</c> - always on, content-free (rare; shared helpers).</item>
        /// </list>
        /// </summary>
        public PluginScope Scope => PluginScope.ForTenants("helloworld");

        /// <summary>Registers everything this plugin contributes. Called once at engine startup.</summary>
        public void Register(PluginRegistration registration)
        {
            registration
                .AddWidget(new Widgets.HelloWorldWidget());

            // ---- Add data here -------------------------------------------------------------------
            // A widget that shows data needs a cube (a DataSourceBuilder) plus the dimensions it
            // slices by. The pattern is in Reference/DataExample.cs.txt - copy it into real .cs files,
            // then wire it up like this:
            //
            //   var date = new Dimensions.DateDimension();
            //   registration
            //       .AddBuilder(new Cubes.SalesCube())
            //       .AddWidget(new Widgets.RevenueByMonthChart())
            //       .AddDimension(ConformedDimensionInfo.Date(Dimensions.DateDimension.Name,
            //                                                  date.FromYear, date.ToYear));
            //
            // ...and add a matching datasource YAML (see config/helloworld/datasources/) plus a
            // widget placement in a report page (see config/helloworld/reports/hello.yaml).
        }
    }
}
