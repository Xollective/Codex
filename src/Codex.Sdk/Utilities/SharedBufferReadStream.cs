using System.Buffers;
using BuildXL.Utilities;
using Codex.Utilities.Serialization;
using DotNext;

namespace Codex.Utilities;

public class SharedValues<T>(int count, Func<int, T> initializeBuffer, Action<int, T>? cleanupBuffer)
{
    public int Count => count;

    private readonly ArrayBuilder<Handle?> _bufferHandles = new(count) { Length = count };

    public Handle GetBufferHandle(int index)
    {
        ref var handle = ref _bufferHandles[index];
        var localHandle = handle;

        while (true)
        {
            if (handle?.TryReference() == true)
            {
                return handle;
            }

            localHandle = new(index, Lazy.Create(() =>
            {
                return initializeBuffer(index);
            }),
            onCleanup: lazyValue =>
            {
                if (lazyValue.IsValueCreated)
                {
                    cleanupBuffer?.Invoke(index, lazyValue.Value);
                }
            });

            if (Atomic.TryCompareExchange(ref handle, comparand: handle, value: localHandle))
            {
                return localHandle;
            }
        }
    }

    public class Handle(int index, Lazy<T> value, Action<Lazy<T>>? onCleanup = null)
        : RefCountHandle<Lazy<T>>(value, onCleanup)
    {
        public int Index => index;

        public new T Value => base.Value.Value;
    }
}

public class SharedBuffersReadStream(SharedValues<ArraySegment<byte>> buffers, int bufferSize, long length) : ReadStream
{
    public static SharedBuffersReadStream CreatePooledBufferFileReadStream(int bufferSize, FileStream fileStream)
    {
        var fileHandle = fileStream.SafeFileHandle;

        var length = fileStream.Length;

        var fileExtent = new LongExtent(0, length);

        bufferSize = Bits.NextHighestPowerOfTwo(bufferSize);
        bufferSize = (int)Math.Min(length, bufferSize);

        var count = (int)NumberUtils.DivCeiling(length, bufferSize);

        var buffers = new SharedValues<ArraySegment<byte>>(count,
            initializeBuffer: index =>
            {
                var fileOffset = index * (long)bufferSize;
                var readLength = (int)Math.Min(bufferSize, length - fileOffset);

                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

                var segment = new ArraySegment<byte>(buffer, 0, readLength);

                RandomAccess.Read(fileHandle, segment.AsSpan(), fileOffset);

                return segment;
            },
            cleanupBuffer: (index, buffer) =>
            {
                ArrayPool<byte>.Shared.Return(buffer.Array);
            });

        return new SharedBuffersReadStream(buffers, bufferSize, length);
    }

    public SharedBuffersReadStream New(long? startPosition = null)
    {
        return new(buffers, bufferSize, length)
        {
            Position = startPosition ?? Position
        };
    }

    public override bool CanSeek => true;

    public override long Length => length;

    public override long Position { get; set; }

    private SharedValues<ArraySegment<byte>>.Handle? _currentHandle;

    public override void Flush()
    {
    }

    public override int ReadCore(Span<byte> target)
    {
        var (bufferIndex, bufferOffset) = Math.DivRem(Position, bufferSize);
        if (_currentHandle?.Index != bufferIndex)
        {
            _currentHandle?.Release();
            _currentHandle = buffers.GetBufferHandle((int)bufferIndex);
        }

        Contract.Assert(_currentHandle.IsValid);

        var source = _currentHandle.Value.AsSpan((int)bufferOffset);
        int readCount = source.SafeCopyTo(target);
        Position += readCount;
        return readCount;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            _ => Length + offset
        };

        return Position = position;
    }

    protected override void Dispose(bool disposing)
    {
        _currentHandle?.Release();
        base.Dispose(disposing);
    }
}