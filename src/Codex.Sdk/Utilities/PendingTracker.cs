using System.Diagnostics;
using BuildXL.Utilities.Threading;
using Codex.Utilities.Tasks;

namespace Codex.Utilities;

public class PendingTracker
{
    private readonly ReadWriteLock _nodeLock = ReadWriteLock.Create();

    private WaitNode _currentNode;

    public PendingTracker()
    {
        _currentNode = new(priorNodeTask: Task.CompletedTask);
    }

    public async ValueTask WaitAsync()
    {
        var node = _currentNode;
        if (Atomic.TryCompareExchangeValue(ref node.State, NodeState.HasPending, NodeState.Waiting, new(out var state)))
        {
            node.Release();

            var nextNode = new WaitNode(priorNodeTask: node.Task);
            nextNode.Take();

            try
            {
                using (_nodeLock.AcquireWriteLock())
                {
                    _currentNode = nextNode;
                }

                await node.Task;
            }
            finally
            {
                nextNode.Release();
            }
        }
        else if (state == NodeState.None)
        {
            await node.PriorNodeTask;
        }
        else
        {
            await node.Task;
        }
    }

    public IDisposable Track()
    {
        using var _ = _nodeLock.AcquireReadLock();
        var node = _currentNode;
        Atomic.CompareExchangeValue(ref node.State, NodeState.None, NodeState.HasPending);
        node.Take();
        return node;
    }

    private enum NodeState
    {
        None,
        HasPending,
        Waiting
    }

    private class WaitNode(Task priorNodeTask) : IDisposable
    {
        private int _pendingCount = 0;

        public readonly Task PriorNodeTask = priorNodeTask;

        public NodeState State;

        private readonly TaskSourceSlim<object> _taskSource = TaskSourceSlim.Create<object>();
        public Task Task => _taskSource.Task;

        public void Take()
        {
            Interlocked.Increment(ref _pendingCount);
        }

        public void Release()
        {
            var pendingCount = Interlocked.Decrement(ref _pendingCount);
            Contract.Assert(pendingCount >= -1);
            if (pendingCount == -1)
            {
                _taskSource.TrySetResult(default);
            }
            else if (pendingCount == 0)
            {
                Atomic.CompareExchangeValue(ref State, NodeState.HasPending, NodeState.None);
            }
        }

        void IDisposable.Dispose()
        {
            Release();
        }
    }
}