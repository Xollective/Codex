using Codex.Lucene.Formats;
using Codex.Lucene.Utilities;
using Codex.Utilities;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using IBitSet = Codex.Lucene.Utilities.IBitSet;

namespace Codex.Lucene.Search;

public class StableIdTermFilter : Filter
{
    private string field;
    private int stableId;

    public override FilteredQuery.FilterStrategy FilterStrategy => FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY;

    public StableIdTermFilter(string field, int stableId)
    {
        this.field = field;
        this.stableId = stableId;
    }

    public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
    {
        var stableIdDocValues = context.AtomicReader.GetNumericDocValues(field);
        if (stableIdDocValues is INumericDocValuesRange range)
        {
            if (!(stableId >= range.MinValue && stableId <= range.MaxValue))
            {
                // No matches since stable id is outside range
                return null;
            }
        }

        IBinarySearchNumericDocValues binarySearch = stableIdDocValues.AsSearchValues(context);

        var docId = binarySearch.BinarySearch(stableId, context.MaxDoc);
        if (docId < 0)
        {
            return null;
        }

        return new BitSetDocIdSet(new SingleDocBitSet(docId));
    }

    private record SingleDocBitSet(int DocId) : IBitSet
    {
        public int Length => DocId + 1;

        public bool Get(int index)
        {
            return index == DocId;
        }

        public int NextSetBit(int minValue)
        {
            if (minValue <= DocId) return DocId;
            else return -1;
        }
    }
}
