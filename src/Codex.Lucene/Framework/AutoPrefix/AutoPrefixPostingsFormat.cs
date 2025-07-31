using Lucene.Net.Codecs;
using Lucene.Net.Index;

namespace Codex.Lucene.Framework.AutoPrefix
{
    [PostingsFormatName("Lucene41AutoPrefix")]
    public class AutoPrefixPostingsFormat : PostingsFormat
    {
        private readonly PostingsFormat inner;

        public AutoPrefixPostingsFormat()
        {
            this.inner = PostingsFormat.ForName("Lucene41");
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new AutoPrefixFieldsConsumer(state, inner.FieldsConsumer(state));
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            Placeholder.Todo("Need some special logic to only return full terms for sake of merge");
            return inner.FieldsProducer(state);
        }
    }

}
