namespace WindowResizer.Core.Layout;

public static class WindowLayoutEngine
{
    public static WindowLayoutPlan CreateLayout(MonitorWorkArea workArea, int requestedWidthPx, int windowCount)
    {
        if (requestedWidthPx <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedWidthPx),
                requestedWidthPx,
                "Requested width must be greater than zero.");
        }

        if (windowCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windowCount),
                windowCount,
                "Window count cannot be negative.");
        }

        if (workArea.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workArea),
                workArea.Width,
                "Work area width must be greater than zero.");
        }

        if (workArea.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workArea),
                workArea.Height,
                "Work area height must be greater than zero.");
        }

        var effectiveWidthPx = Math.Min(requestedWidthPx, workArea.Width);
        if (windowCount == 0)
        {
            return new WindowLayoutPlan(effectiveWidthPx, Array.Empty<WindowLayoutRect>());
        }

        var rectangles = new List<WindowLayoutRect>(windowCount);
        var step = windowCount == 1
            ? 0d
            : (workArea.Width - effectiveWidthPx) / (double)(windowCount - 1);

        for (var index = 0; index < windowCount; index++)
        {
            var x = windowCount == 1
                ? workArea.Left
                : workArea.Left + (int)Math.Round(index * step, MidpointRounding.AwayFromZero);

            rectangles.Add(new WindowLayoutRect(x, workArea.Top, effectiveWidthPx, workArea.Height));
        }

        return new WindowLayoutPlan(effectiveWidthPx, rectangles);
    }
}
