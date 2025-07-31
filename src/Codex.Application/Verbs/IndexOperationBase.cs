using Codex.Lucene.Search;
using Codex.Storage.Store;
using Codex.Utilities;
using DotNext;

namespace Codex.Application.Verbs;

public abstract record IndexReadOperationBase : OperationBase
{
    [Option('o', "out", HelpText = "The directory to the write the index or analysis data.")]
    public string OutputDirectory { get; set; }

    internal ICodexStore OutputStore { get; set; }

    public virtual Task CleanupAsync()
    {
        return Task.CompletedTask;
    }
}
