using Codex.Lucene.Formats;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Search;

public class StableIdFilter(string field, IBitSet filterIds) : Filter
{
    public int FilteredDocs;
    public int MatchingDocs;

    public override FilteredQuery.FilterStrategy FilterStrategy => FilteredQuery.QUERY_FIRST_FILTER_STRATEGY;

    public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
    {
        var stableIdDocValues = context.AtomicReader.GetNumericDocValues(field);
        return new InnerDocIdSet(this, stableIdDocValues, filterIds, context.Reader.MaxDoc, acceptDocs);
    }

    private class InnerDocIdSet : FieldCacheDocIdSet
    {
        private readonly StableIdFilter stableIdFilter;
        private readonly NumericDocValues stableIdDocValues;
        private readonly IBitSet filterIds;

        public InnerDocIdSet(StableIdFilter stableIdFilter, NumericDocValues stableIdDocValues, IBitSet filterIds, int maxDoc, IBits acceptDocs) 
            : base(maxDoc, acceptDocs)
        {
            this.stableIdFilter = stableIdFilter;
            this.stableIdDocValues = stableIdDocValues;
            this.filterIds = filterIds;
        }

        protected override bool MatchDoc(int doc)
        {
            var docId = stableIdDocValues.Get(doc);
            bool matches = filterIds.Get((int)docId);

            ref int counter = ref (matches ? ref stableIdFilter.MatchingDocs : ref stableIdFilter.FilteredDocs);
            Interlocked.Increment(ref counter);

            return matches;
        }
    }
}
