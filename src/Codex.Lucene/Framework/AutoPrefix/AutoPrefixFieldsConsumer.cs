using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Codex.Lucene.Framework.AutoPrefix
{
    public class AutoPrefixFieldsConsumer : FieldsConsumer
    {
        private readonly FieldsConsumer inner;
        private readonly SegmentWriteState state;

        public AutoPrefixFieldsConsumer(SegmentWriteState state, FieldsConsumer inner)
        {
            this.state = state;
            this.inner = inner;
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            var innerTermsConsumer = inner.AddField(field);
            return new AutoPrefixTermsConsumer(innerTermsConsumer, CreateTermStore(field, innerTermsConsumer.Comparer), state.SegmentInfo.DocCount);
        }

        public override void Merge(MergeState mergeState, Fields fields)
        {
            base.Merge(mergeState, fields);
        }

        private IOrderingTermStore CreateTermStore(FieldInfo field, IComparer<BytesRef> comparer)
        {
            return new MemoryOrderingTermStore(comparer);
        }

        protected override void Dispose(bool disposing)
        {
            inner.Dispose();
        }
    }
}
