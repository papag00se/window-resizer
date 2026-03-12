using WindowResizer.Core.Layout;

namespace WindowResizer.App.IntegrationTests;

public class WindowLayoutEngineIntegrationTests
{
    [Fact]
    public void CreateLayoutUsesMonitorTopForEveryWindowAndSpansFullWorkingHeight()
    {
        var workArea = new MonitorWorkArea(Left: 25, Top: 40, Width: 2560, Height: 1372);

        var plan = WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx: 1100, windowCount: 2);

        Assert.All(plan.Rectangles, rectangle =>
        {
            Assert.Equal(40, rectangle.Y);
            Assert.Equal(1372, rectangle.Height);
        });
    }
}
