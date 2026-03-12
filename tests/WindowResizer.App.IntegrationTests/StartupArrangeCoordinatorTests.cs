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
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(101), CreateWindow(202)]),
            positioningService,
            new HeuristicWindowOrderResolver(),
            visibilitySynchronizer);
        var coordinator = new StartupArrangeCoordinator(
            new FakeWindowSource([CreateWindow(101), CreateWindow(202)]),
            arrangeService);

        var result = coordinator.ArrangeExistingWindowsIfNeeded(900);

        Assert.NotNull(result);
        Assert.Equal(ManualArrangeStatus.Success, result!.Status);
        Assert.Equal([101, 202], visibilitySynchronizer.SynchronizedHandles.Select(handle => (int)handle));
        Assert.Equal(2, positioningService.AppliedRectangles.Count);
    }

    [Fact]
    public void ArrangeExistingWindowsIfNeededDoesNothingWhenFewerThanTwoWindowsExist()
    {
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var positioningService = new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 2000, 1000));
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(101)]),
            positioningService,
            new HeuristicWindowOrderResolver(),
            visibilitySynchronizer);
        var coordinator = new StartupArrangeCoordinator(
            new FakeWindowSource([CreateWindow(101)]),
            arrangeService);

        var result = coordinator.ArrangeExistingWindowsIfNeeded(900);

        Assert.Null(result);
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
        Assert.Empty(positioningService.AppliedRectangles);
    }

    private static TopLevelWindowInfo CreateWindow(nint handle)
    {
        return new TopLevelWindowInfo(
            handle,
            "VS Code",
            "Chrome_WidgetWin_1",
            100 + (int)handle,
            "Code",
            DateTimeOffset.Parse("2026-03-12T17:00:00Z").AddMinutes((int)handle),
            0,
            0,
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
