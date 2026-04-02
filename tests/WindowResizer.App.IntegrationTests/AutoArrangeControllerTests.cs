using WindowResizer.App.Arrange;
using WindowResizer.Core.Layout;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.IntegrationTests;

public class AutoArrangeControllerTests
{
    [Fact]
    public void HandlePotentialArrangeWindowObservesANewWindowWithoutTriggeringAutomaticLayout()
    {
        using var fired = new ManualResetEventSlim(false);
        var existingLeft = CreateWindow(100, currentLeft: 0);
        var existingRight = CreateWindow(200, currentLeft: 800);
        var newWindow = CreateWindow(300, currentLeft: 1600);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var positioningService = new RecordingWindowPositioningService(new MonitorWorkArea(0, 0, 2800, 900), fired);
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([newWindow, existingRight, existingLeft]),
            positioningService,
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);

        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(newWindow),
            resolver,
            arrangeOperationTracker,
            startupWindowsProvider: () => [existingRight, existingLeft],
            orderingUniverseProvider: () => [existingRight, existingLeft, newWindow]);

        controller.Start();

        Assert.True(controller.HandlePotentialArrangeWindow(300, objectId: 0, childId: 0));
        Assert.False(controller.HandlePotentialArrangeWindow(300, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
        Assert.Empty(positioningService.AppliedRectangles);

        var result = arrangeService.ArrangeNow(
            requestedWidthPx: 900,
            synchronizeTaskbarOrder: false,
            preferCurrentScreenOrder: false,
            normalizeZOrder: false);

        Assert.True(fired.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(ManualArrangeStatus.Success, result.Status);
        Assert.Equal([100, 200, 300], positioningService.AppliedHandles.Select(handle => (int)handle));
        Assert.Equal(
            [
                (100, new WindowLayoutRect(0, 0, 900, 900)),
                (200, new WindowLayoutRect(950, 0, 900, 900)),
                (300, new WindowLayoutRect(1900, 0, 900, 900))
            ],
            positioningService.AppliedRectangles);
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresNonWindowEventsAndIneligibleProcesses()
    {
        using var fired = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100), CreateWindow(200, processName: "notepad")),
            resolver,
            arrangeOperationTracker,
            startupWindowsProvider: () => [CreateWindow(100)],
            orderingUniverseProvider: () => [CreateWindow(100)]);

        controller.Start();

        Assert.False(controller.HandlePotentialArrangeWindow(100, objectId: 1, childId: 0));
        Assert.False(controller.HandlePotentialArrangeWindow(200, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresEventsRaisedDuringAnActiveArrangeOperationAndCooldown()
    {
        using var fired = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var currentTime = DateTimeOffset.Parse("2026-03-12T18:00:00Z");
        var arrangeOperationTracker = new ArrangeOperationTracker(
            utcNow: () => currentTime,
            cooldownDuration: TimeSpan.FromSeconds(2));
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100), CreateWindow(200)),
            resolver,
            arrangeOperationTracker,
            startupWindowsProvider: () => [CreateWindow(100)],
            orderingUniverseProvider: () => [CreateWindow(100), CreateWindow(200)]);

        controller.Start();

        using (arrangeOperationTracker.Enter())
        {
            Assert.False(controller.HandlePotentialArrangeWindow(200, objectId: 0, childId: 0));
        }

        Assert.False(controller.HandlePotentialArrangeWindow(200, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));

        currentTime = currentTime.AddSeconds(3);

        Assert.True(controller.HandlePotentialArrangeWindow(200, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
    }

    private static TopLevelWindowInfo CreateWindow(
        nint handle,
        string processName = "Code",
        int currentLeft = 0,
        int currentTop = 0,
        bool isMinimized = false)
    {
        return new TopLevelWindowInfo(
            handle,
            "VS Code",
            "Chrome_WidgetWin_1",
            100 + (int)handle,
            processName,
            DateTimeOffset.Parse("2026-03-12T17:00:00Z").AddMinutes((int)handle),
            currentLeft,
            currentTop,
            true,
            isMinimized,
            false,
            false,
            false);
    }

    private sealed class FakeWindowEnumerator(params TopLevelWindowInfo[] windows) : TopLevelWindowEnumerator
    {
        private readonly Dictionary<nint, TopLevelWindowInfo> _windows = windows.ToDictionary(window => window.Handle);

        public override TopLevelWindowInfo? TryGetWindowInfo(nint handle)
        {
            return _windows.TryGetValue(handle, out var window) ? window : null;
        }
    }

    private sealed class FakeWindowSource(IReadOnlyList<TopLevelWindowInfo> windows) : IEligibleWindowSource
    {
        public IReadOnlyList<TopLevelWindowInfo> EnumerateEligibleWindows() => windows;
    }

    private sealed class FakeWindowPositioningService(MonitorWorkArea workArea, ManualResetEventSlim fired) : IWindowPositioningService
    {
        public MonitorWorkArea GetWorkAreaForWindow(nint handle) => workArea;

        public void ApplyWindowRect(nint handle, WindowLayoutRect rectangle)
        {
            fired.Set();
        }

        public void ApplyWindowZOrder(IReadOnlyList<nint> handlesTopToBottom)
        {
        }
    }

    private sealed class RecordingWindowPositioningService(MonitorWorkArea workArea, ManualResetEventSlim fired) : IWindowPositioningService
    {
        public List<nint> AppliedHandles { get; } = [];
        public List<(nint Handle, WindowLayoutRect Rectangle)> AppliedRectangles { get; } = [];

        public MonitorWorkArea GetWorkAreaForWindow(nint handle) => workArea;

        public void ApplyWindowRect(nint handle, WindowLayoutRect rectangle)
        {
            AppliedHandles.Add(handle);
            AppliedRectangles.Add((handle, rectangle));
            fired.Set();
        }

        public void ApplyWindowZOrder(IReadOnlyList<nint> handlesTopToBottom)
        {
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
