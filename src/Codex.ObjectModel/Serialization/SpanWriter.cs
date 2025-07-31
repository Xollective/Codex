// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

#nullable enable

namespace Codex.Utilities.Serialization
{
    /// <summary>
    /// A lightweight wrapper for writing data into <see cref="Span{T}"/>.
    /// </summary>
    /// <remarks>
    /// The main purpose of this type is serializing instances directly to spans.
    /// Because its a struct, the serialization methods should get the instance by ref in order for the caller methods to "observe" the position
    /// changes that happen during serialization.
    /// The instance might be created with <see cref="CodexArrayBufferWriter{T}"/> to allow writing into an expandable buffers.
    /// </remarks>
    public ref struct SpanWriter
    {
        public delegate bool RequestBytesDelegate(ref SpanWriter writer, int requiredLength, bool synchronize = false);

        public RequestBytesDelegate? RequestBytes;

        /// <summary>
        /// The original span.
        /// </summary>
        public Span<byte> Span { get; private set; }

        /// <summary>
        /// The current position inside the span. A valid range is [0..Span.Length].
        /// </summary>
        public int Position { get; set; }


        /// <nodoc />
        public bool IsEnd => Span.Length == Position;

        /// <summary>
        /// Returns a remaining length available to the writer.
        /// </summary>
        public int RemainingLength => Span.Length - Position;

        /// <summary>
        /// Gets the remaining data in the original span.
        /// </summary>
        public Span<byte> Remaining => Span.Slice(Position);

        /// <summary>
        /// Gets the written data in the original span.
        /// </summary>
        public Span<byte> WrittenBytes => Span.Slice(0, Position);

        /// <nodoc />
        public SpanWriter(IBufferWriter<byte> bufferWriter, int defaultSizeHint = 4 * 1024)
        {
            RequestBytes = (ref SpanWriter writer, int requiredLength, bool synchronize) =>
            {
                writer.RequestBytesFromWriter(bufferWriter, requiredLength);
                return true;
            };

            Span = bufferWriter.GetSpan(defaultSizeHint);
            Position = 0;
        }
        
        /// <nodoc />
        private SpanWriter(Span<byte> span, int position, RequestBytesDelegate requestBytes)
        {
            RequestBytes = requestBytes;

            Span = span;
            Position = position;
        }
        
        /// <nodoc />
        public SpanWriter(Span<byte> span)
        {
            Span = span;
            Position = 0;
            RequestBytes = null;
        }

        /// <nodoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte b)
        {
            EnsureLength(sizeof(byte));
            Span[Position++] = b;
        }
        
        /// <nodoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteShort(ushort value)
        {
            unchecked
            {
                EnsureLength(sizeof(ushort));
                Span[Position] = (byte)value;
                Span[Position + 1] = (byte)(value >> 8);

                Position += 2;
            }
        }

        /// <summary>
        /// Advances the current position by <paramref name="length"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The operation will fail if <code>Position + length >= Span.Length;</code>.
        /// </exception>
        public void Advance(int length)
        {
            EnsureLength(length);
            Position += length;
        }

        /// <nodoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Forcing the inlineing of this method, because its used very often.
        public void WriteSpan(scoped ReadOnlySpan<byte> source)
        {
            EnsureLength(source.Length);
            source.CopyTo(Span.Slice(Position, source.Length));
            Position += source.Length;
        }

        /// <summary>
        /// Makes sure that the write has enough space for <paramref name="minLength"/>.
        /// </summary>
        public void EnsureLength(int minLength)
        {
            if (RemainingLength < minLength && RequestBytes?.Invoke(ref this, minLength) != true)
            {
                // Extracting the throw method to make the current one inline friendly.
                InsufficientLengthException.Throw(minLength, RemainingLength);
            }
        }

        private void RequestBytesFromWriter(IBufferWriter<byte> bufferWriter, int requiredLength)
        {
            var newSpan = bufferWriter.GetSpan(sizeHint: (Position + requiredLength) * 2);
            var other = new SpanWriter(newSpan, Position, RequestBytes!);
            this = other;
        }

        public void Dispose()
        {
            Synchronize();
        }

        private void Synchronize()
        {
            RequestBytes?.Invoke(ref this, 0, synchronize: true);
        }

        private void Initialize()
        {
            RequestBytes?.Invoke(ref this, 0, synchronize: false);
        }

        public static SpanWriter FromMemoryStream(MemoryStream stream, bool fromCurrent = false, RequestBytesDelegate? requestBytes = null)
        {
            var position = (int)stream.Position;
            var offset = fromCurrent ? position : 0;

            requestBytes ??= (ref SpanWriter writer, int requiredLength, bool synchronize) =>
            {
                var requiredSize = writer.Position + requiredLength + offset;
                if (synchronize)
                {
                    stream.SetLength(requiredSize);
                    stream.Position = requiredSize;

                    writer.Span = stream.GetBuffer().AsSpan(offset, writer.Position);
                }
                else
                {
                    if (requiredSize > stream.Capacity)
                    {
                        stream.SetLength(requiredSize);
                    }

                    if (stream.Length < stream.Capacity)
                    {
                        stream.SetLength(stream.Capacity);
                    }

                    writer.Span = stream.GetBuffer().AsSpan(offset);
                }

                return true;
            };

            var buffer = stream.GetBuffer().AsSpan(offset);
            var writer = new SpanWriter(buffer, position: fromCurrent ? 0 : position, requestBytes);
            writer.Initialize();
            return writer;
        }

        /// <nodoc />
        public static implicit operator SpanWriter(Span<byte> span)
        {
            return new SpanWriter(span);
        }
    }
}