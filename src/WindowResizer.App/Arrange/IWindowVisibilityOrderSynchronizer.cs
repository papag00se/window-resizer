using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public interface IWindowVisibilityOrderSynchronizer
{
    void SynchronizeOrder(IReadOnlyList<TopLevelWindowInfo> windows);
}
