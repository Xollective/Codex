using System.Diagnostics;
using System.Net.Sockets;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Codex.Lucene.Framework.AutoPrefix
{
    public class AutoPrefixTermsConsumer : TermsConsumer
    {
        private readonly IOrderingTermStore termStore;
        private readonly TermsConsumer inner;

        public override IComparer<BytesRef> Comparer => inner.Comparer;

        private AutoPrefixTermsBuilder<NodeDocSet> builder;
        internal AutoPrefixTermNode<NodeDocSet> CurrentNode => builder.CurrentNode;

        public AutoPrefixTermsConsumer(TermsConsumer inner, IOrderingTermStore termStore, int docCount, bool validating = false)
        {
            this.inner = inner;
            this.termStore = termStore;
            builder = new(new(docCount, validating), node => termStore.Store(node.Term, node.Value.Docs));
        }

        public override void Merge(MergeState mergeState, IndexOptions indexOptions, TermsEnum termsEnum)
        {
            if (termsEnum != TermsEnum.EMPTY)
            {
                if (termsEnum is MultiTermsEnum mte && mte.ActiveSubReaderSlices.Count > 1)
                {
                    termsEnum = new AutoPrefixMergeTermsEnum(mte);
                }

                inner.Merge(mergeState, indexOptions, termsEnum);
            }
            else
            {
                base.Merge(mergeState, indexOptions, termsEnum);
            }
        }

        public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
        {
            builder.Finish();

            termStore.ForEachTerm(t =>
            {
                var consumer = inner.StartTerm(t.term);

                int count = 0;
                foreach (var doc in t.docs.Enumerate())
                {
                    count++;
                    consumer.StartDoc(doc, -1);
                    consumer.FinishDoc();
                }

                builder.Print($"Finish.FinishTerm {count}", t.term);
                inner.FinishTerm(t.term, new TermStats(count, count));
            });

            inner?.Finish(sumTotalTermFreq, sumDocFreq, docCount);
        }

        public override void FinishTerm(BytesRef text, TermStats stats)
        {
        }

        public override PostingsConsumer StartTerm(BytesRef textRef)
        {
            return builder.StartTerm(textRef).Value;
        }
    }
}
