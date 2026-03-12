using WindowResizer.App.Arrange;
using WindowResizer.Core.Layout;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.IntegrationTests;

public class ManualArrangeServiceTests
{
    [Fact]
    public void ArrangeNowUsesDiscoveredWindowsAndAppliesTheComputedLayout()
    {
        var windowSource = new FakeWindowSource(
            [
                CreateWindow(101),
                CreateWindow(202)
            ]);
        var positioningService = new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 2000, 1000));
        var arrangeService = new ManualArrangeService(windowSource, positioningService);

        var result = arrangeService.ArrangeNow(requestedWidthPx: 900);

        Assert.Equal(ManualArrangeStatus.Success, result.Status);
        Assert.Equal(2, result.ArrangedWindowCount);
        Assert.Equal(
            [
                (101, new WindowLayoutRect(0, 0, 900, 1000)),
                (202, new WindowLayoutRect(1100, 0, 900, 1000))
            ],
            positioningService.AppliedRectangles);
    }

    [Fact]
    public void ArrangeNowReturnsNoEligibleWindowsWhenTheSourceIsEmpty()
    {
        var positioningService = new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900));
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([]),
            positioningService);

        var result = arrangeService.ArrangeNow(requestedWidthPx: 1000);

        Assert.Equal(ManualArrangeStatus.NoEligibleWindows, result.Status);
        Assert.Equal(0, result.ArrangedWindowCount);
        Assert.Empty(positioningService.AppliedRectangles);
    }

    private static TopLevelWindowInfo CreateWindow(nint handle)
    {
        return new TopLevelWindowInfo(handle, "VS Code", "Chrome_WidgetWin_1", "Code", true, false, false, false, false);
    }

    private sealed class FakeWindowSource(IReadOnlyList<TopLevelWindowInfo> windows) : IEligibleWindowSource
    {
        public IReadOnlyList<TopLevelWindowInfo> EnumerateEligibleWindows() => windows;
    }

    private sealed class FakeWindowPositioningService(MonitorWorkArea workArea) : IWindowPositioningService
    {
        public List<(nint Handle, WindowLayoutRect Rectangle)> AppliedRectangles { get; } = [];

        public MonitorWorkArea GetWorkAreaForWindow(nint handle) => workArea;

        public void ApplyWindowRect(nint handle, WindowLayoutRect rectangle)
        {
            AppliedRectangles.Add((handle, rectangle));
        }
    }
}
