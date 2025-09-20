using BuildXL.Utilities;
using Codex.Sdk;
using Codex.Storage.BlockLevel;
using Codex.Utilities;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Codex
{
    public class DirectoryFileSystem : SystemFileSystem
    {
        public readonly string RootDirectory;
        protected readonly string SearchPattern;

        public DirectoryFileSystem(string rootDirectory, string searchPattern = "*")
        {
            RootDirectory = rootDirectory;
            SearchPattern = searchPattern;
        }

        public override IEnumerable<string> GetFiles()
        {
            return GetFiles("");
        }

        public override IEnumerable<string> GetFiles(string relativeDirectoryPath)
        {
            var path = Path.Combine(RootDirectory, relativeDirectoryPath);
            if (Directory.Exists(path))
            {
                return PathUtilities.GetAllRelativeFilesRecursive(directory: path, rootDirectory: RootDirectory);
            }
            else
            {
                return [];
            }
        }

        protected override string PreparePath(string filePath)
        {
            filePath = Path.Combine(RootDirectory, filePath);
            return base.PreparePath(filePath);
        }

        public override string ToString()
        {
            return $"{RootDirectory}({SearchPattern})";
        }
    }

    public class FlattenDirectoryFileSystem : DirectoryFileSystem
    {
        public FlattenDirectoryFileSystem(string rootDirectory, string searchPattern = "*")
            : base(rootDirectory, searchPattern)
        {
        }

        public override IEnumerable<string> GetFiles(string relativeDirectoryPath)
        {
            List<string> files = new List<string>();

            foreach (var subDirectory in Directory.GetDirectories(RootDirectory))
            {
                files.AddRange(GetFiles(subDirectory));
            }

            return files;
        }
    }

    public class ZipFileSystem : FileSystem
    {
        private readonly FileStream _file;
        private SharedBuffersReadStream _sharedBufferStream;
        public readonly string ArchivePath;
        private ZipFile zipFile;
        private byte[] capturedZipFileBytes;

        private static RefOfFunc<ZipFile, Stream> GetBaseStreamField = Reflector.GetFieldRef<ZipFile, Stream>("baseStream_");

        public ZipFileSystem(string archivePath, string password = null, string privateKey = null)
        {
            _file = File.Open(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _sharedBufferStream = SharedBuffersReadStream.CreatePooledBufferFileReadStream(128 << 10, _file);
            ArchivePath = archivePath;
            zipFile = new ZipFile(NewSubStream(), leaveOpen: false);
            zipFile.Password = MiscUtilities.TryGetZipPassword(zipFile, privateKey) ?? password;
            // Ensure entries are read
            var entries = GetEntries();
        }

        private Stream NewSubStream([CallerMemberName]string caller = null)
        {
            var stream = _sharedBufferStream.New();
            return SdkFeatures.TryGetAnalysisZipParallelReadStream.Value?.Invoke(caller, _file, stream) ?? stream;
        }

        public IEnumerable<ZipEntry> GetEntries()
        {
            for (int i = 0; i < zipFile.Count; i++)
            {
                yield return zipFile[i];
            }
        }

        public override Stream OpenFile(string filePath)
        {
            var entry = zipFile.GetEntry(filePath);
            var baseStreamField = GetBaseStreamField(zipFile);

            lock (zipFile)
            {
                var prior = baseStreamField.Value;
                try
                {
                    // HACK: need to replace underlying stream prior to calling GetInputStream
                    // so it captures the clone of the shared buffer stream to allow parallel
                    // reads
                    var subStream = NewSubStream(filePath);
                    baseStreamField.Value = subStream;

                    return new DelegatingStream(zipFile.GetInputStream(zipFile.GetEntry(filePath)))
                    {
                        OnDispose = () =>
                        {
                            if (subStream != _file)
                            {
                                subStream.Dispose();
                            }
                        }
                    };
                }
                finally
                {
                    baseStreamField.Value = prior;
                }
            }
        }

        private int GetBufferSize(long compressedSize)
        {
            var defaultBufferSize = 1 << 17; //128 kb

            var bufferSize = Bits.NextHighestPowerOfTwo((int)Math.Min(defaultBufferSize, compressedSize));

            return bufferSize;
        }

        public override bool FileExists(string filePath)
        {
            return zipFile.GetEntry(filePath) != null;
        }

        public override long GetFileSize(string filePath)
        {
            return zipFile.GetEntry(filePath).Size;
        }

        public override IEnumerable<string> GetFiles()
        {
            return GetEntries().Where(e => e.Size != 0).Select(e => e.Name).ToArray();
        }

        public override IEnumerable<string> GetFiles(string relativeDirectoryPath)
        {
            relativeDirectoryPath = PathUtilities.EnsureTrailingSlash(relativeDirectoryPath, '\\');
            var files = GetFiles();
            return files.Where(n => n.Replace('/', '\\').StartsWith(relativeDirectoryPath));
        }

        public override void Dispose()
        {
            zipFile.Close();
            zipFile = null;
            _sharedBufferStream.Dispose();
            _sharedBufferStream = null;
            _file.Close();
        }
    }
}