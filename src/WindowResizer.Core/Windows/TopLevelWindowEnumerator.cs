using System.Diagnostics;
using System.Text;

namespace WindowResizer.Core.Windows;

public sealed class TopLevelWindowEnumerator
{
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
        var processName = TryGetProcessName(hWnd);
        if (processName is null)
        {
            return null;
        }

        return new TopLevelWindowInfo(
            Handle: hWnd,
            Title: GetWindowText(hWnd),
            ClassName: GetClassName(hWnd),
            ProcessName: processName,
            IsVisible: NativeMethods.IsWindowVisible(hWnd),
            IsMinimized: NativeMethods.IsIconic(hWnd),
            IsCloaked: IsCloaked(hWnd),
            HasOwner: NativeMethods.GetWindow(hWnd, NativeMethods.GwOwner) != IntPtr.Zero,
            IsToolWindow: (NativeMethods.GetWindowExStyle(hWnd) & NativeMethods.WsExToolWindow) != 0);
    }

    private static string? TryGetProcessName(IntPtr hWnd)
    {
        NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
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
}
