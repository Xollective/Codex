using Codex.Utilities;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net;

interface IAttributeProvider<T>
{
    T GetAttribute();
}

public static class BinaryListTokenStream
{
    public static BinaryListTokenStream<TBinaryItem> Create<TBinaryItem>(IEnumerable<TBinaryItem> items)
        where TBinaryItem : struct, IBinaryItem<TBinaryItem>
    {
        return new BinaryListTokenStream<TBinaryItem>(items);
    }

    public static BinaryListTokenStream<TBinaryItem> CreateTokenStream<TBinaryItem>(this IEnumerable<TBinaryItem> items)
        where TBinaryItem : struct, IBinaryItem<TBinaryItem>
    {
        return new BinaryListTokenStream<TBinaryItem>(items);
    }
}

public sealed class BinaryListTokenStream<TBinaryItem> : TokenStream,
    IAttributeProvider<ICharTermAttribute>,
    IAttributeProvider<ITermToBytesRefAttribute>,
    IAttributeProvider<IOffsetAttribute>
    where TBinaryItem : struct, IBinaryItem<TBinaryItem>
{
    internal void InitializeInstanceFields()
    {
        //termAttribute = AddAttribute<ICharTermAttribute>();
        binaryTermAttribute = new();
        offsetAttribute = AddAttribute<IOffsetAttribute>();
    }

    internal ITermToBytesRefAttribute termAttribute;
    internal IOffsetAttribute offsetAttribute;
    private BinaryTermToBytesRefAttribute binaryTermAttribute;

    internal IEnumerable<TBinaryItem> value;
    private IEnumerator<TBinaryItem> _enumerator;

    /// <summary>
    /// Creates a new <see cref="TokenStream"/> that returns a <see cref="string"/> as single token.
    /// <para/>Warning: Does not initialize the value, you must call
    /// <see cref="SetValue(string)"/> afterwards!
    /// </summary>
    public BinaryListTokenStream(IEnumerable<TBinaryItem> value)
    {
        InitializeInstanceFields();
        SetValue(value);
    }

    /// <summary>
    /// Sets the string value. </summary>
    internal void SetValue(IEnumerable<TBinaryItem> value)
    {
        this.value = value;
        this._enumerator = value.GetEnumerator();
    }

    public override bool IncrementToken()
    {
        if (!_enumerator.MoveNext())
        {
            return false;
        }

        ClearAttributes();
        var item = _enumerator.Current;
        binaryTermAttribute.Item = item;
        offsetAttribute.SetOffset(item.StartOffset, item.EndOffset);
        return true;
    }

    public override void End()
    {
        base.End();
    }

    public override void Reset()
    {
        _enumerator = value.GetEnumerator();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            value = null;
        }
    }

    public override T GetAttribute<T>()
    {
        if (this is IAttributeProvider<T> provider)
        {
            return provider.GetAttribute();
        }

        return base.GetAttribute<T>();
    }

    ICharTermAttribute IAttributeProvider<ICharTermAttribute>.GetAttribute()
    {
        throw new NotImplementedException();
    }

    ITermToBytesRefAttribute IAttributeProvider<ITermToBytesRefAttribute>.GetAttribute()
    {
        return binaryTermAttribute;
    }

    IOffsetAttribute IAttributeProvider<IOffsetAttribute>.GetAttribute()
    {
        return offsetAttribute;
    }

    private class BinaryTermToBytesRefAttribute : Attribute, ITermToBytesRefAttribute
    {
        public TBinaryItem Item;

        public BytesRef BytesRef { get; private set; } = new BytesRef();

        public override object Clone()
        {
            var clone = (BinaryTermToBytesRefAttribute)base.Clone();
            clone.BytesRef = BytesRef.DeepCopyOf(BytesRef);
            return Clone();
        }

        public override void Clear()
        {
            BytesRef.Length = 0;
        }

        public override void CopyTo(IAttribute target)
        {
            var t = (BinaryTermToBytesRefAttribute)target;
            t.Item = Item;
        }

        public void FillBytesRef()
        {
            BytesRef.Grow(Item.Length);
            BytesRef.Length = Item.Length;
            Item.CopyTo(BytesRef.Span);
        }

    }
}