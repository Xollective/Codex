using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Codex.Lucene.Search
{
    public interface ILuceneEntityStore
    {
        Task InitializeAsync();

        Task FinalizeAsync();
    }

    public class LuceneEntityStore<T> : ILuceneEntityStore
        where T : class, ISearchEntity
    {
        private LuceneCodexStore Store { get; }
        private SearchType<T> SearchType { get; }
        private IndexWriter Writer { get; set; }

        public LuceneEntityStore(LuceneCodexStore store, SearchType<T> searchType)
        {
            Store = store;
            SearchType = searchType;
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();

            Writer = new IndexWriter(
                    Store.Configuration.OpenIndexDirectory(SearchType),
                    new IndexWriterConfig(LuceneConstants.CurrentVersion, LuceneConstants.StandardAnalyzer));
        }

        public async Task FinalizeAsync()
        {
            await Task.Yield();

            Writer.Dispose();
        }

        public async ValueTask AddAsync(T entity)
        {
            var document = new Document();

            Placeholder.Todo("Add fields to document");

            document.Add(new StoredField(LuceneConstants.SourceFieldName, GetSource(entity)));

            Writer.AddDocument(document);
        }

        private BytesRef GetSource(T entity)
        {
            throw Placeholder.NotImplementedException("Serialize entity. Should byte[] be pooled?");
        }
    }
}
