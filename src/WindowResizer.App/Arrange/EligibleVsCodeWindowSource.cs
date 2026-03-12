using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public sealed class EligibleVsCodeWindowSource : IEligibleWindowSource
{
    private readonly TopLevelWindowEnumerator _windowEnumerator = new();

    public IReadOnlyList<TopLevelWindowInfo> EnumerateEligibleWindows()
    {
        return _windowEnumerator.EnumerateEligibleVsCodeWindows();
    }
}
