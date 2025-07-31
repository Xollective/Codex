using Codex.Lucene.Search;
using Codex.Sdk.Search;
using Codex.Utilities;
using Codex.Web.Common;
using Lucene.Net.Index;

namespace Codex.Workspaces;

public record UpdatableCodex(string RootDirectory, ICodex RemoteCodex = null) : CodexWrapperBase
{
    protected LuceneCodex LocalCodex { get; private set; }

    public string IndexDirectory { get; } = Path.Combine(RootDirectory, "index");

    protected ValueTask<ICodex>? BaseCodexTask { get; set; }

    public override async ValueTask<ICodex> GetBaseCodex(ContextCodexArgumentsBase arguments)
    {
        if (BaseCodexTask == null)
        {
            BaseCodexTask = Task.Run(async () =>
            {
                if (!File.Exists(PagingHelpers.GetDirectoryInfoFilePath(IndexDirectory)))
                {
                    var store = new LuceneCodexStore(new LuceneWriteConfiguration(IndexDirectory));

                    await store.InitializeAsync();

                    await store.FinalizeAsync();
                }

                LocalCodex = new LuceneCodex(new LuceneConfiguration(IndexDirectory)
                {
                    DefaultAccessLevel = RepoAccess.Internal
                });

                ICodex codex = LocalCodex;

                if (RemoteCodex != null)
                {
                    codex = new TieredCodex(LocalCodex, RemoteCodex: RemoteCodex);
                }

                BaseCodexTask = ValueTask.FromResult(codex);
                return codex;

            }).ToValueTask();
        }

        return await BaseCodexTask.Value;
    }

    public async Task UpdateLocalCodex(Func<Task> updateAsync, bool clean = false)
    {
        var baseCodex = await GetBaseCodex(null);

        async Task<ICodex> updateBaseCodexAsync()
        {
            await Task.Yield();

            LocalCodex.Client.Dispose();
            if (clean)
            {
                PathUtilities.ForceDeleteDirectory(IndexDirectory);
            }
            await updateAsync();
            LocalCodex.Reset();
            BaseCodexTask = ValueTask.FromResult(baseCodex);
            return baseCodex;
        }

        BaseCodexTask = updateBaseCodexAsync().ToValueTask();

        await BaseCodexTask.Value;
    }
}
