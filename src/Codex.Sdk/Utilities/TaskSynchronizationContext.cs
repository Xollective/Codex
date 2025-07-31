namespace Codex.Utilities;

/// <summary>
/// Helper to allow queuing to thread pool using SynchronizationContext
/// </summary>
public class TaskSynchronizationContext(TaskFactory taskScheduler) : SynchronizationContext
{
    public static TaskSynchronizationContext Default { get; } = new(new TaskFactory(TaskScheduler.Default));

    public override void Post(SendOrPostCallback d, object? state)
    {
        taskScheduler.StartNew(() =>
        {
            d(state);
        }).IgnoreAsync();
    }
}

public static class TaskEx
{
    public static TaskUtilities.SynchronizationContextAwaitable Yield() => TaskSynchronizationContext.Default.SwitchTo();
}
