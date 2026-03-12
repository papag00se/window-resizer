using WindowResizer.Core.Layout;
using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public sealed class ManualArrangeService
{
    private readonly IEligibleWindowSource _windowSource;
    private readonly IWindowPositioningService _windowPositioningService;
    private readonly HeuristicWindowOrderResolver _windowOrderResolver;
    private readonly IWindowVisibilityOrderSynchronizer _windowVisibilityOrderSynchronizer;
    private readonly ArrangeOperationTracker _arrangeOperationTracker;

    public ManualArrangeService(
        IEligibleWindowSource windowSource,
        IWindowPositioningService windowPositioningService,
        HeuristicWindowOrderResolver windowOrderResolver,
        IWindowVisibilityOrderSynchronizer? windowVisibilityOrderSynchronizer = null,
        ArrangeOperationTracker? arrangeOperationTracker = null)
    {
        _windowSource = windowSource;
        _windowPositioningService = windowPositioningService;
        _windowOrderResolver = windowOrderResolver;
        _windowVisibilityOrderSynchronizer = windowVisibilityOrderSynchronizer ?? new NoOpWindowVisibilityOrderSynchronizer();
        _arrangeOperationTracker = arrangeOperationTracker ?? new ArrangeOperationTracker();
    }

    public ManualArrangeResult ArrangeNow(
        int requestedWidthPx,
        bool synchronizeTaskbarOrder = true,
        bool preferCurrentScreenOrder = false,
        bool normalizeZOrder = true)
    {
        using var arrangeScope = _arrangeOperationTracker.Enter();

        var windows = ResolveArrangeOrder(_windowSource.EnumerateEligibleWindows(), preferCurrentScreenOrder);
        if (windows.Count == 0)
        {
            return new ManualArrangeResult(ManualArrangeStatus.NoEligibleWindows, 0, 0);
        }

        if (synchronizeTaskbarOrder)
        {
            _windowVisibilityOrderSynchronizer.SynchronizeOrder(windows);
        }

        var workArea = _windowPositioningService.GetWorkAreaForWindow(windows[0].Handle);
        var plan = WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx, windows.Count);

        for (var index = 0; index < windows.Count; index++)
        {
            _windowPositioningService.ApplyWindowRect(windows[index].Handle, plan.Rectangles[index]);
        }

        if (normalizeZOrder)
        {
            _windowPositioningService.ApplyWindowZOrder(windows.Select(window => window.Handle).ToArray());
        }

        return new ManualArrangeResult(ManualArrangeStatus.Success, windows.Count, plan.EffectiveWidthPx);
    }

    private IReadOnlyList<TopLevelWindowInfo> ResolveArrangeOrder(
        IReadOnlyList<TopLevelWindowInfo> windows,
        bool preferCurrentScreenOrder)
    {
        var heuristicOrder = _windowOrderResolver.OrderWindows(windows);
        if (!preferCurrentScreenOrder)
        {
            return heuristicOrder;
        }

        var heuristicIndexByHandle = heuristicOrder
            .Select((window, index) => new { window.Handle, Index = index })
            .ToDictionary(entry => entry.Handle, entry => entry.Index);

        return windows
            .OrderBy(window => window.CurrentLeft)
            .ThenBy(window => window.CurrentTop)
            .ThenBy(window => heuristicIndexByHandle[window.Handle])
            .ToArray();
    }
}
