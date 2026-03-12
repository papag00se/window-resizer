using WindowResizer.App.Arrange;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.IntegrationTests;

public class Win32WindowVisibilityOrderSynchronizerTests
{
    [Fact]
    public void SynchronizeOrderDoesNotTreatPreviouslyHiddenShowWindowResultAsAFailure()
    {
        var calls = new List<(nint Handle, int Command)>();
        var delays = new List<TimeSpan>();
        var synchronizer = new Win32WindowVisibilityOrderSynchronizer(
            delay: delays.Add,
            showWindow: (handle, command) => calls.Add((handle, command)));

        synchronizer.SynchronizeOrder([CreateWindow(101), CreateWindow(202)]);

        Assert.Equal(
            [
                (new nint(101), 0),
                (new nint(202), 0),
                (new nint(101), 4),
                (new nint(202), 4)
            ],
            calls);
        Assert.Equal(
            [
                TimeSpan.FromMilliseconds(120),
                TimeSpan.FromMilliseconds(120),
                TimeSpan.FromMilliseconds(300),
                TimeSpan.FromMilliseconds(160),
                TimeSpan.FromMilliseconds(160)
            ],
            delays);
    }

    [Fact]
    public void SynchronizeOrderRestoresAlreadyHiddenWindowsWhenAHideCallThrows()
    {
        var calls = new List<(nint Handle, int Command)>();
        var delays = new List<TimeSpan>();
        var synchronizer = new Win32WindowVisibilityOrderSynchronizer(
            delay: delays.Add,
            showWindow: (handle, command) =>
            {
                calls.Add((handle, command));
                if (handle == 202 && command == 0)
                {
                    throw new InvalidOperationException("Simulated hide failure.");
                }
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => synchronizer.SynchronizeOrder([CreateWindow(101), CreateWindow(202), CreateWindow(303)]));

        Assert.Equal("Simulated hide failure.", exception.Message);
        Assert.Equal(
            [
                (new nint(101), 0),
                (new nint(202), 0),
                (new nint(101), 4)
            ],
            calls);
        Assert.Equal(
            [
                TimeSpan.FromMilliseconds(120),
                TimeSpan.FromMilliseconds(160)
            ],
            delays);
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
}
