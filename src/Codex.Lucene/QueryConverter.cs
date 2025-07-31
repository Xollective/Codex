using Codex.Sdk.Search;
using Lucene.Net.Queries;
using Lucene.Net.QueryParsers.Simple;
using Lucene.Net.Search;

namespace Codex.Lucene.Search;

public record QueryState<T>(RewriteQuery<T> Rewrite = null, bool IsNegated = false)
{
    public static readonly QueryState<T> Default = new QueryState<T>();

}

public delegate Query RewriteQuery<T>(
    Query luceneQuery,
    CodexQuery<T> codexQuery,
    QueryState<T> state);

public interface IQueryRewriter
{
    Query Rewrite<T>(Query luceneQuery, CodexQuery<T> codexQuery, QueryState<T> state);
}

public class QueryConverter
{
    public static Query FromCodexQuery<T>(CodexQuery<T> query)
    {
        return FromCodexQuery(query, QueryState<T>.Default);
    }

    public static Query FromCodexQuery<T>(CodexQuery<T> query, QueryState<T> state)
    {
        if (query == null) return null;
        var luceneQuery = getQuery();
        if (query.BoostValue != null)
        {
            luceneQuery.Boost = query.BoostValue.Value;
        }

        luceneQuery = state.Rewrite?.Invoke(luceneQuery, query, state) ?? luceneQuery;

        return luceneQuery;

        Query getQuery()
        {
            const bool flattenQueries = true;
            switch (query.Kind)
            {
                case CodexQueryKind.Boosting:
                    {
                        var bq = (BoostingCodexQuery<T>)query;
                        var innerQuery = FromCodexQuery(bq.InnerQuery, state);
                        var boostQuery = FromCodexQuery(bq.BoostQuery, state);
                        if (boostQuery == null)
                        {
                            return innerQuery;
                        }

                        if (innerQuery is not BoostingQuery)
                        {
                            //innerQuery = new ConstantScoreQuery(innerQuery);
                        }

                        return new BoostingQuery(match: innerQuery, context: boostQuery, boost: bq.BoostValue ?? LuceneConstants.Boosts.DefaultExplicit);
                    }
                case CodexQueryKind.And:
                case CodexQueryKind.Or:
                    {
                        var bq = new BooleanQuery();
                        var clauseSet = new HashSet<BooleanClause>();
                        var binaryQuery = (BinaryCodexQuery<T>)query;

                        addClauses(binaryQuery.LeftQuery);
                        addClauses(binaryQuery.RightQuery);

                        void addClauses(CodexQuery<T> q)
                        {
                            Query lq = FromCodexQuery(q, state);
                            if (lq == null) return;

                            if (flattenQueries
                                && (q.Kind == query.Kind || (q.Kind == CodexQueryKind.Negate && query.Kind == CodexQueryKind.And))
                                && lq is BooleanQuery boolQuery)
                            {
                                foreach (var clause in boolQuery.Clauses)
                                {
                                    // Only include MUST_NOT clauses from Negate queries
                                    if (q.Kind != CodexQueryKind.Negate || clause.Occur == Occur.MUST_NOT)
                                    {
                                        if (clauseSet.Add(clause))
                                        {
                                            bq.Add(clause);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                bq.Add(lq, query.Kind == CodexQueryKind.And ? Occur.MUST : Occur.SHOULD);
                            }
                        }
                        return bq;
                    }
                case CodexQueryKind.Term:
                    var tq = (ITermQuery)query;
                    return tq.CreateQuery(QueryFactory.Instance);
                case CodexQueryKind.Negate:
                    {
                        var nq = (NegateCodexQuery<T>)query;
                        var bq = new BooleanQuery();
                        bq.Add(FromCodexQuery(nq.InnerQuery, state with { IsNegated = !state.IsNegated }), Occur.MUST_NOT);
                        bq.Add(new MatchAllDocsQuery(), Occur.MUST);
                        return bq;
                    }
                case CodexQueryKind.None:
                    {
                        return new BooleanQuery();
                    }
                case CodexQueryKind.All:
                    {
                        return new MatchAllDocsQuery();
                    }
                case CodexQueryKind.MatchPhrase:
                    var mq = (MatchPhraseCodexQuery<T>)query;

                    var fieldName = mq.Field.Name;
                    var simpleQueryParser = new SimpleQueryParser(
                        LuceneConstants.StandardAnalyzer,
                        fieldName);

                    string phrase = mq.Phrase;
                    if (mq.MaxExpansions > 0)
                    {
                        phrase += "*";
                    }

                    var parsedQuery = simpleQueryParser.CreatePhraseQuery(fieldName, phrase);
                    return parsedQuery;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}