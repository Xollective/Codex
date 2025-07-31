using Codex.Lucene.Search;
using Codex.Sdk.Utilities;
using Codex.Utilities;
using Lucene.Net.Index;
using Lucene.Net.Index.Memory;
using Lucene.Net.Util;

namespace Codex.Lucene;

public class AppliedExclusionIndexReader : CompositeReader
{
    private IndexReader inner;
    private SummaryQueryState state;

    public AppliedExclusionIndexReader(IndexReader inner, SummaryQueryState state)
    {
        this.inner = inner;
        this.state = state;
    }

    public override int NumDocs => inner.NumDocs;

    public override int MaxDoc => inner.MaxDoc;

    public override int DocFreq(Term term)
    {
        return inner.DocFreq(term);
    }

    public override void Document(int docID, StoredFieldVisitor visitor)
    {
        inner.Document(docID, visitor);
    }

    public override int GetDocCount(string field)
    {
        return inner.GetDocCount(field);
    }

    public override long GetSumDocFreq(string field)
    {
        return inner.GetSumDocFreq(field);
    }

    public override long GetSumTotalTermFreq(string field)
    {
        return inner.GetSumTotalTermFreq(field);
    }

    public override Fields GetTermVectors(int docID)
    {
        return inner.GetTermVectors(docID);
    }

    public override long TotalTermFreq(Term term)
    {
        return inner.TotalTermFreq(term);
    }

    protected override void DoClose()
    {
    }

    protected override IList<IndexReader> GetSequentialSubReaders()
    {
        var readers = inner.Leaves.Select(a => a.Reader).ToArray();
        for (int i = 0; i < readers.Length; i++)
        {
            if (state.IsExcluded(i))
            {
                var reader = readers[i];
                readers[i] = new EmptyAtomicIndexReader((AtomicReader)reader);
            }
        }

        return readers;
    }
}