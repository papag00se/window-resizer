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
        bool normalizeZOrder = true,
        IReadOnlyList<TopLevelWindowInfo>? orderingUniverse = null)
    {
        using var arrangeScope = _arrangeOperationTracker.Enter();

        var eligibleWindows = _windowSource.EnumerateEligibleWindows();
        var placements = ResolvePlacements(eligibleWindows, preferCurrentScreenOrder, orderingUniverse);
        if (placements.Count == 0)
        {
            return new ManualArrangeResult(ManualArrangeStatus.NoEligibleWindows, 0, 0);
        }

        if (synchronizeTaskbarOrder)
        {
            _windowVisibilityOrderSynchronizer.SynchronizeOrder(placements.Select(placement => placement.Window).ToArray());
        }

        var workArea = _windowPositioningService.GetWorkAreaForWindow(placements[0].Window.Handle);
        var plan = WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx, placements.Max(placement => placement.SlotIndex) + 1);

        foreach (var placement in placements)
        {
            _windowPositioningService.ApplyWindowRect(placement.Window.Handle, plan.Rectangles[placement.SlotIndex]);
        }

        if (normalizeZOrder)
        {
            _windowPositioningService.ApplyWindowZOrder(placements.Select(placement => placement.Window.Handle).ToArray());
        }

        return new ManualArrangeResult(ManualArrangeStatus.Success, placements.Count, plan.EffectiveWidthPx);
    }

    private IReadOnlyList<WindowPlacement> ResolvePlacements(
        IReadOnlyList<TopLevelWindowInfo> eligibleWindows,
        bool preferCurrentScreenOrder)
    {
        var orderedEligibleWindows = ResolveArrangeOrder(eligibleWindows, preferCurrentScreenOrder);
        return orderedEligibleWindows
            .Select((window, index) => new WindowPlacement(window, index))
            .ToArray();
    }

    private IReadOnlyList<WindowPlacement> ResolvePlacements(
        IReadOnlyList<TopLevelWindowInfo> eligibleWindows,
        bool preferCurrentScreenOrder,
        IReadOnlyList<TopLevelWindowInfo>? orderingUniverse)
    {
        if (orderingUniverse is null || preferCurrentScreenOrder)
        {
            return ResolvePlacements(eligibleWindows, preferCurrentScreenOrder);
        }

        var orderedUniverse = ResolveArrangeOrder(orderingUniverse, preferCurrentScreenOrder: false);
        var eligibleWindowsByHandle = eligibleWindows.ToDictionary(window => window.Handle);
        var placements = new List<WindowPlacement>(eligibleWindows.Count);

        for (var index = 0; index < orderedUniverse.Count; index++)
        {
            if (eligibleWindowsByHandle.TryGetValue(orderedUniverse[index].Handle, out var window))
            {
                placements.Add(new WindowPlacement(window, index));
            }
        }

        if (placements.Count > 0)
        {
            return placements;
        }

        return ResolvePlacements(eligibleWindows, preferCurrentScreenOrder: false);
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

    private sealed record WindowPlacement(TopLevelWindowInfo Window, int SlotIndex);
}
