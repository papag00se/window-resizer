using WindowResizer.Core.Layout;

namespace WindowResizer.App.Arrange;

public sealed class ManualArrangeService
{
    private readonly IEligibleWindowSource _windowSource;
    private readonly IWindowPositioningService _windowPositioningService;
    private readonly HeuristicWindowOrderResolver _windowOrderResolver;

    public ManualArrangeService(
        IEligibleWindowSource windowSource,
        IWindowPositioningService windowPositioningService,
        HeuristicWindowOrderResolver windowOrderResolver)
    {
        _windowSource = windowSource;
        _windowPositioningService = windowPositioningService;
        _windowOrderResolver = windowOrderResolver;
    }

    public ManualArrangeResult ArrangeNow(int requestedWidthPx)
    {
        var windows = _windowOrderResolver.OrderWindows(_windowSource.EnumerateEligibleWindows());
        if (windows.Count == 0)
        {
            return new ManualArrangeResult(ManualArrangeStatus.NoEligibleWindows, 0, 0);
        }

        var workArea = _windowPositioningService.GetWorkAreaForWindow(windows[0].Handle);
        var plan = WindowLayoutEngine.CreateLayout(workArea, requestedWidthPx, windows.Count);

        for (var index = 0; index < windows.Count; index++)
        {
            _windowPositioningService.ApplyWindowRect(windows[index].Handle, plan.Rectangles[index]);
        }

        return new ManualArrangeResult(ManualArrangeStatus.Success, windows.Count, plan.EffectiveWidthPx);
    }
}
