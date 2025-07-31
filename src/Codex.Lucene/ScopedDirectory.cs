using Codex.Utilities;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace Codex.Lucene.Search
{
    public class ScopedDirectory : FilterDirectory
    {
        public ScopedDirectory(Directory @in, string subPath) : base(@in)
        {
            SubPath = subPath;
        }

        public string SubPath { get; }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            name = Remap(name);
            return base.CreateOutput(name, context);
        }

        public override string[] ListAll()
        {
            return base.ListAll()
                .Where(s => s.StartsWith(SubPath))
                .Select(s => PathUtilities.GetRelativePath(SubPath, s))
                .ToArray();
        }

        public override bool FileExists(string name)
        {
            name = Remap(name);
            return base.FileExists(name);
        }

        public override void DeleteFile(string name)
        {
            name = Remap(name);
            base.DeleteFile(name);
        }

        public override long FileLength(string name)
        {
            name = Remap(name);
            return base.FileLength(name);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            name = Remap(name);
            return base.OpenInput(name, context);
        }

        public string Remap(string name)
        {
            return PathUtilities.UriCombine(SubPath, name);
        }
    }
}
