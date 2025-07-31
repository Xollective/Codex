using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.ContractsLight;
using BuildXL.Utilities.Collections;
using Codex.Utilities;
using DotNext;
using DotNext.Collections.Generic;
using DotNext.Runtime.CompilerServices;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Index.Sorter;
using Lucene.Net.Util;
using MessagePack.Formatters;
using static Codex.Utilities.CollectionUtilities;
using static Lucene.Net.Index.FilterAtomicReader;
using static Lucene.Net.Index.MultiTermsEnum;

namespace Codex.Lucene.Framework.AutoPrefix
{
    public class AutoPrefixMergeTermsEnum : FilterTermsEnum
    {
        protected internal readonly MultiTermsEnum inner;

        private PriorityQueue<TermsEnumWithSlice> termsHeap;

        private Stack<PrefixEntry> prefixStack = new Stack<PrefixEntry>();
        private BytesRef prefixTerm = new BytesRef(BytesRef.EMPTY_BYTES);
        private BytesRef lastTerm = new BytesRef(BytesRef.EMPTY_BYTES);
        private int startMatchCount = 0;

        private Comparer<TermsEnumWithSlice> DocOrderComparer = Comparer<TermsEnumWithSlice>.Create((a, b) => a.SubSliceStart.CompareTo(b.SubSliceStart));

        public AutoPrefixMergeTermsEnum(MultiTermsEnum inner)
            : base(inner)
        {
            Contract.Assert(inner.MatchCount == 0);
            this.inner = inner;
            termsHeap = inner.TermQueue;
        }

        public override bool MoveNext()
        {
            if (prefixStack.TryPop(out var entry))
            {
                prefixTerm.Length = entry.PrefixLength;
                SetPrefixTerm(entry.MatchCount);
                if (entry.MatchCount == startMatchCount)
                {
                    startMatchCount = -1;
                }
                return true;
            }
            else if (prefixTerm.Length != 0 && startMatchCount > 0)
            {
                // This is the case where the head term is a prefix of another term but there
                // are no other common prefixes with the head term so no prefixes on the stack.
                ReturnMatches(startMatchCount, prefixTerm.Length);
                startMatchCount = 0;
            }

            if (InnerMoveNext())
            {
                PullTopPrefixesFromTermsHeap();
                return !(Term is null);
            }

            return false;
        }

        protected virtual bool InnerMoveNext()
        {
            if (Term != null)
            {
                lastTerm.CopyBytes(Term);
            }

            return inner.MoveNext();
        }

        private void SetPrefixTerm(int matchCount)
        {
            ReturnMatches(matchCount, maxTermLength: int.MaxValue);
            inner.SetTerm(prefixTerm);
        }

        private void ReturnMatches(int matchCount, int maxTermLength)
        {
            var matches = inner.MatchArray;
            int count = inner.MatchCount;
            if (count != matchCount)
            {
                var prefixLength = prefixTerm.Length;
                int cursor = 0;
                for (int i = 0; i < count; i++, cursor++)
                {
                    var match = matches[i];
                    Contract.Assert(match.Current != null);
                    if (match.PrefixLength < prefixLength || match.Current.Length > maxTermLength)
                    {
                        // Re-add slice now that related prefix has been processed
                        termsHeap.Add(match);
                        match.PrefixLength = int.MaxValue;
                        cursor--;
                    }
                    else if (cursor < i)
                    {
                        matches[cursor] = match;
                    }
                }

                Contract.Assert(cursor == matchCount);
                inner.MatchCount = matchCount;
            }
        }

        private void PullTopPrefixesFromTermsHeap()
        {
            var term = inner.Term;

            int minPrefixLength = GetCommonPrefixLength(lastTerm, term);

            var matches = inner.MatchArray;

            //prefixTerm.Length = 0;
            startMatchCount = inner.MatchCount;
            while (termsHeap.Count > 0 && Out.TrueVar(out var min, termsHeap.Top) && Out.Var(out var commonPrefixLength, GetCommonPrefixLength(min.Current, term)) > minPrefixLength)
            {
                if (startMatchCount == inner.MatchCount)
                {
                    prefixTerm.CopyBytes(term);
                }

                if (commonPrefixLength < term.Length)
                {
                    prefixStack.Push(new(prefixTerm, term.Length, inner.MatchCount));
                    prefixTerm.Length = commonPrefixLength;
                    term = prefixTerm;
                }

                min = termsHeap.Pop();
                min.PrefixLength = commonPrefixLength;
                matches[inner.MatchCount++] = min;
            }

            if (startMatchCount != inner.MatchCount)
            {
                matches.AsSpan(0, inner.MatchCount).Sort(DocOrderComparer);
                inner.SetTerm(term);
            }
        }

        private int GetCommonPrefixLength(BytesRef term1, BytesRef term2)
        {
            return GetCommonPrefixLength(term1.Span, term2.Span);
        }

        private int GetCommonPrefixLength(Span<byte> term1, Span<byte> term2)
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

        public override SeekStatus SeekCeil(BytesRef text)
        {
            throw new NotSupportedException();
        }

        public override void SeekExact(long ord)
        {
            throw new NotSupportedException();
        }

        private record struct PrefixEntry(BytesRef BaseTerm, int PrefixLength, int MatchCount)
        {
            public BytesRefString PrefixTerm_Debug
            {
                get
                {
                    var r = new BytesRef();
                    r.Sync(BaseTerm);
                    r.Length = PrefixLength;
                    return r;
                }
            }
        }
    }
}