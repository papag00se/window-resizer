namespace WindowResizer.Core.Layout;

public sealed record WindowLayoutPlan(int EffectiveWidthPx, IReadOnlyList<WindowLayoutRect> Rectangles);
