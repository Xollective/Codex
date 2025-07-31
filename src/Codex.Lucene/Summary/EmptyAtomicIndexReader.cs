using Lucene.Net.Index;
using Lucene.Net.Index.Memory;
using Lucene.Net.Util;

namespace Codex.Lucene;

public class EmptyAtomicIndexReader : FilterAtomicReader
{
    public EmptyAtomicIndexReader(AtomicReader input) : base(input)
    {
    }

    public override Fields Fields => null;

    public override FieldInfos FieldInfos => null;

    public override void Document(int docID, StoredFieldVisitor visitor)
    {
    }

    public override BinaryDocValues GetBinaryDocValues(string field)
    {
        return null;
    }

    public override IBits GetDocsWithField(string field)
    {
        return null;
    }

    public override NumericDocValues GetNormValues(string field)
    {
        return null;
    }

    public override NumericDocValues GetNumericDocValues(string field)
    {
        return null;
    }

    public override SortedDocValues GetSortedDocValues(string field)
    {
        return null;
    }

    public override SortedSetDocValues GetSortedSetDocValues(string field)
    {
        return null;
    }

    public override Fields GetTermVectors(int docID)
    {
        return null;
    }

    protected override void DoClose()
    {
    }
}