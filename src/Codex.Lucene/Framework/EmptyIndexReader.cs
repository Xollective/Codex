using Codex.Lucene.Search;
using Codex.Sdk.Utilities;
using Codex.Utilities;
using Lucene.Net.Index;
using Lucene.Net.Index.Memory;
using Lucene.Net.Util;

namespace Codex.Lucene;

public class EmptyIndexReader : CompositeReader
{
    public static EmptyIndexReader Instance { get; } = new();

    public EmptyIndexReader()
    {
    }

    public override int NumDocs => 0;

    public override int MaxDoc => 0;

    public override int DocFreq(Term term)
    {
        return 0;
    }

    public override void Document(int docID, StoredFieldVisitor visitor)
    {
    }

    public override int GetDocCount(string field) => 0;

    public override long GetSumDocFreq(string field) => 0;

    public override long GetSumTotalTermFreq(string field) => 0;

    public override Fields GetTermVectors(int docID) => null;

    public override long TotalTermFreq(Term term) => 0;

    protected override void DoClose()
    {
    }

    protected override IList<IndexReader> GetSequentialSubReaders()
    {
        return Array.Empty<IndexReader>();
    }
}