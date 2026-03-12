using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public sealed class NoOpWindowVisibilityOrderSynchronizer : IWindowVisibilityOrderSynchronizer
{
    public void SynchronizeOrder(IReadOnlyList<TopLevelWindowInfo> windows)
    {
    }
}
