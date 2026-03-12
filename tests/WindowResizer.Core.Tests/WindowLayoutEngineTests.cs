using WindowResizer.Core.Layout;

namespace WindowResizer.Core.Tests;

public class WindowLayoutEngineTests
{
    [Fact]
    public void CreateLayoutEvenlyDistributesLeftEdgesWithoutOverlapWhenSpaceAllows()
    {
        var workArea = new MonitorWorkArea(Left: 100, Top: 50, Width: 3000, Height: 1400);

        var plan = WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx: 900, windowCount: 3);

        Assert.Equal(900, plan.EffectiveWidthPx);
        Assert.Equal(
            [
                new WindowLayoutRect(100, 50, 900, 1400),
                new WindowLayoutRect(1150, 50, 900, 1400),
                new WindowLayoutRect(2200, 50, 900, 1400)
            ],
            plan.Rectangles);
    }

    [Fact]
    public void CreateLayoutPreservesWidthAndAllowsOverlapWhenNeeded()
    {
        var workArea = new MonitorWorkArea(Left: 0, Top: 0, Width: 1920, Height: 1080);

        var plan = WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx: 1200, windowCount: 3);

        Assert.Equal(1200, plan.EffectiveWidthPx);
        Assert.Equal(
            [
                new WindowLayoutRect(0, 0, 1200, 1080),
                new WindowLayoutRect(360, 0, 1200, 1080),
                new WindowLayoutRect(720, 0, 1200, 1080)
            ],
            plan.Rectangles);
    }

    [Fact]
    public void CreateLayoutClampsWidthToWorkArea()
    {
        var workArea = new MonitorWorkArea(Left: 10, Top: 20, Width: 800, Height: 600);

        var plan = WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx: 1200, windowCount: 2);

        Assert.Equal(800, plan.EffectiveWidthPx);
        Assert.Equal(
            [
                new WindowLayoutRect(10, 20, 800, 600),
                new WindowLayoutRect(10, 20, 800, 600)
            ],
            plan.Rectangles);
    }

    [Fact]
    public void CreateLayoutReturnsNoRectanglesWhenThereAreNoWindows()
    {
        var workArea = new MonitorWorkArea(Left: 0, Top: 0, Width: 1600, Height: 900);

        var plan = WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx: 1000, windowCount: 0);

        Assert.Empty(plan.Rectangles);
        Assert.Equal(1000, plan.EffectiveWidthPx);
    }

    [Fact]
    public void CreateLayoutRejectsNonPositiveRequestedWidth()
    {
        var workArea = new MonitorWorkArea(Left: 0, Top: 0, Width: 1600, Height: 900);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx: 0, windowCount: 1));

        Assert.Equal("requestedWidthPx", exception.ParamName);
    }
}
