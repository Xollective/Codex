using System;
using System.Threading;
using System.Threading.Tasks;
using Codex.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Codex.Analysis
{
    public class StaticTextLoader : TextLoader
    {
        public string Content { get; }

        private SourceText sourceText;

        public StaticTextLoader(string content)
        {
            Content = content;
        }

        public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        {
            sourceText = sourceText ?? SourceText.From(Content, checksumAlgorithm: options.ChecksumAlgorithm);
            return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Default));
        }
    }
}