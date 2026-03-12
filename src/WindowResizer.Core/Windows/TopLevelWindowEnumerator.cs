using System.Diagnostics;
using System.Text;

namespace WindowResizer.Core.Windows;

public class TopLevelWindowEnumerator
{
    public virtual TopLevelWindowInfo? TryGetWindowInfo(nint handle)
    {
        return TryCreateWindowInfo(handle);
    }

    public IReadOnlyList<TopLevelWindowInfo> EnumerateAll()
    {
        var windows = new List<TopLevelWindowInfo>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            var window = TryCreateWindowInfo(hWnd);
            if (window is not null)
            {
                windows.Add(window);
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public IReadOnlyList<TopLevelWindowInfo> EnumerateEligibleVsCodeWindows()
    {
        return EnumerateAll()
            .Where(VsCodeWindowEligibility.IsEligible)
            .ToArray();
    }

    private static TopLevelWindowInfo? TryCreateWindowInfo(IntPtr hWnd)
    {
        var processInfo = TryGetProcessInfo(hWnd);
        if (processInfo is null)
        {
            return null;
        }

        return new TopLevelWindowInfo(
            Handle: hWnd,
            Title: GetWindowText(hWnd),
            ClassName: GetClassName(hWnd),
            ProcessId: processInfo.Value.ProcessId,
            ProcessName: processInfo.Value.ProcessName,
            ProcessStartTimeUtc: processInfo.Value.ProcessStartTimeUtc,
            CurrentLeft: GetWindowRect(hWnd).Left,
            CurrentTop: GetWindowRect(hWnd).Top,
            IsVisible: NativeMethods.IsWindowVisible(hWnd),
            IsMinimized: NativeMethods.IsIconic(hWnd),
            IsCloaked: IsCloaked(hWnd),
            HasOwner: NativeMethods.GetWindow(hWnd, NativeMethods.GwOwner) != IntPtr.Zero,
            IsToolWindow: (NativeMethods.GetWindowExStyle(hWnd) & NativeMethods.WsExToolWindow) != 0);
    }

    private static (int ProcessId, string ProcessName, DateTimeOffset? ProcessStartTimeUtc)? TryGetProcessInfo(IntPtr hWnd)
    {
        NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            DateTimeOffset? processStartTimeUtc = null;

            try
            {
                processStartTimeUtc = new DateTimeOffset(process.StartTime.ToUniversalTime());
            }
            catch (InvalidOperationException)
            {
                processStartTimeUtc = null;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                processStartTimeUtc = null;
            }

            return ((int)processId, process.ProcessName, processStartTimeUtc);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string GetWindowText(IntPtr hWnd)
    {
        var length = NativeMethods.GetWindowTextLengthW(hWnd);
        var builder = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowTextW(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClassName(IntPtr hWnd)
    {
        var builder = new StringBuilder(256);
        _ = NativeMethods.GetClassNameW(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static bool IsCloaked(IntPtr hWnd)
    {
        var result = NativeMethods.DwmGetWindowAttribute(
            hWnd,
            NativeMethods.DwmaCloaked,
            out var cloaked,
            sizeof(int));

        return result == 0 && cloaked != 0;
    }

    private static NativeMethods.Rect GetWindowRect(IntPtr hWnd)
    {
        if (!NativeMethods.GetWindowRect(hWnd, out var rect))
        {
            throw new InvalidOperationException($"Could not read bounds for window handle {hWnd}.");
        }

        return rect;
    }
}
