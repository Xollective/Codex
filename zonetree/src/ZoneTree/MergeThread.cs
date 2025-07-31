namespace Tenray.ZoneTree;

public record struct MergeThread(Task Task)
{
    private static int _managedThreadId = 1000;

    public void Join()
    {
        Task.GetAwaiter().GetResult();
    }

    public int ManagedThreadId { get; } = Interlocked.Increment(ref _managedThreadId);
}
