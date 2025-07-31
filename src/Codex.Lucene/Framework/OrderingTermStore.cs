using Codex.Lucene.Framework.AutoPrefix;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Framework
{
    public interface IOrderingTermStore
    {
        void ForEachTerm(Action<(BytesRef term, DocIdSet docs)> action);
        void Store(BytesRef term, DocIdSet docs);
    }

    public class MemoryOrderingTermStore : IOrderingTermStore
    {
        private readonly SortedDictionary<BytesRefString, DocIdSet> _sortedTerms;

        public MemoryOrderingTermStore(IComparer<BytesRef> comparer)
        {
            _sortedTerms = new SortedDictionary<BytesRefString, DocIdSet>(Comparer<BytesRefString>.Create((b1, b2) => comparer.Compare(b1.Value, b2.Value)));
        }

        public void ForEachTerm(Action<(BytesRef term, DocIdSet docs)> action)
        {
            foreach (var item in _sortedTerms)
            {
                action((item.Key, item.Value));
            }
        }

        public void Store(BytesRef term, DocIdSet docs)
        {
            _sortedTerms.Add(BytesRef.DeepCopyOf(term), new PForDeltaDocIdSet.Builder().Add(docs.GetIterator()).Build());
        }
    }
}
