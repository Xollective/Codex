using Codex.Utilities;
using Lucene.Net.Util;

namespace Codex.Lucene.Framework;

public record AndBits(IBitSet Left, IBitSet Right) : IBitSet
{
    public int Length => Math.Max(Left.Length, Right.Length);

    public int ValidLength { get; } = Math.Min(Left.Length, Right.Length);

    public IEnumerable<int> Enumerate()
    {
        return CollectionUtilities.DistinctMergeSorted(
            Left.Enumerate(),
            Right.Enumerate(),
            i => i,
            i => i)
            .Where(m => m.mode == CollectionUtilities.MergeMode.Both)
            .Select(e => e.right);
    }

    public bool Get(int index)
    {
        if (index >= ValidLength)
        {
            return false;
        }

        bool result = Left.Get(index) && Right.Get(index);
        if (!result)
        {

        }

        return result;
    }
}
