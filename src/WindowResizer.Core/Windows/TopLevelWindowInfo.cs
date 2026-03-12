namespace WindowResizer.Core.Windows;

public sealed record TopLevelWindowInfo(
    nint Handle,
    string Title,
    string ClassName,
    string ProcessName,
    bool IsVisible,
    bool IsMinimized,
    bool IsCloaked,
    bool HasOwner,
    bool IsToolWindow);
