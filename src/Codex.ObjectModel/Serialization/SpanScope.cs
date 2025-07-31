using System.Diagnostics.ContractsLight;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Codex.Utilities;

public unsafe struct SpanScope<T> : IDisposable
{
    private void* _ptr;
    private int _length;

    private SpanScope(void* ptr, int length)
    {
        _ptr = ptr;
        _length = length;
    }

    public Span<T> Span
    {
        get
        {
            Contract.Assert(_length >= 0, "Cannot use disposed scope");
            if (_length == 0)
            {
                return default;
            }

            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_ptr), _length);
        }
    }

    public static SpanScope<T> FromSpan(Span<T> span)
    {
        if (span.Length == 0)
        {
            return default;
        }

        return new SpanScope<T>(Unsafe.AsPointer(ref span[0]), span.Length);
    }

    public void Dispose()
    {
        _length = -1;
    }
}

public unsafe struct ReadOnlySpanScope<T> : IDisposable
    where T : unmanaged
{
    private void* _ptr;
    private int _length;

    private ReadOnlySpanScope(void* ptr, int length)
    {
        _ptr = ptr;
        _length = length;
    }

    public ReadOnlySpan<T> Span
    {
        get
        {
            Contract.Assert(_length >= 0, "Cannot use disposed scope");
            if (_length == 0)
            {
                return default;
            }

            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_ptr), _length);
        }
    }

    public static ReadOnlySpanScope<T> FromSpan(ReadOnlySpan<T> span)
    {
        if (span.Length == 0)
        {
            return default;
        }

        fixed (void* ptr = &span.GetPinnableReference())
        {
            return new ReadOnlySpanScope<T>(ptr, span.Length);
        }
    }

    public void Dispose()
    {
        _length = -1;
    }
}
