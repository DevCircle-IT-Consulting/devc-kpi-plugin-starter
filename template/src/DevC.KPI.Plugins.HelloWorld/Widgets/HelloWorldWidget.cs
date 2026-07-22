namespace DevC.KPI.Plugins.HelloWorld.Widgets
{
    using DevC.KPI.Reporting.Plugins;

    /// <summary>
    /// The one widget this starter ships: a static "Hello, world!" text card.
    /// <para>
    /// It derives from <see cref="StaticWidget"/>, the base for widgets that read NO datasource.
    /// The engine renders it without building any cube, so it works even with no database wired up -
    /// which is exactly why it is the ideal first widget. Its <see cref="StaticWidget.Key"/> ("HelloWorld")
    /// is what a report page references via <c>widget: HelloWorld</c>.
    /// </para>
    /// <para>
    /// A widget always returns exactly one <see cref="WidgetOutput"/>. The four kinds are:
    /// <c>Text</c> (this one), <c>SingleValue</c> (a KPI tile), <c>Table</c>, and <c>Chart</c>
    /// (any ECharts type). A data-bound widget instead derives from <c>Widget</c>, declares which
    /// datasource + query result it reads, and builds its output from the <c>QueryResult</c> -
    /// see Reference/DataExample.cs.txt and ../docs/04-widgets-and-charts.md.
    /// </para>
    /// </summary>
    public sealed class HelloWorldWidget : StaticWidget
    {
        /// <summary>The placement key referenced from report YAML (<c>widget: HelloWorld</c>).</summary>
        public override string Key => "HelloWorld";

        /// <summary>Produces the card content. The (empty) query result is ignored for a static widget.</summary>
        public override WidgetOutput Render(WidgetContext context)
            => WidgetOutput.Text(
                "Hello, world!\n\n" +
                "This card is a DevC.KPI plugin widget rendered by the reporting engine. " +
                "It reads no datasource - it is the simplest possible widget.\n\n" +
                "Next steps:\n" +
                "  1. Rename this plugin for your tenant (see the repo README / dotnet new template).\n" +
                "  2. Add a real datasource + cube (Reference/DataExample.cs.txt) to show live numbers.\n" +
                "  3. Place more widgets on the report page (config/helloworld/reports/hello.yaml).");
    }
}
