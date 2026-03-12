namespace WindowResizer.App.Arrange;

public sealed class DebouncedActionScheduler : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly Action _action;
    private readonly object _lock = new();
    private System.Threading.Timer? _timer;

    public DebouncedActionScheduler(TimeSpan delay, Action action)
    {
        _delay = delay;
        _action = action;
    }

    public void Request()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = new System.Threading.Timer(_ => _action(), null, _delay, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
