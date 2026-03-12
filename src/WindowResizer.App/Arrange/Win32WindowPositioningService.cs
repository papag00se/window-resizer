using System.Runtime.InteropServices;
using WindowResizer.Core.Layout;

namespace WindowResizer.App.Arrange;

public sealed class Win32WindowPositioningService : IWindowPositioningService
{
    private const uint MonitorDefaultToNearest = 2;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    public MonitorWorkArea GetWorkAreaForWindow(nint handle)
    {
        var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
        if (monitor == nint.Zero)
        {
            throw new InvalidOperationException($"Could not resolve a monitor for window handle {handle}.");
        }

        var monitorInfo = new MonitorInfoEx
        {
            cbSize = Marshal.SizeOf<MonitorInfoEx>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            throw new InvalidOperationException($"Could not read monitor information for window handle {handle}.");
        }

        var workArea = monitorInfo.rcWork;
        return new MonitorWorkArea(
            workArea.Left,
            workArea.Top,
            workArea.Right - workArea.Left,
            workArea.Bottom - workArea.Top);
    }

    public void ApplyWindowRect(nint handle, WindowLayoutRect rectangle)
    {
        if (!SetWindowPos(
                handle,
                nint.Zero,
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                rectangle.Height,
                SwpNoZOrder | SwpNoActivate))
        {
            throw new InvalidOperationException($"Could not move window handle {handle}.");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MonitorInfoEx lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
