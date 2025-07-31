using System.Collections.Concurrent;
using System.Text;
using Codex.Lucene.Framework;
using Codex.Lucene.Framework.AutoPrefix;
using Codex.Sdk.Search;
using Codex.Search;
using Codex.Storage;
using Codex.Storage.BlockLevel;
using Codex.Utilities;
using Codex.Utilities.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Support.Threading;

namespace Codex.Lucene.Search;

using static FullTextUtilities;
using static LuceneConstants;
using M = SearchMappings;

public class LuceneCodex : CodexBase<LuceneClient, LuceneConfiguration>
{
    private ConcurrentDictionary<(string name, StoredFilterKinds kind, RepoAccess? access), AsyncLazy<LuceneFilterContext>> _storedFilterCache;

    public LuceneClient Client { get; private set; }

    private Task<bool> _initializeTask = null;

    public Lazy<PageFileObjectStorage> PageFileStorage { get; private set; }

    public LuceneCodex(LuceneConfiguration configuration)
        : base(configuration)
    {
        FieldMappingCodec.EnsureRegistered();
        Reset();
    }

    public Task WarmAsync()
    {
        return Task.Run(() =>
        {
            foreach (var index in Client.Indices)
            {
                var createdIndex = index.Value;
            }
        });
    }

    public void Reset()
    {
        Client = new LuceneClient(this);
        _storedFilterCache = new();
        PageFileStorage = new Lazy<PageFileObjectStorage>(() => new PageFileObjectStorage(Configuration.PageFileAccessor, Configuration.PagingInfo));
    }

    private AsyncLazy<LuceneFilterContext> GetLuceneFilterAsync(string name, StoredFilterKinds kind)
    {
        return _storedFilterCache.GetIfOrAdd((name, kind, null), key => AsyncLazy.Create(async () =>
        {
            var filterAccessor = new StoredFilterFiles(PageFileStorage.Value, key.name, key.kind);

            var filter = await filterAccessor.ActiveFile.LoadAsync();

            return new LuceneFilterContext(
                Client,
                filter.ProjectReferenceCountSketch,
                SearchTypeFilterProvider.From(filter.AllFilter),
                SearchTypeFilterProvider.From(filter.DeclaredDefinitionFilter));
        }),
        shouldReplace: lazy => lazy.IsFaulted);
    }

    protected override async Task<StoredFilterSearchContext<LuceneClient>> GetStoredFilterContextAsync(
        ContextCodexArgumentsBase arguments)
    {
        await Atomic.RunOnceAsync(ref _initializeTask, this, static async @this =>
        {
            await @this.Client.InitializeAsync();
            return true;
        });

        RepoAccess accessLevel = arguments.AccessLevel ?? Configuration.DefaultAccessLevel;

        var accessFilter = await GetLuceneFilterAsync(accessLevel.GetGroupName(), StoredFilterKinds.access).GetValueAsync();

        if (arguments.DisableStoredFilter)
        {
            return accessFilter;
        }

        string scopeName = arguments.RepositoryScopeId ?? Configuration.DefaultGroup ?? AllGroupName;
        var kind = IsRepoFilterName(scopeName) ? StoredFilterKinds.repo : StoredFilterKinds.group;

        var result = await _storedFilterCache.GetIfOrAdd((scopeName, kind, accessLevel), key => AsyncLazy.Create(async () =>
        {
            var scopeFilter = await GetLuceneFilterAsync(scopeName, kind).GetValueAsync();

            return accessFilter.Intersect(scopeFilter);
        }),
        shouldReplace: lazy => lazy.IsFaulted).GetValueAsync();

        result.AccessLevel = accessLevel;

        return result;
    }
}

public interface ILuceneIndex : IDisposable
{
    IndexReader Reader { get; }
    IndexSearcher Searcher { get; }
    SearchType SearchType { get; }
}

public class LuceneClient : ClientBase, IDisposable
{
    private LuceneCodex codex;
    //private ActionQueue documentRetrievalQueue;
    private ParallelOptions documentRetrievalQueue;
    private IExternalRetrievalClient? ExternalClient;

    public List<ILazy<ILuceneIndex>> Indices { get; } = new List<ILazy<ILuceneIndex>>();

    public LuceneClient(LuceneCodex codex)
    {
        this.codex = codex;
        var concurrency = Environment.ProcessorCount * 2;
        ExternalClient = codex.Configuration.ExternalRetrievalClient;
        //documentRetrievalQueue = new ActionQueue(Environment.ProcessorCount.Todo("Use configuration"));
        documentRetrievalQueue = new ParallelOptions()
        {
            TaskScheduler = new LimitedConcurrencyLevelTaskScheduler(concurrency.Todo("Use configuration"))
        };
    }

    public async Task InitializeAsync()
    {
        if (ExternalClient != null)
        {
            await ExternalClient.InitializeAsync();
        }
    }

    public void Dispose()
    {
        foreach (var index in Indices)
        {
            if (index.IsValueCreated)
            {
                index.Value.Dispose();
            }
        }
    }

    public override IIndex<T> CreateIndex<T>(SearchType<T> searchType)
    {
        return new LuceneIndex<T>(this, searchType);
    }

    protected override Lazy<IIndex<T>> GetIndexFactory<T>(SearchType<T> searchType)
    {
        var factory = base.GetIndexFactory<T>(searchType);
        Indices.Add(Lazy.Create(() => (LuceneIndex<T>)factory.Value));
        return factory;
    }

    public class LuceneIndex<T> : IIndex<T>, ILuceneIndex
        where T : class, ISearchEntity<T>
    {
        private readonly LuceneClient client;
        private readonly SearchType<T> searchType;

        private const int TermCountThreshold = 256;

        SearchType ILuceneIndex.SearchType => searchType;

        public IndexReader Reader { get; }
        public IndexSearcher Searcher { get; }
        private readonly SummaryIndexReader SummaryIndexReader;
        private readonly Func<CodexQuery<T>, Query> FromCodexQuery;

        private NumericDocValues StableIdDocValues { get; }
        public QueryState<T> QueryState { get; }

        private Func<BytesRefString, bool>[] FieldPossiblyHasTerm { get; }

        private readonly CodexQueryBuilder<T> queryBuilder = new CodexQueryBuilder<T>();

        public int Count => Reader.MaxDoc;

        public LuceneIndex(LuceneClient client, SearchType<T> searchType)
        {
            this.client = client;
            this.searchType = searchType;

            if (Features.EnableSummaryIndex)
            {
                SummaryIndexReader = new SummaryIndexReader(client.codex.Configuration, searchType);
                Reader = SummaryIndexReader.MainReader;
                Searcher = SummaryIndexReader.MainSearcher;
                FromCodexQuery = SummaryIndexReader.FromCodexQuery;
            }
            else
            {
                Reader = client.codex.Configuration.OpenReader(searchType);
                Searcher = new IndexSearcher(Reader, TaskScheduler.Default);
                FromCodexQuery = FromCodexQueryCore;
                QueryState = QueryState<T>.Default;
                if (searchType.TypeId == SearchTypeId.Definition)
                {
                    FieldPossiblyHasTerm = new Func<BytesRefString, bool>[searchType.Fields.Count];
                    QueryState = QueryState with { Rewrite = RewriteQuery };
                }
            }

            StableIdDocValues = MultiDocValues.GetNumericValues(Reader, searchType.StableIdField.Name);
        }

        private Query RewriteQuery(Query luceneQuery, CodexQuery<T> codexQuery, QueryState<T> state)
        {
            if (codexQuery.Field?.BehaviorInfo.LowCardinalityTermOptimization == true
                && luceneQuery is TermQuery termQuery)
            {
                var possiblyHasTerm = FieldPossiblyHasTerm[codexQuery.Field.Index];
                if (possiblyHasTerm == null)
                {
                    var allTerms = new HashSet<BytesRefString>();
                    void populateTerms()
                    {
                        foreach (var leaf in Reader.Leaves)
                        {
                            var terms = leaf.AtomicReader.Fields.GetTerms(codexQuery.Field.Name);
                            if (terms == null) continue;

                            if (terms.Count > TermCountThreshold)
                            {
                                allTerms = null;
                                return;
                            }

                            foreach (var term in terms.Enumerate())
                            {
                                if (allTerms.Add(term.Copy())
                                    && allTerms.Count > TermCountThreshold)
                                {
                                    allTerms = null;
                                    return;
                                }
                            }
                        }
                    }

                    populateTerms();
                    possiblyHasTerm = allTerms != null
                        ? (allTerms.Count == 0
                            ? term => false
                            : term => allTerms.Contains(term))
                        : term => true;
                }

                if (!possiblyHasTerm(termQuery.Term.Bytes))
                {
                    return null;
                }
            }

            return luceneQuery;
        }

        private Query FromCodexQueryCore(CodexQuery<T> query)
        {
            return QueryConverter.FromCodexQuery(query, QueryState);
        }

        public void Dispose()
        {
            Reader.Dispose();
            SummaryIndexReader?.MainReader.Dispose();
        }

        private static readonly TopDocs EmptyTopDocs = new TopDocs(0, Array.Empty<ScoreDoc>(), 0);

        public async Task<IIndexSearchResponse<TResult>> QueryAsync<TResult>(
            IStoredFilterInfo storedFilterInfo,
            Func<CodexQueryBuilder<T>, CodexQuery<T>> buildQuery,
            Func<QueryOptions<T>, QueryOptions<T>> updateOptions = null)
            where TResult : T
        {
            QueryOptions<T> options = new QueryOptions<T>();
            options = updateOptions?.Invoke(options) ?? options;

            var luceneContext = (ILuceneStoredFilter)storedFilterInfo;
            var filterBits = luceneContext?.FilterProvider.GetFilter(searchType.TypeId);
            Filter filter = filterBits == null ? null : new StableIdFilter(searchType.StableIdField.Name, filterBits);

            await Task.Yield();

            var query = buildQuery(queryBuilder);
            var luceneQuery = FromCodexQuery(new BoostingCodexQuery<T>(query, options.Boost?.Invoke(queryBuilder)));
            var innerQuery = luceneQuery;

            if (options.ProjectSortField != null && luceneContext != null)
            {
                var scoreState = new ProjectReferenceScoreState(luceneContext.ProjectReferenceCountSketch);

                // Special case System.Private.Corelib to aggregate score for all corelib assemblies
                scoreState.ProjectScoreCache["System.Private.CoreLib"] =
                    scoreState.Sketch.Get("mscorlib")
                    + scoreState.Sketch.Get("system.runtime")
                    + scoreState.Sketch.Get("system.private.corelib");

                luceneQuery = new DocValuesScoreQuery(
                    luceneQuery,
                    scoreState,
                    options.ProjectSortField.Name);
            }

            var topDocs = luceneQuery == null ? EmptyTopDocs : Searcher.Search(luceneQuery, filter, options.Take ?? 1000);

            LuceneFeatures.OnQuery.Value?.Invoke(this, luceneQuery, filter);

            var docIds = topDocs.ScoreDocs.SelectList(sd => sd.Doc);

            var entities = await GetAsync<TResult>(docIds, options);

            var dupes = entities.GroupBy(e => e.StableId);

            var secondaryFilter = luceneContext.SecondaryFilter?.FilterProvider.GetFilter(searchType.TypeId);

            var results = entities.DistinctBy(e => e.StableId)
                .Select(r => new SearchHit<TResult>()
                {
                    Source = r,
                    MatchesSecondaryFilter = secondaryFilter?.Get(r.StableId) ?? false
                }).ToList<ISearchHit<TResult>>();

            if (options.HighlightField is { } highlightField)
            {
                StringBuilder stringBuffer = new StringBuilder();
                var highlighter = new Highlighter(
                    new SimpleHTMLFormatter(
                        HighlightStartTagCharString,
                        HighlightEndTagCharString),
                    new QueryScorer(innerQuery));

                var docVisitor = new DocumentVisitor(new Document());

                foreach (var result in results)
                {
                    docVisitor.Document.Fields.Clear();
                    highlightField.Visit(result.Source, docVisitor);

                    var fieldValue = docVisitor.Document.Get(highlightField.Name);
                    var lineCount = StringExtensions.EnumerateLineSpans(fieldValue).Count();

                    var highlightSpans = highlighter.GetBestSpans(StandardAnalyzer.GetTokenStream(highlightField.Name, fieldValue), fieldValue)
                        .Select(t => new ClassificationSpan() { Start = t.Start, Length = t.Length });

                    var highlights = IndexingUtilities.GetLineClassifications(highlightSpans, StringExtensions.EnumerateLineSpans(fieldValue));

                    ((SearchHit<TResult>)result).Highlights = highlights.Select(h => new TextLineSpan()
                    {
                        LineSpanText = h.FullLineExtent.GetExtentSubstring(fieldValue),
                        LineSpanStart = h.Offset,
                        LineNumber = h.LineNumber,
                        Start = h.Start,
                        Length = h.Length
                    }).ToList();
                }
            }

            return new IndexSearchResponse<TResult>()
            {
                Hits = results,
                Total = (options.Take == null || results.Count < options.Take) ? results.Count : topDocs.TotalHits
            };
        }

        public async Task<IReadOnlyList<TResult>> GetAsync<TResult>(
            IReadOnlyList<int> docIds,
            QueryOptions<T> options = default)
            where TResult : T
        {
            var docAndStableIds = docIds.SelectList(doc => (DocId: doc, StableId: (int)StableIdDocValues.Get(doc)));

            return await client.documentRetrievalQueue.SelectAsync(docAndStableIds, async (doc, i) =>
            {
                TResult entity;
                if (client.ExternalClient is { } externalClient)
                {
                    var externalEntity = await externalClient.GetEntityAsync<T>(new(new EntityMappingKey(StableId: doc.StableId, searchType.TypeId, options.AddressKind), options.Includes ?? searchType.IncludeAll()));

                    entity = (TResult)externalEntity;
                }
                else
                {
                    var sourceField = Reader.Document(doc.DocId).GetField(LuceneConstants.SourceFieldName);
                    var stringValue = sourceField.GetBinaryValue().AsReadOnlyMemory();
                    entity = stringValue.Span.DeserializeEntity<TResult>();
                    //if (Features.UseExternalStorage
                    //    && searchType.HasExternalLink(entity)
                    //    && client.ExternalClient is IExternalRetrievalClient<T> externalClient)
                    //{
                    //    entity = (TResult)await externalClient.Get(new GetExternalArguments<T>(entity, options.Includes ?? searchType.IncludeAll(), options.AddressKind));
                    //}
                }

                if (entity != null)
                {
                    entity.StableId = doc.StableId;
                    entity.DocId = doc.DocId;
                }

                return entity;
            });
        }
    }
}
