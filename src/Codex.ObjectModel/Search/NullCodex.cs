using Codex.ObjectModel;

namespace Codex.Sdk.Search
{
    public class NullCodex : ICodex
    {
        public async Task<IndexQueryResponse<ReferencesResult>> FindAllReferencesAsync(FindAllReferencesArguments arguments)
        {
            await Task.Yield();
            return PostProcess<IndexQueryResponse<ReferencesResult>>(new());
        }

        public async Task<IndexQueryHitsResponse<IDefinitionSearchModel>> FindDefinitionAsync(FindDefinitionArguments arguments)
        {
            await Task.Yield();
            return PostProcess<IndexQueryHitsResponse<IDefinitionSearchModel>>(new());
        }

        public async Task<IndexQueryResponse<ReferencesResult>> FindDefinitionLocationAsync(FindDefinitionLocationArguments arguments)
        {
            await Task.Yield();
            return PostProcess<IndexQueryResponse<ReferencesResult>>(new());
        }

        public async Task<IndexQueryResponse<GetProjectResult>> GetProjectAsync(GetProjectArguments arguments)
        {
            await Task.Yield();
            return PostProcess<IndexQueryResponse<GetProjectResult>>(new());
        }

        public async Task<IndexQueryHitsResponse<ICommit>> GetRepositoryHeadsAsync(GetRepositoryHeadsArguments arguments)
        {
            await Task.Yield();
            return PostProcess<IndexQueryHitsResponse<ICommit>>(new());
        }

        public async Task<IndexQueryResponse<IBoundSourceFile>> GetSourceAsync(GetSourceArguments arguments)
        {
            await Task.Yield();
            return PostProcess<IndexQueryResponse<IBoundSourceFile>>(new());
        }

        public async Task<IndexQueryHitsResponse<ISearchResult>> SearchAsync(SearchArguments arguments)
        {
            await Task.Yield();
            return PostProcess<IndexQueryHitsResponse<ISearchResult>>(new());
        }

        private TResponse PostProcess<TResponse>(TResponse response)
            where TResponse : IndexQueryResponse
        {
            response.Error = "No results found.";
            return response;
        }
    }
}
