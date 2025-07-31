namespace Codex.Lucene.Framework
{
    // TODO: Use a combination of ngrams and doc values to search terms finding fuzzy match
    // For instance the following searches should find ShortNameQuery:
    // (Results should be ordered by the length of the longest common subsequence
    // [ "ShortQuery", "NameShortQuery", "NameQuery", "Query", "ShrtNameQuery", "ShortNmeQuery" ]
    //public class ShortNameQuery : MultiTermQuery
    //{
    //    public override string ToString(string field)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
