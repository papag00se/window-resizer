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
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(100)]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100)),
            arrangeService,
            resolver,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(75));

        controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0);
        controller.HandlePotentialArrangeWindow(100, objectId: 0, childId: 0);

        Assert.True(fired.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void HandlePotentialArrangeWindowIgnoresNonWindowEventsAndIneligibleProcesses()
    {
        using var fired = new ManualResetEventSlim(false);
        var resolver = new HeuristicWindowOrderResolver();
        var arrangeService = new ManualArrangeService(
            new FakeWindowSource([CreateWindow(100)]),
            new FakeWindowPositioningService(new MonitorWorkArea(0, 0, 1600, 900), fired),
            resolver);
        using var controller = new AutoArrangeController(
            new FakeWindowEnumerator(CreateWindow(100), CreateWindow(200, processName: "notepad")),
            arrangeService,
            resolver,
            () => 1000,
            debounceDelay: TimeSpan.FromMilliseconds(50));

        Assert.False(controller.HandlePotentialArrangeWindow(100, objectId: 1, childId: 0));
        Assert.False(controller.HandlePotentialArrangeWindow(200, objectId: 0, childId: 0));
        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(200)));
    }

    private static TopLevelWindowInfo CreateWindow(nint handle, string processName = "Code")
    {
        return new TopLevelWindowInfo(
            handle,
            "VS Code",
            "Chrome_WidgetWin_1",
            100 + (int)handle,
            processName,
            DateTimeOffset.Parse("2026-03-12T17:00:00Z").AddMinutes((int)handle),
            true,
            false,
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
    }
}
