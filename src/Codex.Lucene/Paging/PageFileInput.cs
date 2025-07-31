using Lucene.Net.Store;

namespace Codex.Lucene.Search
{
    public class PageFileInput : BufferedIndexInput
    {
        public PageFileInput(Func<IPageFile> fileFactory, string resourceDesc) 
            : base(resourceDesc)
        {
            File = fileFactory();
            FileFactory = fileFactory;
        }

        public override object Clone()
        {
            var clone = (PageFileInput)base.Clone();
            clone.File = File.CreateClone();
            clone.Disposed = false;
            return clone;
        }

        public IPageFile File { get; private set; }
        public Func<IPageFile> FileFactory { get; }
        public long Position { get; set; }

        public bool Disposed { get; set; }

        public override long Length => File.Length;

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            File.Dispose();
        }

        protected override void ReadInternal(byte[] b, int offset, int length)
        {
            File.ReadRange(Position, b, offset, length);
            Position += length;
        }

        protected override void SeekInternal(long pos)
        {
            Position = pos;
        }

        public override void SkipBytes(long numBytes)
        {
            Seek(Position + numBytes);
        }
    }
}
