using System.Collections.Immutable;
using System.Diagnostics.ContractsLight;
using Codex.Lucene.Framework.AutoPrefix;
using Codex.Utilities;
using J2N.Numerics;
using Lucene.Net.Index;
using Lucene.Net.Index.Memory;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Framework;

public class SetTermsEnum(ImmutableSortedSet<BytesRefString> terms, IReadOnlyList<DocIdSet> docs) : TermsEnum
{
    internal readonly BytesRef br = new BytesRef();
    internal int termUpto = -1;
    internal int lastTermUpTo = -2;

    public int Count => terms.Count;

    public IEnumerable<int> CurrentDocs => docs[termUpto].Enumerate();

    public static SetTermsEnum Create(TermsEnum te, bool includeDocs = true)
    {
        var terms = ImmutableSortedSet.CreateBuilder<BytesRefString>();
        var docs = new List<DocIdSet>();
        DocsEnum docsReuse = null;
        foreach (var term in te.Enumerate())
        {
            var copy = new BytesRef();
            copy.CopyBytes(term);
            terms.Add(copy);
            if (includeDocs)
            {
                docs.Add(new PForDeltaDocIdSet.Builder().Add((docsReuse = te.Docs(null, docsReuse))).Build());
            }
        }

        return new(terms.ToImmutable(), docs);
    }

    public override bool SeekExact(BytesRef text)
    {
        termUpto = terms.IndexOf(text);
        return termUpto >= 0;
    }

    public override SeekStatus SeekCeil(BytesRef text)
    {
        termUpto = terms.IndexOf(text);
        if (termUpto < 0) // not found; choose successor
        {
            termUpto = -termUpto - 1;
            if (termUpto >= terms.Count)
            {
                return SeekStatus.END;
            }
            else
            {
                UpdateTerm();
                return SeekStatus.NOT_FOUND;
            }
        }
        else
        {
            UpdateTerm();
            return SeekStatus.FOUND;
        }
    }

    private BytesRef UpdateTerm()
    {
        if (lastTermUpTo != termUpto)
        {
            lastTermUpTo = termUpto;

            if ((uint)termUpto <= (uint)terms.Count)
            {
                br.CopyBytes(terms[termUpto]);
            }
            else
            {
                return null;
            }
        }

        return br;
    }

    public override void SeekExact(long ord)
    {
        Contract.Assert(ord < terms.Count);
        termUpto = (int)ord;
        UpdateTerm();
    }

    public override bool MoveNext()
    {
        termUpto++;
        if (termUpto >= terms.Count)
        {
            return false;
        }
        else
        {
            UpdateTerm();
            return true;
        }
    }

    public override BytesRef Term => UpdateTerm();

    public override long Ord => termUpto;

    public override int DocFreq => 1;

    public override long TotalTermFreq => 1;

    public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
    {
        if (reuse is null || !(reuse is MemoryDocsEnum toReuse))
            toReuse = new MemoryDocsEnum();

        return toReuse.Reset(docs[termUpto].GetIterator());
    }

    public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
    {
        throw new NotSupportedException();
    }

    public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

    public override void SeekExact(BytesRef term, TermState state)
    {
        this.SeekExact(((OrdTermState)state).Ord);
    }

    public override TermState GetTermState()
    {
        OrdTermState ts = new OrdTermState();
        ts.Ord = termUpto;
        return ts;
    }

    private class MemoryDocsEnum() : DocsEnum
    {
        private DocIdSetIterator ids;

        public MemoryDocsEnum Reset(DocIdSetIterator ids)
        {
            this.ids = ids;
            return this;
        }

        public override int Freq => 1;

        public override int DocID => ids.DocID;

        public override int Advance(int target)
        {
            return ids.Advance(target);
        }

        public override long GetCost()
        {
            return ids.GetCost();
        }

        public override int NextDoc()
        {
            return ids.NextDoc();
        }
    }
}
