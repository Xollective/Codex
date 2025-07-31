using Lucene.Net.Codecs;
using Lucene.Net.Index;

namespace Codex.Lucene.Framework;

public class StableIdDocValuesRewritingFormat : DocValuesFormat
{
    private readonly DocValuesFormat inner;

    public StableIdDocValuesRewritingFormat(DocValuesFormat inner)
    {
        this.inner = inner;
    }

    public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
    {
        throw new NotImplementedException();
    }

    public override DocValuesProducer FieldsProducer(SegmentReadState state)
    {
        throw new NotImplementedException();
    }
}

