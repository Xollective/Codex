using System;
using System.Runtime.CompilerServices;

using System.Threading;

namespace Codex.Utilities;

/// <summary>
/// Tracks the ref count to an object (initialized at 1) and allows for performing a cleanup
/// action when the ref count reaches zero.
/// </summary>
public class RefCountHandle<T>(T value, Action<T>? onCleanup = null) : IDisposable
{
    public T? Value { get; private set; } = value;

    private Action<T?>? _onCleanup = onCleanup;

    private long _refCount = 1;
    public bool IsValid => _refCount > 0;

    public string? LastReferenceCallerName { get; private set; }

    public RefCountHandle<T> GetValue(out T value)
    {
        Contract.Assert(IsValid);
        value = Value;
        return this;
    }

    public bool TryReference([CallerMemberName] string? name = null, bool force = false)
    {
        if (_refCount <= 0)
        {
            return false;
        }

        if (ChangeState(1) <= 0)
        {
            ChangeState(-1);
            return false;
        }

        LastReferenceCallerName = name;
        return true;
    }

    public void Release()
    {
        ChangeState(-1);
    }

    public void Dispose()
    {
        Release();
    }

    private long ChangeState(int addend)
    {
        var refCount = Interlocked.Add(ref _refCount, addend);
        if (refCount == 0)
        {
            // Ref count is zero try to clean up
            if (Atomic.TryCompareExchange(ref _refCount, int.MinValue, comparand: 0))
            {
                // Ref count is zero and cleanup was reserved
                _onCleanup?.Invoke(Value);
                _onCleanup = null;
                Value = default;
            }
        }

        return refCount;
    }
}