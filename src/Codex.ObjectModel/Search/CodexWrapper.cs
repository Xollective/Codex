using Codex.ObjectModel;

namespace Codex.Sdk.Search
{
    public record CodexWrapper(ValueTask<ICodex> BaseCodexTask) : CodexWrapperBase
    {
        public CodexWrapper(ICodex BaseCodex) 
            : this(ValueTask.FromResult(BaseCodex))
        {
        }

        public override ValueTask<ICodex> GetBaseCodex(ContextCodexArgumentsBase arguments) => BaseCodexTask;
    }

    public abstract record CodexWrapperBase : ICodex
    {
        public abstract ValueTask<ICodex> GetBaseCodex(ContextCodexArgumentsBase arguments);

        public virtual async Task<IndexQueryResponse<ReferencesResult>> FindAllReferencesAsync(FindAllReferencesArguments arguments)
        {
            ModifyArguments(arguments);
            return await (await GetBaseCodex(arguments)).FindAllReferencesAsync(arguments);
        }

        public virtual async Task<IndexQueryHitsResponse<IDefinitionSearchModel>> FindDefinitionAsync(FindDefinitionArguments arguments)
        {
            ModifyArguments(arguments);
            return await (await GetBaseCodex(arguments)).FindDefinitionAsync(arguments);
        }

        public virtual async Task<IndexQueryResponse<ReferencesResult>> FindDefinitionLocationAsync(FindDefinitionLocationArguments arguments)
        {
            ModifyArguments(arguments);
            return await (await GetBaseCodex(arguments)).FindDefinitionLocationAsync(arguments);
        }

        public virtual async Task<IndexQueryResponse<GetProjectResult>> GetProjectAsync(GetProjectArguments arguments)
        {
            ModifyArguments(arguments);
            return await (await GetBaseCodex(arguments)).GetProjectAsync(arguments);
        }

        public virtual async Task<IndexQueryResponse<IBoundSourceFile>> GetSourceAsync(GetSourceArguments arguments)
        {
            ModifyArguments(arguments);
            return await (await GetBaseCodex(arguments)).GetSourceAsync(arguments);
        }

        public virtual async Task<IndexQueryHitsResponse<ISearchResult>> SearchAsync(SearchArguments arguments)
        {
            ModifyArguments(arguments);
            return await (await GetBaseCodex(arguments)).SearchAsync(arguments);
        }

        public async Task<IndexQueryHitsResponse<ICommit>> GetRepositoryHeadsAsync(GetRepositoryHeadsArguments arguments)
        {
            ModifyArguments(arguments);
            return await (await GetBaseCodex(arguments)).GetRepositoryHeadsAsync(arguments);
        }

        protected virtual void ModifyArguments(ContextCodexArgumentsBase arguments)
        {

        }
    }
}
