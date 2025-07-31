
namespace Codex.Utilities.Tasks;

public class TaskCompletionScope : IAsyncDisposable
{
    private int _outstanding = 1;
    private List<Task> _failedTasks;

    private TaskSourceSlim<bool> _completion = TaskSourceSlim.Create<bool>();

    private Action<ValueTask> continueWithAction;

    public void Track(Taskish task)
    {
        Interlocked.Increment(ref _outstanding);

        continueWithAction ??= OnTaskCompleted;

        task.ContinueWith(continueWithAction);
    }

    public void Track<T>(ValueTask<T> task) => Track(task.IgnoreResultAsync());

    private void OnTaskCompleted(ValueTask t)
    {
        if (!t.IsCompletedSuccessfully)
        {
            _failedTasks ??= Atomic.Create(ref _failedTasks, new());
            lock (_failedTasks)
            {
                _failedTasks.Add(t.AsTask());
            }
        }

        Complete();
    }

    private void Complete()
    {
        if (Interlocked.Decrement(ref _outstanding) == 0)
        {
            if (_failedTasks != null)
            {
                _completion.TrySetFromTask(Task.WhenAll(_failedTasks), () => false);
            }
            else
            {
                _completion.TrySetResult(true);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        Complete();
        await _completion.Task;
    }
}