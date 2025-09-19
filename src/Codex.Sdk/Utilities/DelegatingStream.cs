// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // Forwards all calls to an inner stream except where overridden in a derived class.
    public class DelegatingStream(Stream innerStream, Stream writeStream = null) : Stream
    {
        public bool DisposeInnerStream { get; init; } = true;
        public bool DisposeWriteStream { get; init; } = true;

        protected Stream WriteStream { get; set; } = writeStream ?? innerStream;

        protected Stream InnerStream { get; set; } = innerStream;

        /// <summary>
        /// Action to perform on dispose
        /// </summary>
        public Action? OnDispose { get; init; }
       
        protected virtual void BeforeWrite(int length)
        {
        }

        protected virtual int AfterRead(int read)
        {
            return read;
        }

        #region Properties

        public override bool CanRead
        {
            get { return InnerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return InnerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return InnerStream.CanWrite; }
        }

        public override long Length
        {
            get { return InnerStream.Length; }
        }

        public override long Position
        {
            get { return InnerStream.Position; }
            set { InnerStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return InnerStream.ReadTimeout; }
            set { InnerStream.ReadTimeout = value; }
        }

        public override bool CanTimeout
        {
            get { return InnerStream.CanTimeout; }
        }

        public override int WriteTimeout
        {
            get { return InnerStream.WriteTimeout; }
            set { InnerStream.WriteTimeout = value; }
        }

        #endregion Properties

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (DisposeInnerStream)
                {
                    InnerStream.Dispose();
                }

                if (DisposeWriteStream && ((WriteStream != InnerStream) || !DisposeInnerStream))
                {
                    WriteStream.Dispose();
                }

                OnDispose?.Invoke();
            }
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            return InnerStream.DisposeAsync();
        }

        #region Read

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return AfterRead(InnerStream.Read(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            return AfterRead(InnerStream.Read(buffer));
        }

        public override int ReadByte()
        {
            var result = InnerStream.ReadByte();
            AfterRead(result >= 0 ? 1 : 0);
            return result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return AfterRead(await InnerStream.ReadAsync(buffer, offset, count, cancellationToken));
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return AfterRead(await InnerStream.ReadAsync(buffer, cancellationToken));
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return InnerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return AfterRead(InnerStream.EndRead(asyncResult));
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            InnerStream.CopyTo(destination, bufferSize);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return InnerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        #endregion Read

        #region Write

        public override void Flush()
        {
            WriteStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return WriteStream.FlushAsync(cancellationToken);
        }

        public override void SetLength(long value)
        {
            WriteStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BeforeWrite(count);
            WriteStream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            BeforeWrite(buffer.Length);
            WriteStream.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            BeforeWrite(1);
            WriteStream.WriteByte(value);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            BeforeWrite(count);
            return WriteStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            BeforeWrite(buffer.Length);
            return WriteStream.WriteAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            BeforeWrite(count);
            return WriteStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            WriteStream.EndWrite(asyncResult);
        }
        #endregion Write
    }
}