using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codex.Utilities
{
    public interface IListWrapper<TList, T> : IReadOnlyList<T>
        where TList : IReadOnlyList<T>
    {
        public TList List { get; }

        int IReadOnlyCollection<T>.Count => List.Count;

        T IReadOnlyList<T>.this[int index] => List[index];

        public new ReadOnlyListEnumerator<TList, T> GetEnumerator()
        {
            return new ReadOnlyListEnumerator<TList, T>(List, 0, Count);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
