using Codex.Utilities;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Framework
{
    public static class Helpers
    {
        public static FixedBitSet<TBits> AsFixedBitSet<TBits>(this TBits bits, int? cardinality = null)
            where TBits : struct, IMemory<long>
        {
            return new FixedBitSet<TBits>(bits, bits.Length * 64) { Count = cardinality };
        }

        public static IFixedBitSet CreateFixedBitSet(int numBits)
        {
            var bits = StructArray.Create(new long[FixedBitSet.Bits2words(numBits)]);
            return bits.AsFixedBitSet();
        }

        public static ReadOnlyMemory<byte> AsReadOnlyMemory(this BytesRef bytes)
        {
            return bytes.Bytes.AsMemory(bytes.Offset, bytes.Length);
        }

        public static IEnumerable<int> Enumerate(this DocIdSet docs)
        {
            var iterator = docs.GetIterator();
            return iterator.Enumerate();
        }

        public static IEnumerable<int> Enumerate(this DocIdSetIterator iterator)
        {
            while (true)
            {
                var doc = iterator.NextDoc();
                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    yield break;
                }

                yield return doc;
            }
        }
    }
}
