using Codex.Sdk;

public class AsyncThrottle
{
    private readonly Timestamp[] _pendingTimestamps;
    private int _getCursor = -1;
    private int _setCursor = -1;
    private readonly TimeSpan _period;
    private readonly SemaphoreSlim _semaphore;

    public AsyncThrottle(int maxOperations, TimeSpan period)
    {
        if (maxOperations <= 0) throw new ArgumentOutOfRangeException(nameof(maxOperations));
        _pendingTimestamps = new Timestamp[maxOperations];
        _semaphore = TaskUtilities.CreateSemaphore(maxOperations);
        _period = period;
    }

    public async ValueTask<ThrottleScope> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        var index = Interlocked.Increment(ref _getCursor) % _pendingTimestamps.Length;

        var timestamp = _pendingTimestamps[index];
        var elapsed = timestamp.Elapsed;
        var waitTime = _period - elapsed;
        if (waitTime > TimeSpan.Zero)
        {
            await Task.Delay(waitTime, cancellationToken);
        }

        return new ThrottleScope(this);
    }

    public void Release()
    {
        var index = Interlocked.Increment(ref _setCursor) % _pendingTimestamps.Length;
        _pendingTimestamps[index] = Timestamp.New();

        _semaphore.Release();
    }

    public struct ThrottleScope(AsyncThrottle owner) : IDisposable
    {
        public void Dispose()
        {
            owner.Release();
        }
    }
}
