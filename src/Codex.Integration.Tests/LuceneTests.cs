using System.Collections.Immutable;
using Codex.Integration.Tests;
using Codex.Lucene;
using Codex.Lucene.Framework;
using Codex.Lucene.Framework.AutoPrefix;
using Codex.Lucene.Search;
using Codex.ObjectModel;
using Codex.ObjectModel.Implementation;
using Codex.Sdk.Search;
using Codex.Utilities;
using Codex.View;
using Codex.Workspaces;
using CommunityToolkit.HighPerformance;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Xunit.Abstractions;
using static Lucene.Net.Index.MultiTermsEnum;

namespace Codex.Integration.Tests.Lucene;

using D = SearchMappings.Definition;

public partial record LuceneTests(ITestOutputHelper Output) : CodexTestBase(Output)
{
    private const string TestData = """
        ^account
        ^account$
        ^accountindex
        ^accountindex$
        ^accountlabel
        ^accountlabel$
        ^accountlabeling$
        ^accountlabelresponse
        ^accountlabelresponse$
        ^accountlabelresponse.cs$
        ^accountresponse
        ^accountresponse$
        ^accountresponse.cs$
        """;


    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(2, 1)]
    [InlineData(3)]
    public void TestAutoPrefixMerge(int index, int dataIndex = 0)
    {

        var result = index switch
        {
            0 => GetMergeAutoPrefixTerms(
            B("b"), B("brazen"), B("mapplet"),
            B("create"), B("merit"), B("maple"),
            B("mapplication"),
            B("mazure"), B("cat"), B("nope"), B("meritocracy"), B("mapplet")),

            1 => GetMergeAutoPrefixTerms(
            B("applet", "basic"), B("applet"), B("apple", "base"),
            B("application", "bas", "bass"), B("azure")),

            2 => GetMergeAutoPrefixTerms(
                AutoPrefixMergeData[dataIndex].Split("#", StringSplitOptions.RemoveEmptyEntries).Select(s => B(s.GetLines().Select(t => t.Trim()).Where(t => !t.EndsWith(":"))
                .WhereNotNullOrEmpty().Select(t => (BytesRefString)t).ToArray())).ToArray()),

            3 => GetMergeAutoPrefixTerms(
                B(TestData.GetLines().Select(t => t.Trim()).Select(t => (BytesRefString)t).ToArray())),

            _ => default
        };
        
    }

    

    public BytesRefString[] B(params BytesRefString[] array) => array;

    private class Node
    {
        public Node Parent { get; set; }
        public List<Node> Children = new List<Node>();
        public ImmutableHashSet<int> Docs = ImmutableHashSet<int>.Empty;

    }

    public (BytesRefString, IntList)[] GetMergeAutoPrefixTerms(params BytesRefString[][] termLists)
    {
        //ConcurrentBigMap<BytesRefString, Node> prefixMap = new();
        //foreach ((var termList, var docId) in termLists.WithIndices())
        //{
        //    foreach (var term in termList)
        //    {
        //        Node parent = null;
        //        for (int i = 1; i <= term.Length; i++)
        //        {
        //            var prefix = new BytesRef();
        //            prefix.CopyBytes(term, i);
        //            var node = prefixMap.GetOrAdd(prefix, parent, static (_, parent) => new(parent)).Item.Value;
        //            node.Docs.Add(docId);
        //            node.IsReal = true;
        //        }
        //    }
        //}

        //var expectedTerms = prefixMap.Where(e => e.Value.Count > 1).OrderBy(e => e.Key).ToArray();
        //var expectedIterator = expectedTerms.GetIterator(moveNext: false);

        MultiTermsEnum createMultiTerms()
        {
            var termsEnumsIndex = termLists.Select((t, i) => new TermsEnumIndex(
                new TestTermsEnum() { Terms = t, Id = i + 1 }, i)).ToArray();

            return new MultiTermsEnum(termLists.Select((t, i) => new ReaderSlice(i, 1, 0)).ToArray()).With(m => m.Reset(termsEnumsIndex));
        }

        TermsEnum createMergeTerms()
        {
            return new AutoPrefixMergeTermsEnum(createMultiTerms());
        }


        void validate(TermsEnum input)
        {
            var mergeTermsOutput = SetTermsEnum.Create(createMergeTerms());

            var validator = new AutoPrefixTermsValidator(input, mergeTermsOutput, mergeTermsOutput.Count, termLists.Length);

            validator.OnError += error =>
            {
                Assert.Fail(error.ToString());
            };

            validator.Run().Should().BeTrue();
        }

        validate(createMultiTerms());
        validate(SetTermsEnum.Create(createMergeTerms()));



        var multiTerms = createMergeTerms();
        BytesRef lastTerm = null;

        IEnumerable<(BytesRefString term, IntList docs)> enumerate()
        {
            while (multiTerms.MoveNext())
            {
                BytesRefString term = multiTerms.Term;
                //expectedIterator.TryMoveNext(out var expectedEntry).Should().BeTrue();

                //term.Should().Be(expectedEntry.Key);
                if (lastTerm != null)
                {
                    term.Should<BytesRefString>().BeGreaterThan(lastTerm);
                }

                var values = multiTerms.Docs(null, null).Enumerate().ToArray();
                var expectedValues = termLists.WithIndices().Where(t => t.Item.Any(b => b.Value.StartsWith(term)))
                    .Select(t => t.Index).Distinct().Order().ToArray();

                values.SequenceEqual(expectedValues).Should().BeTrue();
                var copyTerm = new BytesRef();
                copyTerm.CopyBytes(term);
                lastTerm = copyTerm;
                yield return (copyTerm, new IntList(termLists, values));
            }
        }

        var result = enumerate().ToArray();

        result.Select(r => r.term).Should().BeInAscendingOrder();

        result.Should().AllSatisfy(r => r.docs.Values.Should().BeInAscendingOrder());

        return result;
    }

    public record IntList(BytesRefString[][] terms, int[] Values)
    {
        public override string ToString()
        {
            return $"{{{string.Join(", ", Values.Select((v, i) => $"{v}:{terms[v][0]}"))}}}";
        }
    }

    private class TestTermsEnum : TermsEnum
    {
        public BytesRefString[] Terms { get; set; }

        public int Id { get; set; }

        private int _index = -1;

        public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

        private BytesRef _bufferTerm = new BytesRef();

        public override BytesRef Term => unchecked((uint)_index < (uint)Terms.Length) ? _bufferTerm : null;

        public override long Ord => _index;

        public override int DocFreq => throw new NotImplementedException();

        public override long TotalTermFreq => throw new NotImplementedException();

        public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
        {
            return new TestDocs()
            {
                Ids = new[] { 0 }
            };
        }

        public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool MoveNext()
        {
            _index++;
            if (_index < Terms.Length)
            {
                var term = Terms[_index];
                _bufferTerm.Grow(term.Length);
                _bufferTerm.CopyBytes(term);
                return true;
            }

            return false;
        }

        public override SeekStatus SeekCeil(BytesRef text)
        {
            throw new NotImplementedException();
        }

        public override void SeekExact(long ord)
        {
            throw new NotImplementedException();
        }

        private class TestDocs : DocsEnum
        {
            private int _index = -1;
            public int[] Ids { get; set; }

            public override int Freq => 1;

            public override int DocID => Ids[_index];

            public override int Advance(int target)
            {
                throw new NotImplementedException();
            }

            public override long GetCost()
            {
                return Ids.Length;
            }

            public override int NextDoc()
            {
                _index++;
                return (_index < Ids.Length) ? Ids[_index] : NO_MORE_DOCS;
            }
        }
    }


    [Fact]
    public void TestMerger()
    {
        var merger = new TestIndexMerger();

        var b600 = TestIndexMerger.GetBucket(600);
        var b520 = TestIndexMerger.GetBucket(520);

        var targetIndex = new TestIndex(
            1_000,
            1_500,
            1_200,
            600);

        var sourceIndex = new TestIndex(1, 2);
        var result = merger.MergeIndices("test1", sourceIndex, targetIndex, targetIndex);

        result.AllMergedSegments.Length.Should().Be(sourceIndex.Segments.Count);
        result.AllMergedSegments.Should().AllSatisfy(s => s.Origin.Should().Be(sourceIndex));

        sourceIndex = new TestIndex(520);
        result = merger.MergeIndices("test2", sourceIndex, targetIndex, targetIndex);

        result.AllMergedSegments.Length.Should().Be(sourceIndex.Segments.Count + 1);
        result.AllMergedSegments.Where(s => s.Origin == sourceIndex).Distinct().Count()
            .Should().Be(sourceIndex.Segments.Count);
        result.AllMergedSegments.Where(s => s.Origin != sourceIndex).Count()
            .Should().Be(1);
        result.AllMergedSegments.Where(s => s.Origin != sourceIndex).First().SizeMb.Should().Be(600);

        targetIndex = new TestIndex(100, 100, 100, 100);
        sourceIndex = new TestIndex(520);
        result = merger.MergeIndices("test3", sourceIndex, targetIndex, targetIndex);

        result.AllMergedSegments.Length.Should().Be(sourceIndex.Segments.Count);

        targetIndex = new TestIndex(4000, 4000, 4000);
        sourceIndex = new TestIndex(520);
        result = merger.MergeIndices("test3", sourceIndex, targetIndex, targetIndex);

        result.AllMergedSegments.Length.Should().Be(sourceIndex.Segments.Count);
    }

    [Fact]
    public void TestMaxMergeBucket()
    {
        var merger = new TestIndexMerger();
        merger.MaxMergeableBucket = 3;

        var b600 = TestIndexMerger.GetBucket(1000);
        var b520 = TestIndexMerger.GetBucket(1200);

        var targetIndex = new TestIndex(1200, 1200, 1200);
        var sourceIndex = new TestIndex(520, 1200);
        var result = merger.MergeIndices("test3", sourceIndex, targetIndex, targetIndex);

        result.AllMergedSegments.Length.Should().Be(sourceIndex.Segments.Count);
        result.AllMergedSegments.Should().AllSatisfy(t => t.Origin.Should().Be(sourceIndex));
    }

    private record TestIndexMerger : IndexMerger<TestIndex, TestIndex, TestSegment, TestIndexMerger>,
        IndexMergerOperations<TestIndex, TestIndex, TestSegment, TestIndexMerger>
    {
        protected override TestIndex AddIndexes(TestIndex targetWriter, IndexData mergeData)
        {
            var segments = mergeData.MergedSegments.Segments.Keys.ToArray();

            return targetWriter with
            {
                Segments = targetWriter.Segments.AddRange(segments.ToDictionary(s => s.Id))
            };
        }

        public static (long Size, int FileCount) GetFileInfo(TestSegment segment)
        {
            return ((long)(segment.SizeMb * CodexConstants.BytesInMb), segment.FileCount);
        }

        public static IEnumerable<TestSegment> GetLeafSegments(TestIndex reader)
        {
            return reader.Segments.Values;
        }

        public static long GetMergePriority(TestSegment segment)
        {
            return segment.Id;
        }

        public static string Name(TestSegment segment)
        {
            return $"S_{segment.Id}";
        }
    }

    private record TestIndex
    {
        public ImmutableDictionary<int, TestSegment> Segments { get; init; }

        public TestIndex(params long[] segmentSizes)
        {
            Segments = segmentSizes
                .Select(size => new TestSegment(this, size))
                .ToImmutableDictionary(s => s.Id);
        }
    }

    private record struct TestSegment(TestIndex Origin, double SizeMb, int FileCount = 1)
    {
        private static int _nextId;
        public int Id { get; } = Interlocked.Increment(ref _nextId);
    }

    [Fact]
    public async Task TestEmptyCodex()
    {
        var indexDirectory = GetTestOutputDirectory(clean: true);

        var codex = new LuceneCodex(new LuceneConfiguration(indexDirectory));

        var store = new LuceneCodexStore(new LuceneWriteConfiguration(indexDirectory));

        await store.InitializeAsync();

        await store.FinalizeAsync();

        var result = await codex.SearchAsync(new SearchArguments()
        {
            SearchString = "hello"
        });

        Assert.Null(result.Error);
    }

    [Fact]
    public async Task TestSummaryIndex()
    {
        var indexDirectory = GetTestOutputDirectory();
        System.IO.Directory.Delete(indexDirectory, recursive: true);
        var fsDir = FSDirectory.Open(indexDirectory);

        var config = new LuceneConfiguration(indexDirectory);
        // Create an instance of the IndexWriter
        var list = new List<DefinitionSymbol>();
        using (var writer = config.CreateWriter(SearchTypes.Definition))
        {
            writer.Add(new DefinitionSymbol() { ProjectId = "testproject1", Id = "apple".AsId(), ShortName = "red" }).AddTo(list);
            writer.Add(new DefinitionSymbol() { ProjectId = "testproject2", Id = "banana".AsId(), ShortName = "yellow" }).AddTo(list);
            writer.Add(new DefinitionSymbol() { ProjectId = "otherproject", Id = "apple".AsId(), ShortName = "green" }).AddTo(list);
        }

        SummaryIndexReader.Update(config, SearchTypes.Definition);

        var summaryReader = new SummaryIndexReader(config, SearchTypes.Definition);

        summaryReader.Search(b => b.Term(D.ShortName, "^gre")).AssertHitCount(1);

        summaryReader.Search(b => b.Term(D.ShortName, "^gre") | b.Term(D.ShortName, "^red")).AssertHitCount(2);

        summaryReader.Search(b => b.Term(D.ShortName, "^gre") | b.Term(D.ShortName, "^yel")).AssertHitCount(2);

        summaryReader.Search(b => b.Term(D.ProjectId, "test")).AssertHitCount(0);

        summaryReader.Search(b => !b.Term(D.ProjectId, "testproject1")).AssertHitCount(2);

        summaryReader.Search(b => !b.Term(D.ProjectId, "testproject")).AssertHitCount(3);
    }
}

public static class LuceneTestExtensions
{
    public static void ValidateAutoPrefix(this Terms terms, int maxDoc)
    {
        var input = terms.GetEnumerator();
        var mergeTermsOutput = terms.GetEnumerator();
        var validator = new AutoPrefixTermsValidator(input, mergeTermsOutput, (int)terms.Count, MaxDoc: maxDoc);

        validator.OnError += error =>
        {
            Assert.Fail(error.ToString());
        };

        validator.Run().Should().BeTrue();
    }

    public static DefinitionSymbol Add(this IndexWriter writer, DefinitionSymbol definition, bool commit = true)
    {
        var value = new DefinitionSearchModel()
        {
            Definition = definition
        };

        var searchType = SearchTypes.Definition;
        var document = new Document();
        var documentVisitor = new DocumentVisitor(document);

        searchType.VisitFields(value, documentVisitor);

        writer.AddDocument(document);

        if (commit)
        {
            writer.Commit();
        }

        return definition;

    }

    public static DefinitionSymbol AddSimpleDef(this IndexWriter writer, string shortName, bool commit = true)
    {
        return writer.Add(new DefinitionSymbol() { ShortName = shortName }, commit);
    }

    public static AtomicReader AsAtomic(this IndexReader reader) => SlowCompositeReaderWrapper.Wrap(reader, forceSliceAware: true);
    public static DirectoryReader AsDirectoryReader(this IndexReader reader) => (DirectoryReader)reader;

    public static IndexSearcher Newdarcher(this IndexReader reader) => new IndexSearcher(reader);

    public static IndexSearcher NewSearcher(this IndexReaderContext reader) => new IndexSearcher(reader);

    public static bool GoToExact(this TermsEnum te, BytesRefString b) => te.SeekExact(b);

    public static bool GoToCeil(this TermsEnum te, BytesRefString b) => te.SeekCeil(b) != TermsEnum.SeekStatus.END;

    public static bool GoToPrefix(this TermsEnum te, BytesRefString b) => te.SeekCeil(b) != TermsEnum.SeekStatus.END && te.Term.StartsWith(b);

    public static IndexReader GetReader(this IIndex index) => ((ILuceneIndex)index).Reader;

    public static IndexSearcher GetSearcher(this IIndex index) => ((ILuceneIndex)index).Searcher;

    public static BytesRefString AsBytesRef(this string s) => s;

    public static SymbolId AsId(this string value)
    {
        return SymbolId.UnsafeCreateWithValue(value);
    }

    public static DefinitionSymbol AddTo(this DefinitionSymbol value, List<DefinitionSymbol> list)
    {
        list.Add(value);
        return value;
    }

    public static TopDocs Search(this SummaryIndexReader reader, Func<CodexQueryBuilder<IDefinitionSearchModel>, CodexQuery<IDefinitionSearchModel>> createQuery)
    {
        var builder = new CodexQueryBuilder<IDefinitionSearchModel>();
        var query = createQuery(builder);
        var luceneQuery = reader.FromCodexQuery(query);
        var docs = reader.MainSearcher.Search(luceneQuery, 1000);
        return docs;
    }

    public static TopDocs AssertHitCount(this TopDocs docs, int hitCount = 0)
    {
        Assert.Equal(docs.TotalHits, hitCount);
        return docs;
    }
}