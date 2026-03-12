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
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            windowSource,
            positioningService,
            new HeuristicWindowOrderResolver(),
            visibilitySynchronizer);

        var result = arrangeService.ArrangeNow(requestedWidthPx: 900);

        Assert.Equal(ManualArrangeStatus.Success, result.Status);
        Assert.Equal(2, result.ArrangedWindowCount);
        Assert.Equal([101, 202], visibilitySynchronizer.SynchronizedHandles.Select(handle => (int)handle));
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
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([]),
            positioningService,
            new HeuristicWindowOrderResolver(),
            visibilitySynchronizer);

        var result = arrangeService.ArrangeNow(requestedWidthPx: 1000);

        Assert.Equal(ManualArrangeStatus.NoEligibleWindows, result.Status);
        Assert.Equal(0, result.ArrangedWindowCount);
        Assert.Empty(positioningService.AppliedRectangles);
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void ArrangeNowUsesHeuristicOrderInsteadOfEnumerationOrder()
    {
        var firstObserved = CreateWindow(202);
        var secondObserved = CreateWindow(101);
        var resolver = new HeuristicWindowOrderResolver();
        resolver.ObserveWindow(firstObserved);
        resolver.ObserveWindow(secondObserved);
        var positioningService = new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 2000, 1000));
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();

        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([secondObserved, firstObserved]),
            positioningService,
            resolver,
            visibilitySynchronizer);

        var result = arrangeService.ArrangeNow(requestedWidthPx: 900);

        Assert.Equal(ManualArrangeStatus.Success, result.Status);
        Assert.Equal([202, 101], visibilitySynchronizer.SynchronizedHandles.Select(handle => (int)handle));
        Assert.Equal([202, 101], positioningService.AppliedRectangles.Select(entry => (int)entry.Handle));
    }

    [Fact]
    public void ArrangeNowCanPreferCurrentLeftToRightOrderForManualReordering()
    {
        var leftmost = CreateWindow(202, currentLeft: 0, currentTop: 10);
        var middle = CreateWindow(303, currentLeft: 500, currentTop: 10);
        var rightmost = CreateWindow(101, currentLeft: 1000, currentTop: 10);
        var resolver = new HeuristicWindowOrderResolver();
        resolver.ObserveWindow(rightmost);
        resolver.ObserveWindow(leftmost);
        resolver.ObserveWindow(middle);
        var positioningService = new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 2200, 1000));
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([rightmost, middle, leftmost]),
            positioningService,
            resolver,
            visibilitySynchronizer);

        var result = arrangeService.ArrangeNow(
            requestedWidthPx: 900,
            synchronizeTaskbarOrder: true,
            preferCurrentScreenOrder: true);

        Assert.Equal(ManualArrangeStatus.Success, result.Status);
        Assert.Equal([202, 303, 101], visibilitySynchronizer.SynchronizedHandles.Select(handle => (int)handle));
        Assert.Equal([202, 303, 101], positioningService.AppliedRectangles.Select(entry => (int)entry.Handle));
    }

    [Fact]
    public void ArrangeNowCanSkipTaskbarSynchronizationForAutomaticRuns()
    {
        var positioningService = new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 2000, 1000));
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(101), CreateWindow(202)]),
            positioningService,
            new HeuristicWindowOrderResolver(),
            visibilitySynchronizer);

        var result = arrangeService.ArrangeNow(requestedWidthPx: 900, synchronizeTaskbarOrder: false);

        Assert.Equal(ManualArrangeStatus.Success, result.Status);
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
        Assert.Equal(2, positioningService.AppliedRectangles.Count);
    }

    private static TopLevelWindowInfo CreateWindow(nint handle, int currentLeft = 0, int currentTop = 0)
    {
        return new TopLevelWindowInfo(
            handle,
            "VS Code",
            "Chrome_WidgetWin_1",
            100 + (int)handle,
            "Code",
            DateTimeOffset.Parse("2026-03-12T17:00:00Z").AddMinutes((int)handle),
            currentLeft,
            currentTop,
            true,
            false,
            false,
            false,
            false);
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

    private sealed class FakeWindowVisibilityOrderSynchronizer : IWindowVisibilityOrderSynchronizer
    {
        public List<nint> SynchronizedHandles { get; } = [];

        public void SynchronizeOrder(IReadOnlyList<TopLevelWindowInfo> windows)
        {
            SynchronizedHandles.AddRange(windows.Select(window => window.Handle));
        }
    }
}
