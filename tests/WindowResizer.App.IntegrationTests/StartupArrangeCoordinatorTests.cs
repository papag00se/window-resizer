using WindowResizer.App.Arrange;
using WindowResizer.Core.Layout;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.IntegrationTests;

public class StartupArrangeCoordinatorTests
{
    [Fact]
    public void ArrangeExistingWindowsIfNeededRunsManualArrangeWhenMultipleWindowsAlreadyExist()
    {
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var positioningService = new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 2000, 1000));
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var firstWindow = CreateWindow(101, currentLeft: 900);
        var secondWindow = CreateWindow(202, currentLeft: 100);
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([firstWindow, secondWindow]),
            positioningService,
            new HeuristicWindowOrderResolver(),
            visibilitySynchronizer,
            arrangeOperationTracker);
        var coordinator = new StartupArrangeCoordinator(
            new FakeWindowSource([firstWindow, secondWindow]),
            arrangeService);

        var result = coordinator.ArrangeExistingWindowsIfNeeded(900);

        Assert.NotNull(result);
        Assert.Equal(ManualArrangeStatus.Success, result!.Status);
        Assert.Equal([202, 101], visibilitySynchronizer.SynchronizedHandles.Select(handle => (int)handle));
        Assert.Equal([202, 101], positioningService.AppliedRectangles.Select(entry => (int)entry.Handle));
        Assert.Equal([202, 101], positioningService.ZOrderTopToBottom.Select(handle => (int)handle));
    }

    [Fact]
    public void ArrangeExistingWindowsIfNeededDoesNothingWhenFewerThanTwoWindowsExist()
    {
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var positioningService = new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 2000, 1000));
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(101)]),
            positioningService,
            new HeuristicWindowOrderResolver(),
            visibilitySynchronizer,
            arrangeOperationTracker);
        var coordinator = new StartupArrangeCoordinator(
            new FakeWindowSource([CreateWindow(101)]),
            arrangeService);

        var result = coordinator.ArrangeExistingWindowsIfNeeded(900);

        Assert.Null(result);
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
        Assert.Empty(positioningService.AppliedRectangles);
        Assert.Empty(positioningService.ZOrderTopToBottom);
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
        public List<nint> ZOrderTopToBottom { get; } = [];

        public MonitorWorkArea GetWorkAreaForWindow(nint handle) => workArea;

        public void ApplyWindowRect(nint handle, WindowLayoutRect rectangle)
        {
            AppliedRectangles.Add((handle, rectangle));
        }

        public void ApplyWindowZOrder(IReadOnlyList<nint> handlesTopToBottom)
        {
            ZOrderTopToBottom.AddRange(handlesTopToBottom);
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
