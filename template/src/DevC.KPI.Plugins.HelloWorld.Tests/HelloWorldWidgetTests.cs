namespace DevC.KPI.Plugins.HelloWorld.Tests
{
    using DevC.KPI.Plugins.HelloWorld.Widgets;
    using DevC.KPI.Reporting.Plugins;
    using Xunit;

    /// <summary>
    /// A smoke test for the static hello-world widget: it renders without any datasource.
    /// This is the whole test loop for a static widget. For a DATA-bound cube/widget, the pattern
    /// is to feed sample rows through <c>InMemoryBuildDataAccess</c> and assert the measures - see
    /// <c>Reference/CubeTestExample.cs.txt</c>.
    /// </summary>
    public class HelloWorldWidgetTests
    {
        [Fact]
        public void Renders_WithoutData()
        {
            var widget = new HelloWorldWidget();

            // The key a report page binds to.
            Assert.Equal("HelloWorld", widget.Key);

            // A static widget ignores the (empty) query result; WidgetContext.None = no filters.
            var output = widget.Render(WidgetContext.None);

            Assert.NotNull(output);
        }
    }
}
