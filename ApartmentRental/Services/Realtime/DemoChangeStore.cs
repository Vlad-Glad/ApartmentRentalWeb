using System.Collections.Concurrent;

namespace ApartmentRental.Services.Realtime;

public sealed record DemoState(int Version, DateTimeOffset UpdatedAtUtc);

public interface IDemoChangeStore
{
    DemoState Get();
    DemoState Trigger();
    DemoState Reset();
    Task<DemoState?> WaitForChangeAsync(int sinceVersion, TimeSpan timeout, CancellationToken ct);
}

public sealed class DemoChangeStore : IDemoChangeStore
{
    private readonly object _lock = new();
    private DemoState _state = new(0, DateTimeOffset.UtcNow);

    // One or more waiters for long-poll
    private readonly List<TaskCompletionSource<DemoState>> _waiters = new();

    public DemoState Get()
    {
        lock (_lock) return _state;
    }

    public DemoState Trigger()
    {
        List<TaskCompletionSource<DemoState>> toRelease;

        lock (_lock)
        {
            _state = new DemoState(_state.Version + 1, DateTimeOffset.UtcNow);
            toRelease = _waiters.ToList();
            _waiters.Clear();
        }

        foreach (var w in toRelease)
            w.TrySetResult(_state);

        return _state;
    }

    public DemoState Reset()
    {
        List<TaskCompletionSource<DemoState>> toRelease;

        lock (_lock)
        {
            _state = new DemoState(0, DateTimeOffset.UtcNow);
            toRelease = _waiters.ToList();
            _waiters.Clear();
        }

        foreach (var w in toRelease)
            w.TrySetResult(_state);

        return _state;
    }


    public async Task<DemoState?> WaitForChangeAsync(int sinceVersion, TimeSpan timeout, CancellationToken ct)
    {
        DemoState current;
        TaskCompletionSource<DemoState> tcs;

        lock (_lock)
        {
            current = _state;
            if (current.Version > sinceVersion)
                return current;

            tcs = new TaskCompletionSource<DemoState>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add(tcs);
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            return await tcs.Task.WaitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout or client aborted
            lock (_lock)
            {
                _waiters.Remove(tcs);
            }
            return null;
        }
    }
}
