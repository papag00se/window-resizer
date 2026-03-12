using WindowResizer.App.Arrange;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.IntegrationTests;

public class HeuristicWindowOrderResolverTests
{
    [Fact]
    public void OrderWindowsPrefersObservedOpenSequence()
    {
        var resolver = new HeuristicWindowOrderResolver();
        var firstObserved = CreateWindow(200, processId: 22, startMinutes: 10);
        var secondObserved = CreateWindow(100, processId: 11, startMinutes: 0);

        resolver.ObserveWindow(firstObserved);
        resolver.ObserveWindow(secondObserved);

        var ordered = resolver.OrderWindows([secondObserved, firstObserved]);

        Assert.Equal([200, 100], ordered.Select(window => (int)window.Handle));
    }

    [Fact]
    public void OrderWindowsFallsBackToProcessStartTimeThenPidThenHandle()
    {
        var resolver = new HeuristicWindowOrderResolver();
        var earliestProcess = CreateWindow(300, processId: 30, startMinutes: 1);
        var laterLowerPid = CreateWindow(200, processId: 20, startMinutes: 5);
        var laterHigherPid = CreateWindow(100, processId: 40, startMinutes: 5);

        var ordered = resolver.OrderWindows([laterHigherPid, laterLowerPid, earliestProcess]);

        Assert.Equal([300, 200, 100], ordered.Select(window => (int)window.Handle));
    }

    private static TopLevelWindowInfo CreateWindow(nint handle, int processId, int startMinutes)
    {
        return new TopLevelWindowInfo(
            handle,
            $"VS Code {handle}",
            "Chrome_WidgetWin_1",
            processId,
            "Code",
            DateTimeOffset.Parse("2026-03-12T17:00:00Z").AddMinutes(startMinutes),
            0,
            0,
            true,
            false,
            false,
            false,
            false);
    }
}
