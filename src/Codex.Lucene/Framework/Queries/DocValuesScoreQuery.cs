using System.Collections.Concurrent;
using Codex.Lucene.Framework.AutoPrefix;
using CommunityToolkit.HighPerformance;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Framework;

public interface IDocValuesScoreState
{
    float GetScore(BytesRefString result);
}

public class DocValuesScoreQuery : CustomScoreQuery
{
    public DocValuesScoreQuery(Query subQuery, IDocValuesScoreState state, string field)
        : base(subQuery)
    {
        State = state;
        Field = field;
    }

    public IDocValuesScoreState State { get; }
    public string Field { get; }

    protected override CustomScoreProvider GetCustomScoreProvider(AtomicReaderContext context)
    {
        return new ScoreProvider(context, this);
    }

    private class ScoreProvider : CustomScoreProvider
    {
        public ScoreProvider(AtomicReaderContext context, DocValuesScoreQuery query)
            : base(context)
        {
            DocValues = context.AtomicReader.GetSortedDocValues(query.Field);
            Query = query;
        }

        private Dictionary<int, float> ordToScoreMap = new();
        public BytesRefString Result = new BytesRefString(new BytesRef());
        public SortedDocValues DocValues { get; }
        public DocValuesScoreQuery Query { get; }

        public override float CustomScore(int doc, float subQueryScore, float valSrcScore)
        {
            if (DocValues != null)
            {
                var score = GetScore(doc);
                var result = score + subQueryScore;
                return result;
            }

            return base.CustomScore(doc, subQueryScore, valSrcScore);
        }

        private float GetScore(int doc)
        {
            var ord = DocValues.GetOrd(doc);
            if (ordToScoreMap.TryGetValue(ord, out var score))
            {
                return score;
            }

            DocValues.LookupOrd(ord, Result);

            score = Query.State.GetScore(Result);
            ordToScoreMap[ord] = score;
            return score;
        }
    }
}