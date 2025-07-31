using System;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codex.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Codex.Analysis
{
    public class FileSystemTextLoader : TextLoader
    {
        public FileSystemTextLoader(FileSystem fileSystem, string path)
        {
            FileSystem = fileSystem;
            Path = path;
        }

        public FileSystem FileSystem { get; }
        public string Path { get; }
        public SourceEncodingInfo? EncodingInfo { get; private set; }

        public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        {
            using var stream = FileSystem.OpenFile(Path);
            using var bomCaptureStream = new BomDetectionStream(stream);
            var sourceText = SourceText.From(bomCaptureStream, checksumAlgorithm: options.ChecksumAlgorithm);

            EncodingInfo = bomCaptureStream.GetEncodingInfo(sourceText.Encoding);

            return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Default));
        }
    }
}