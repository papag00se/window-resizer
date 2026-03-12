namespace WindowResizer.Core.Windows;

public static class VsCodeWindowEligibility
{
    private static readonly StringComparer ProcessNameComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly string[] SupportedProcessNames = ["Code", "Code - Insiders"];

    public static bool IsOrderTrackable(TopLevelWindowInfo window)
    {
        ArgumentNullException.ThrowIfNull(window);

        return !window.IsCloaked
            && !window.HasOwner
            && !window.IsToolWindow
            && SupportedProcessNames.Contains(window.ProcessName, ProcessNameComparer);
    }

    public static bool IsEligible(TopLevelWindowInfo window)
    {
        ArgumentNullException.ThrowIfNull(window);

        return window.IsVisible
            && !window.IsMinimized
            && IsOrderTrackable(window);
    }
}
