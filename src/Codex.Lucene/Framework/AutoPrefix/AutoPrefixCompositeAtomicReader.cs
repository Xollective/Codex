using Codex.Lucene.Framework.AutoPrefix;
using Codex.Sdk;
using DotNext;
using Lucene.Net.Index;

namespace Codex.Lucene;


public class AutoPrefixCompositeAtomicReader(AtomicReader reader) : FilterAtomicReader(reader)
{
    private Fields _fields;
    public override Fields Fields => _fields ??= base.Fields?.FluidSelect(static f => new InnerFields(f));

    private class InnerFields(Fields fields) : FilterFields(fields)
    {
        public override Terms GetTerms(string field)
        {
            return base.GetTerms(field)?.FluidSelect(t => new InnerTerms(t));
        }
    }

    private class InnerTerms(Terms terms) : FilterTerms(terms)
    {
        public override TermsEnum GetEnumerator(TermsEnum reuse)
        {
            reuse = reuse is AutoPrefixMergeTermsEnum outer ? outer.inner : reuse;
            return new AutoPrefixMergeTermsEnum((MultiTermsEnum)base.GetEnumerator(reuse));
        }

        public override TermsEnum GetEnumerator()
        {
            return new AutoPrefixMergeTermsEnum((MultiTermsEnum)base.GetEnumerator());
        }
    }
}