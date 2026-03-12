using System.Runtime.InteropServices;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public sealed class AutoArrangeController : IDisposable
{
    private const uint EventObjectShow = 0x8002;
    private const uint EventSystemForeground = 0x0003;
    private const int ObjIdWindow = 0;
    private const uint WineventOutOfContext = 0x0000;
    private const uint WineventSkipOwnProcess = 0x0002;

    private readonly TopLevelWindowEnumerator _windowEnumerator;
    private readonly Func<int> _widthProvider;
    private readonly ManualArrangeService _manualArrangeService;
    private readonly HeuristicWindowOrderResolver _windowOrderResolver;
    private readonly DebouncedActionScheduler _scheduler;
    private readonly WinEventDelegate _eventCallback;
    private nint _showHook;
    private nint _foregroundHook;

    public AutoArrangeController(
        TopLevelWindowEnumerator windowEnumerator,
        ManualArrangeService manualArrangeService,
        HeuristicWindowOrderResolver windowOrderResolver,
        Func<int> widthProvider,
        TimeSpan? debounceDelay = null)
    {
        _windowEnumerator = windowEnumerator;
        _manualArrangeService = manualArrangeService;
        _windowOrderResolver = windowOrderResolver;
        _widthProvider = widthProvider;
        _scheduler = new DebouncedActionScheduler(
            debounceDelay ?? TimeSpan.FromMilliseconds(250),
            () => _manualArrangeService.ArrangeNow(_widthProvider(), synchronizeTaskbarOrder: false));
        _eventCallback = HandleWinEvent;
    }

    public void Start()
    {
        _showHook = SetWinEventHook(
            EventObjectShow,
            EventObjectShow,
            nint.Zero,
            _eventCallback,
            0,
            0,
            WineventOutOfContext | WineventSkipOwnProcess);

        _foregroundHook = SetWinEventHook(
            EventSystemForeground,
            EventSystemForeground,
            nint.Zero,
            _eventCallback,
            0,
            0,
            WineventOutOfContext | WineventSkipOwnProcess);
    }

    public bool HandlePotentialArrangeWindow(nint handle, int objectId, int childId)
    {
        if (handle == nint.Zero || objectId != ObjIdWindow || childId != 0)
        {
            return false;
        }

        var window = _windowEnumerator.TryGetWindowInfo(handle);
        if (window is null || !VsCodeWindowEligibility.IsEligible(window))
        {
            return false;
        }

        _windowOrderResolver.ObserveWindow(window);
        _scheduler.Request();
        return true;
    }

    public void Dispose()
    {
        if (_showHook != nint.Zero)
        {
            UnhookWinEvent(_showHook);
            _showHook = nint.Zero;
        }

        if (_foregroundHook != nint.Zero)
        {
            UnhookWinEvent(_foregroundHook);
            _foregroundHook = nint.Zero;
        }

        _scheduler.Dispose();
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
