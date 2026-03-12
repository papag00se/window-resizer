using WindowResizer.Core.Layout;

namespace WindowResizer.App.Arrange;

public interface IWindowPositioningService
{
    MonitorWorkArea GetWorkAreaForWindow(nint handle);

    void ApplyWindowRect(nint handle, WindowLayoutRect rectangle);
}
