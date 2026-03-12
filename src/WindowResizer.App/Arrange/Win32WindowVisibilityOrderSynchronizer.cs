using System.Runtime.InteropServices;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public sealed class Win32WindowVisibilityOrderSynchronizer : IWindowVisibilityOrderSynchronizer
{
    private const int SwHide = 0;
    private const int SwShowNoActivate = 4;

    private readonly Action<TimeSpan> _delay;
    private readonly Action<nint, int> _showWindow;

    public Win32WindowVisibilityOrderSynchronizer(
        Action<TimeSpan>? delay = null,
        Action<nint, int>? showWindow = null)
    {
        _delay = delay ?? Thread.Sleep;
        _showWindow = showWindow ?? InvokeShowWindow;
    }

    public void SynchronizeOrder(IReadOnlyList<TopLevelWindowInfo> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        if (windows.Count <= 1)
        {
            return;
        }

        var hiddenHandles = new List<nint>(windows.Count);

        try
        {
            foreach (var window in windows)
            {
                _showWindow(window.Handle, SwHide);
                hiddenHandles.Add(window.Handle);
                _delay(TimeSpan.FromMilliseconds(120));
            }

            _delay(TimeSpan.FromMilliseconds(300));
        }
        finally
        {
            foreach (var handle in hiddenHandles)
            {
                _showWindow(handle, SwShowNoActivate);
                _delay(TimeSpan.FromMilliseconds(160));
            }
        }
    }

    private static void InvokeShowWindow(nint handle, int command)
    {
        _ = ShowWindow(handle, command);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);
}
