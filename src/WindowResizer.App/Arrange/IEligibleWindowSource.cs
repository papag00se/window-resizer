using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public interface IEligibleWindowSource
{
    IReadOnlyList<TopLevelWindowInfo> EnumerateEligibleWindows();
}
