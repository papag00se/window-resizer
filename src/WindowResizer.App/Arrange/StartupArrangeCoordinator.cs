namespace WindowResizer.App.Arrange;

public sealed class StartupArrangeCoordinator
{
    private readonly IEligibleWindowSource _windowSource;
    private readonly ManualArrangeService _manualArrangeService;

    public StartupArrangeCoordinator(
        IEligibleWindowSource windowSource,
        ManualArrangeService manualArrangeService)
    {
        _windowSource = windowSource;
        _manualArrangeService = manualArrangeService;
    }

    public ManualArrangeResult? ArrangeExistingWindowsIfNeeded(int requestedWidthPx)
    {
        var existingWindows = _windowSource.EnumerateEligibleWindows();
        if (existingWindows.Count <= 1)
        {
            return null;
        }

        return _manualArrangeService.ArrangeNow(requestedWidthPx, synchronizeTaskbarOrder: true);
    }
}
