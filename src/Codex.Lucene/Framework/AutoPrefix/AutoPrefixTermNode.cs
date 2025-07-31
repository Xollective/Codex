using Lucene.Net.Util;

namespace Codex.Lucene.Framework.AutoPrefix
{
    public interface INodeValue<T>
        where T : INodeValue<T>
    {
        T CreateNew();

        void Add(T other);

        void Clear();
    }


    public class AutoPrefixTermNode<T>
        where T : INodeValue<T>
    {
        private static readonly BytesRef Empty = new BytesRef();
        public BytesRefString Term = Empty;
        public int NumTerms;
        public int Height;

        public T Value { get; }

        public readonly AutoPrefixTermNode<T> Prior;
        public AutoPrefixTermNode<T> Next;

        public AutoPrefixTermNode(T value, AutoPrefixTermNode<T> prior)
        {
            Prior = prior;
            Height = (prior?.Height ?? 0) + 1;
            Value = value;
        }

        public AutoPrefixTermNode<T> Push(BytesRefString term)
        {
            if (Next != null)
            {
                Next.Reset(term);
            }
            else
            {
                Next = new(Value.CreateNew(), this);
                Next.Term = BytesRef.DeepCopyOf(term);
            }

            return Next;
        }

        public AutoPrefixTermNode<T> Pop()
        {
            Prior.Add(Value);
            return this;
        }

        public void Reset(BytesRefString term)
        {
            if (Term.Value.Bytes.Length < term.Length)
            {
                Term.Value.Bytes = new byte[term.Length * 2];
            }

            Array.Copy(term.Bytes, term.Value.Offset, Term.Value.Bytes, 0, term.Length);
            Term.Value.Length = term.Length;

            Value.Clear();
        }

        internal BytesRefString GetCommonPrefix(BytesRefString text)
        {
            var length = Math.Min(text.Length, Term.Length);
            var commonLength = 0;
            for (commonLength = 0; commonLength < length; commonLength++)
            {
                if (text[commonLength] != Term[commonLength])
                {
                    break;
                }
            }

            if (commonLength == 0) return Empty;

            var bytes = new byte[commonLength];
            Array.Copy(text.Bytes, text.Value.Offset, bytes, 0, commonLength);
            return new BytesRef(bytes);
        }

        public void Add(T value)
        {
            Value.Add(value);
        }
    }
}
