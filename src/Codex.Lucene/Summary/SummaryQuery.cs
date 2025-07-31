using System.Collections;
using Codex.ObjectModel.Attributes;
using Codex.Utilities;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using static Codex.Lucene.Search.LuceneStoreFilterBuilder;

namespace Codex.Lucene.Search;

public class SummaryQuery : Query
{
    public Query InnerQuery;
    public SummaryQueryState State;

    public SummaryQuery(Query innerQuery, SummaryQueryState state)
    {
        InnerQuery = innerQuery;
        State = state;
    }

    public override Weight CreateWeight(IndexSearcher searcher)
    {
        return new SummaryWeight(this, InnerQuery.CreateWeight(searcher));
    }

    public override Query Rewrite(IndexReader reader)
    {
        reader = State.InnerCanRewrite
            ? new AppliedExclusionIndexReader(reader, State)
            : null;

        var innerRewrite = InnerQuery.Rewrite(reader);
        if (InnerQuery != innerRewrite)
        {
            return new SummaryQuery(innerRewrite, State);
        }

        return this;
    }

    public override string ToString(string field)
    {
        return $"Summary of {InnerQuery.ToString(field)}";
    }

    public override void ExtractTerms(ISet<Term> terms)
    {
        InnerQuery.ExtractTerms(terms);
    }

    public class SummaryWeight : Weight
    {
        public override SummaryQuery Query { get; }
        private Weight InnerWeight { get; }

        public SummaryWeight(SummaryQuery query, Weight innerWeight)
        {
            Query = query;
            InnerWeight = innerWeight;
        }

        public override Explanation Explain(AtomicReaderContext context, int doc)
        {
            return InnerWeight.Explain(context, doc);
        }

        public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
        {
            if (Query.State.IsExcluded(context))
            {
                return null;
            }

            return InnerWeight.GetScorer(context, acceptDocs);
        }

        public override BulkScorer GetBulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, IBits acceptDocs)
        {
            if (Query.State.IsExcluded(context))
            {
                return null;
            }

            return InnerWeight.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
        }

        public override float GetValueForNormalization()
        {
            return InnerWeight.GetValueForNormalization();
        }

        public override void Normalize(float norm, float topLevelBoost)
        {
            InnerWeight.Normalize(norm, topLevelBoost);
        }
    }
}

public record struct SummaryQueryState(SegmentBitMap Matches, bool InnerCanRewrite)
{
    public bool IsExcluded(AtomicReaderContext context)
    {
        return IsExcluded(context.Ord);
    }

    public bool IsExcluded(int readerOrd)
    {
        return !Matches.Get(readerOrd);
    }

    public static implicit operator SummaryQueryState(SegmentBitMap matches) => new(matches, false);
}
