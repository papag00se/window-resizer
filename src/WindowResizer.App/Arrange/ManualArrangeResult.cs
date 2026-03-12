namespace WindowResizer.App.Arrange;

public enum ManualArrangeStatus
{
    Success,
    NoEligibleWindows
}

public sealed record ManualArrangeResult(
    ManualArrangeStatus Status,
    int ArrangedWindowCount,
    int EffectiveWidthPx);
