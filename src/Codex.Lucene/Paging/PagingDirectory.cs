using Lucene.Net.Store;

namespace Codex.Lucene.Search
{
    public class PagingDirectory : BaseDirectory
    {
        public PagingDirectoryInfo Info { get; }
        public IPageFileProvider Provider { get; }

        public PagingDirectory(PagingDirectoryInfo info, IPageFileProvider provider)
        {
            Info = info;
            Provider = provider;
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            throw new NotImplementedException();
        }

        public override void DeleteFile(string name)
        {
            if (Features.TrackOpenFiles)
            {
                var accessor = ((CachingPageFileProvider)Provider).Accessor;
                var fsAccessor = (FileSystemPageFileAccessor)accessor;

                if (Info.Entries.TryGetValue(name, out var entry))
                {
                    fsAccessor.DeleteError(entry.RealPath ?? name);
                }
                else
                {
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override bool FileExists(string name)
        {
            EnsureOpen();
            return Info.Entries.ContainsKey(name);
        }

        public override long FileLength(string name)
        {
            EnsureOpen();
            if (!Info.Entries.TryGetValue(name, out var file) || file == null)
            {
                throw new FileNotFoundException(name);
            }

            return file.Length;
        }

        public override string[] ListAll()
        {
            EnsureOpen();
            return Info.Entries.Keys.ToArray();
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            if (Info.Entries.TryGetValue(name, out var entry))
            {
                return new PageFileInput(() => Provider.CreatePageFile(entry.RealPath ?? name, entry), name);
            }
            else
            {
                throw new FileNotFoundException($"Cannot find mapping for file '{name}'. Entries={Info.Entries.Count}");
            }
        }

        public override void Sync(ICollection<string> names)
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}