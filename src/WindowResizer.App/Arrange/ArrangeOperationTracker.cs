namespace WindowResizer.App.Arrange;

public sealed class ArrangeOperationTracker
{
    private int _activeArrangeCount;
    private long _suppressedUntilUtcTicks;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly TimeSpan _cooldownDuration;

    public ArrangeOperationTracker(Func<DateTimeOffset>? utcNow = null, TimeSpan? cooldownDuration = null)
    {
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _cooldownDuration = cooldownDuration ?? TimeSpan.FromSeconds(2);
    }

    public bool IsActive => Volatile.Read(ref _activeArrangeCount) > 0;

    public bool IsSuppressed
    {
        get
        {
            if (IsActive)
            {
                return true;
            }

            var suppressedUntilUtcTicks = Interlocked.Read(ref _suppressedUntilUtcTicks);
            return suppressedUntilUtcTicks > _utcNow().UtcTicks;
        }
    }

    public IDisposable Enter()
    {
        Interlocked.Increment(ref _activeArrangeCount);
        return new Scope(this);
    }

    private sealed class Scope(ArrangeOperationTracker owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            Interlocked.Decrement(ref owner._activeArrangeCount);
            Interlocked.Exchange(
                ref owner._suppressedUntilUtcTicks,
                owner._utcNow().Add(owner._cooldownDuration).UtcTicks);
        }
    }
}
