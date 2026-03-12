namespace WindowResizer.Core.Windows;

public sealed record TopLevelWindowInfo(
    nint Handle,
    string Title,
    string ClassName,
    int ProcessId,
    string ProcessName,
    DateTimeOffset? ProcessStartTimeUtc,
    int CurrentLeft,
    int CurrentTop,
    bool IsVisible,
    bool IsMinimized,
    bool IsCloaked,
    bool HasOwner,
    bool IsToolWindow);
