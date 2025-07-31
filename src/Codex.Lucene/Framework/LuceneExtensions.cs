using Codex.Lucene.Framework.AutoPrefix;
using Codex.Lucene.Search;
using Codex.Sdk.Search;
using Codex.Utilities;
using J2N.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene
{
    public static class LuceneExtensions
    {
        public static ILuceneIndex AsLucene(this IIndex index) => (ILuceneIndex)index;

        public static bool IsPrefixOf(this BytesRef term1, BytesRef term2)
        {
            return term1.GetCommonPrefixLength(term2) == term1.Length;
        }

        public static int GetCommonPrefixLength(this BytesRef term1, BytesRef term2)
        {
            return GetCommonPrefixLength(term1.Span, term2.Span);
        }

        private static int GetCommonPrefixLength(Span<byte> term1, Span<byte> term2)
        {
            var length = Math.Min(term1.Length, term2.Length);
            for (int i = 0; i < length; i++)
            {
                if (term1[i] != term2[i])
                {
                    return i;
                }
            }

            return length;
        }

        public static IBinarySearchNumericDocValues AsSearchValues(this NumericDocValues values, AtomicReaderContext context)
        {
            return values as IBinarySearchNumericDocValues ?? new SortedNumericBinarySearchDocValues(values, context.MaxDoc);
        }

        public static void Deconstruct(this AtomicReaderContext context, out AtomicReader reader, out AtomicReaderContext slice)
        {
            reader = context.AtomicReader;
            slice = context;
        }

        public static IEnumerable<SegmentReader> GetLeafSegments(this IndexReader reader)
        {
            return reader.Leaves.Select(arc => (SegmentReader)arc.AtomicReader);
        }

        public static SegmentReader GetLeafSegment(this IndexReader reader, int docId, out int localDocId)
        {
            var result = (SegmentReader)reader.GetLeafSegmentContext(docId, out localDocId).Reader;
            return result;
        }

        public static IndexReader GetRootedLeafSegment(this IndexReader reader, int docId)
        {
            var readerIndex = ReaderUtil.SubIndex(docId, reader.Leaves);
            var state = new SegmentBitMap(reader.Leaves.Count).Set(readerIndex);
            return new AppliedExclusionIndexReader(reader, state);
        }

        public static AtomicReaderContext GetLeafSegmentContext(this IndexReader reader, int docId, out int localDocId)
        {
            var readerIndex = ReaderUtil.SubIndex(docId, reader.Leaves);
            var context = reader.Leaves[readerIndex];

            localDocId = docId - context.DocBase;
            return context;
        }

        public static DocsEnum Docs(this TermsEnum termsEnum) => termsEnum.Docs(null, null);

        public static IEnumerable<BytesRefString> Enumerate(this Terms terms, bool includeCurrent = false)
        {
            TermsEnum reuse = default;
            return terms.Enumerate(ref reuse, includeCurrent);
        }

        public static IEnumerable<BytesRefString> Enumerate(this Terms terms, ref TermsEnum reuse, bool includeCurrent = false)
        {
            reuse = terms.GetEnumerator(reuse);
            return Enumerate(reuse, includeCurrent);
        }

        public static IEnumerable<BytesRefString> GetAndEnumerate(this Terms terms, out TermsEnum te, bool includeCurrent = false)
        {
            te = terms.GetEnumerator();
            return Enumerate(te, includeCurrent);
        }

        public static IEnumerable<BytesRefString> Enumerate(this TermsEnum termsEnum, bool includeCurrent = false)
        {
            if (includeCurrent)
            {
                yield return termsEnum.Term;
            }

            while (termsEnum.MoveNext())
            {
                yield return termsEnum.Term;
            }
        }

        public static IEnumerable<string> SelectValues(this IEnumerable<BytesRefString> items) => items.Select(i => i.ToString());
        public static IEnumerable<string> SelectValues(this IEnumerable<BytesRef> items) => items.Select(i => i.Utf8ToString());

        private record SortedNumericBinarySearchDocValues(NumericDocValues DocValues, int MaxDoc) : IBinarySearchNumericDocValues, IReadOnlyArraySlimList<long>
        {
            public long this[int index] => Get(index);

            public int Length => MaxDoc;

            public long[] UnderlyingArrayUnsafe => null;

            public int BinarySearch(long value, int maxDoc)
            {
                return this.BinarySearch<long>(value);
            }

            public IEnumerable<(int DocId, long Value)> Enumerate()
            {
                for (int docId = 0; docId < MaxDoc; docId++)
                {
                    yield return (docId, Get(docId));
                }
            }

            public long Get(int docID)
            {
                return DocValues.Get(docID);
            }
        }
    }
}
