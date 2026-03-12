using WindowResizer.App.Arrange;
using WindowResizer.Core.Layout;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.IntegrationTests;

public class AutoArrangeControllerTests
{
    [Fact]
    public void HandlePotentialArrangeWindowDebouncesRepeatedEligibleEvents()
    {
        using var fired = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(100)]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100)),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(75));

        controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0);
        controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0);

        Assert.True(fired.Wait(TimeSpan.FromSeconds(2)));
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void StartSeedsExistingWindowsSoANewlyOpenedWindowIsPlacedToTheRight()
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
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 900,
            debounceDelay: TimeSpan.FromMilliseconds(50),
            startupWindowsProvider: () => [existingRight, existingLeft],
            orderingUniverseProvider: () => [existingRight, existingLeft, newWindow]);

        controller.Start();

        Assert.True(controller.HandlePotentialArrangeWindow(300, objectId: 0, childId: 0));
        Assert.True(fired.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal([100, 200, 300], positioningService.AppliedHandles.Select(handle => (int)handle));
        Assert.Equal(1900, positioningService.AppliedRectangles.Single(rect => rect.Handle == 300).Rectangle.X);
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresAlreadyObservedWindowShowEvents()
    {
        using var fired = new ManualResetEventSlim(false);
        var existingWindow = CreateWindow(100);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([existingWindow]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(existingWindow),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50),
            startupWindowsProvider: () => [existingWindow],
            orderingUniverseProvider: () => [existingWindow]);

        controller.Start();

        Assert.False(controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresNonWindowEventsAndIneligibleProcesses()
    {
        using var fired = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(100)]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100), CreateWindow(200, processName: "notepad")),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50));

        Assert.False(controller.HandlePotentialArrangeWindow(100, objectId: 1, childId: 0));
        Assert.False(controller.HandlePotentialArrangeWindow(200, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void ClickingTaskbarActivatedWindowDoesNotScheduleAnArrangeWhenNoShowEventOccurs()
    {
        using var fired = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(100)]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100)),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50));

        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresEventsRaisedDuringAnActiveArrangeOperation()
    {
        using var fired = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var currentTime = DateTimeOffset.Parse("2026-03-12T18:00:00Z");
        var arrangeOperationTracker = new ArrangeOperationTracker(
            utcNow: () => currentTime,
            cooldownDuration: TimeSpan.FromSeconds(2));
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(100)]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100)),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50));

        using var arrangeScope = arrangeOperationTracker.Enter();

        Assert.False(controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresEventsRaisedImmediatelyAfterArrangeCompletion()
    {
        using var fired = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var currentTime = DateTimeOffset.Parse("2026-03-12T18:00:00Z");
        var arrangeOperationTracker = new ArrangeOperationTracker(
            utcNow: () => currentTime,
            cooldownDuration: TimeSpan.FromSeconds(2));
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(100)]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100)),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50));

        using (arrangeOperationTracker.Enter())
        {
        }

        Assert.False(controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));

        currentTime = currentTime.AddSeconds(3);

        Assert.True(controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0));
        Assert.True(fired.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void HandlePotentialArrangeWindowArrangesANewlyOpenedWindowEvenWhenOlderWindowsAreMinimized()
    {
        using var fired = new ManualResetEventSlim(false);
        var existingWindow = CreateWindow(100);
        var newWindow = CreateWindow(200, currentLeft: 1200);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var positioningService = new RecordingWindowPositioningService(new MonitorWorkArea(0, 0, 2200, 900), fired);
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([newWindow]),
            positioningService,
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(newWindow),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50),
            startupWindowsProvider: () => [CreateWindow(100, isMinimized: true)],
            orderingUniverseProvider: () => [CreateWindow(100, isMinimized: true), newWindow]);

        controller.Start();

        Assert.True(controller.HandlePotentialArrangeWindow(200, objectId: 0, childId: 0));
        Assert.True(fired.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(1200, positioningService.AppliedRectangles.Single().Rectangle.X);
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresRestoreOfWindowThatWasMinimizedDuringStartupSeeding()
    {
        using var fired = new ManualResetEventSlim(false);
        var restoredWindow = CreateWindow(100, currentLeft: 400);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([restoredWindow]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(restoredWindow),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50),
            startupWindowsProvider: () => [CreateWindow(100, currentLeft: 400, isMinimized: true)],
            orderingUniverseProvider: () => [CreateWindow(100, currentLeft: 400, isMinimized: true), restoredWindow]);

        controller.Start();

        Assert.False(controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresRestoreNoiseWhenTrackedWindowSetDidNotGrow()
    {
        using var fired = new ManualResetEventSlim(false);
        var startupWindow = CreateWindow(100, isMinimized: true);
        var restoreNoiseWindow = CreateWindow(200, currentLeft: 600);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([restoreNoiseWindow]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(restoreNoiseWindow),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50),
            startupWindowsProvider: () => [startupWindow],
            orderingUniverseProvider: () => [startupWindow]);

        controller.Start();

        Assert.False(controller.HandlePotentialArrangeWindow(200, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
    }

    [Fact]
    public void HandlePotentialArrangeWindowReportsArrangeFailuresWithoutEscapingTheTimerCallback()
    {
        using var failureReported = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeOperationTracker = new ArrangeOperationTracker();
        var visibilitySynchronizer = new FakeWindowVisibilityOrderSynchronizer();
        Exception? reportedException = null;
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(100)]),
            new ThrowingWindowPositioningService(),
            resolver,
            visibilitySynchronizer,
            arrangeOperationTracker);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100)),
            arrangeService,
            resolver,
            arrangeOperationTracker,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50),
            arrangeFailed: ex =>
            {
                reportedException = ex;
                failureReported.Set();
            });

        Assert.True(controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0));
        Assert.True(failureReported.Wait(TimeSpan.FromSeconds(2)));
        Assert.IsType<InvalidOperationException>(reportedException);
        Assert.Equal("boom", reportedException!.Message);
        Assert.Empty(visibilitySynchronizer.SynchronizedHandles);
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

    private sealed class ThrowingWindowPositioningService : IWindowPositioningService
    {
        public MonitorWorkArea GetWorkAreaForWindow(nint handle) => new(0, 0, 1600, 900);

        public void ApplyWindowRect(nint handle, WindowLayoutRect rectangle)
        {
            throw new InvalidOperationException("boom");
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
