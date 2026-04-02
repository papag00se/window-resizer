using System.Runtime.InteropServices;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public sealed class AutoArrangeController : IDisposable
{
    private const uint EventObjectShow = 0x8002;
    private const int ObjIdWindow = 0;
    private const uint WineventOutOfContext = 0x0000;
    private const uint WineventSkipOwnProcess = 0x0002;

    private readonly TopLevelWindowEnumerator _windowEnumerator;
    private readonly HeuristicWindowOrderResolver _windowOrderResolver;
    private readonly ArrangeOperationTracker _arrangeOperationTracker;
    private readonly Func<IReadOnlyList<TopLevelWindowInfo>> _startupWindowsProvider;
    private readonly Func<IReadOnlyList<TopLevelWindowInfo>> _orderingUniverseProvider;
    private readonly WinEventDelegate _eventCallback;
    private readonly object _knownHandlesLock = new();
    private HashSet<nint> _knownTrackedHandles = [];
    private nint _showHook;

    public AutoArrangeController(
        TopLevelWindowEnumerator windowEnumerator,
        HeuristicWindowOrderResolver windowOrderResolver,
        ArrangeOperationTracker arrangeOperationTracker,
        Func<IReadOnlyList<TopLevelWindowInfo>>? startupWindowsProvider = null,
        Func<IReadOnlyList<TopLevelWindowInfo>>? orderingUniverseProvider = null)
    {
        _windowEnumerator = windowEnumerator;
        _windowOrderResolver = windowOrderResolver;
        _arrangeOperationTracker = arrangeOperationTracker;
        _startupWindowsProvider = startupWindowsProvider ?? _windowEnumerator.EnumerateTrackableVsCodeWindows;
        _orderingUniverseProvider = orderingUniverseProvider ?? _windowEnumerator.EnumerateTrackableVsCodeWindows;
        _eventCallback = HandleWinEvent;
    }

    public void Start()
    {
        SeedExistingWindows();
        _showHook = SetWinEventHook(
            EventObjectShow,
            EventObjectShow,
            nint.Zero,
            _eventCallback,
            0,
            0,
            WineventOutOfContext | WineventSkipOwnProcess);
    }

    public bool HandlePotentialArrangeWindow(nint handle, int objectId, int childId)
    {
        if (_arrangeOperationTracker.IsSuppressed)
        {
            return false;
        }

        if (handle == nint.Zero || objectId != ObjIdWindow || childId != 0)
        {
            return false;
        }

        var window = _windowEnumerator.TryGetWindowInfo(handle);
        if (window is null || !VsCodeWindowEligibility.IsEligible(window))
        {
            return false;
        }

        var orderingUniverse = _orderingUniverseProvider();
        if (!TryAdvanceKnownTrackedHandles(orderingUniverse))
        {
            return false;
        }

        return _windowOrderResolver.ObserveWindow(window);
    }

    public void Dispose()
    {
        if (_showHook != nint.Zero)
        {
            UnhookWinEvent(_showHook);
            _showHook = nint.Zero;
        }
    }

    private void HandleWinEvent(
        nint hWinEventHook,
        uint @event,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime)
    {
        HandlePotentialArrangeWindow(hwnd, idObject, idChild);
    }

    private void SeedExistingWindows()
    {
        var startupWindows = _startupWindowsProvider()
            .OrderBy(window => window.CurrentLeft)
            .ThenBy(window => window.CurrentTop)
            .ThenBy(window => window.ProcessStartTimeUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(window => window.ProcessId)
            .ThenBy(window => window.Handle)
            .ToArray();

        lock (_knownHandlesLock)
        {
            _knownTrackedHandles = startupWindows.Select(window => window.Handle).ToHashSet();
        }

        foreach (var window in startupWindows)
        {
            _windowOrderResolver.ObserveWindow(window);
        }
    }

    private bool TryAdvanceKnownTrackedHandles(IReadOnlyList<TopLevelWindowInfo> orderingUniverse)
    {
        lock (_knownHandlesLock)
        {
            var nextKnownHandles = orderingUniverse.Select(window => window.Handle).ToHashSet();
            var discoveredNewHandle = nextKnownHandles.Except(_knownTrackedHandles).Any();
            _knownTrackedHandles = nextKnownHandles;
            return discoveredNewHandle;
        }
    }

    private delegate void WinEventDelegate(
        nint hWinEventHook,
        uint @event,
        nint hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hWinEventHook);
}
