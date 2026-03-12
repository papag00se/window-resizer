using WindowResizer.Core.Windows;

namespace WindowResizer.App.Arrange;

public sealed class HeuristicWindowOrderResolver
{
    private readonly object _lock = new();
    private readonly Dictionary<nint, long> _observedOrder = [];
    private long _nextSequence = 1;

    public bool ObserveWindow(TopLevelWindowInfo window)
    {
        ArgumentNullException.ThrowIfNull(window);

        lock (_lock)
        {
            if (_observedOrder.ContainsKey(window.Handle))
            {
                return false;
            }

            _observedOrder[window.Handle] = _nextSequence++;
            return true;
        }
    }

    public IReadOnlyList<TopLevelWindowInfo> OrderWindows(IReadOnlyList<TopLevelWindowInfo> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        lock (_lock)
        {
            return windows
                .OrderBy(window => _observedOrder.TryGetValue(window.Handle, out var sequence) ? 0 : 1)
                .ThenBy(window => _observedOrder.TryGetValue(window.Handle, out var sequence) ? sequence : long.MaxValue)
                .ThenBy(window => window.ProcessStartTimeUtc ?? DateTimeOffset.MaxValue)
                .ThenBy(window => window.ProcessId)
                .ThenBy(window => window.Handle)
                .ToArray();
        }
    }
}
