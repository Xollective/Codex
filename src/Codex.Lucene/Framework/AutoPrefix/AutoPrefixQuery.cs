using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Codex.Lucene.Framework.AutoPrefix
{
    public class AutoPrefixQuery : TermQuery
    {
        public AutoPrefixQuery(Term prefix) : base(prefix)
        {
            PrebuildTermContext = false;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.Add(Term);
        }

        public override bool Seek(TermsEnum termsEnum, TermState state)
        {
            var seekStatus = termsEnum.SeekCeil(Term.Bytes);
            if (seekStatus == TermsEnum.SeekStatus.NOT_FOUND)
            {
                // No exact match. Cursor is positioned at next term after
                // the target term
                if (termsEnum.Term.Span.StartsWith(Term.Bytes.Span))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (seekStatus == TermsEnum.SeekStatus.FOUND)
            {
                // Exact match
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
