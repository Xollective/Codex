
using System.Buffers;
using Codex.ObjectModel;

namespace Codex.Storage.BlockLevel;

public static class SymbolMapping
{
    const int HASH_COUNT = 8;

    public static readonly FeatureSwitch<int> MinSymbolMapMaxValue = 1 << 7;

    public record struct HashRingEntry(ulong Hash, int Index, bool IsSymbol) : IComparable<HashRingEntry>
    {
        public double Angle => (Hash * 360.0) / ulong.MaxValue;

        public int CompareTo(HashRingEntry other)
        {
            return Hash.ChainCompareTo(other.Hash)
                ?? IsSymbol.ChainCompareTo(other.IsSymbol)
                ?? Index.CompareTo(other.Index);
        }
    }

    public record struct HashRingDiffEntry(ulong Diff, int Index, int SymbolIndex) : IComparable<HashRingDiffEntry>
    {
        public double Angle => (Diff * 360.0) / ulong.MaxValue;

        public ulong DiffFraction => ulong.MaxValue / Diff;

        public int CompareTo(HashRingDiffEntry other)
        {
            return
                Diff.ChainCompareTo(other.Diff)
                ?? Index.ChainCompareTo(other.Index)
                ?? SymbolIndex.ChainCompareTo(other.SymbolIndex)
                ?? 0;
        }
    }

    public static ulong[] HASH_SEEDS = Enumerable.Range(0, HASH_COUNT).Select(i => Murmur3.ComputeBytesHash<int>([i]).Low).ToArray();

    public static IImmutableDictionary<TSymbol, int> PopulateSymbolMap<TSymbol>(
        IEnumerable<TSymbol> symbols,
        IUnifiedComparer<TSymbol> comparer,
        Func<TSymbol, MurmurHash> getHash,
        AsyncOut<TSymbol[]> symbolIndexMap = null)
    {
        var symbolMap = ImmutableSortedDictionary.Create<TSymbol, int>(comparer)
            .SetItems(symbols.Select(s => KeyValuePair.Create(s, 0)))
            .ToBuilder();

        bool isDefault(TSymbol symbol)
        {
            return comparer.Equals(symbol, default);
        }

        var symbolsArray = symbolMap.Keys.ToArray();

        // Ensure mappedSymbols.Length is at least 127 i.e. size of 1-byte
        // 7-bit encoded integer since that's the minimum size used for encoding
        // the reference ids.
        var mappedSymbols = new TSymbol[Math.Max(MinSymbolMapMaxValue, symbolMap.Count * 4)];
        symbolIndexMap?.Set(mappedSymbols);

        Span<ulong> hashInputSpan = [0, 0, 0];

        List<HashRingEntry> entries = new List<HashRingEntry>();

        // NOTE: We skip index = 0, is this relevant?
        for (int index = 1; index < mappedSymbols.Length; index++)
        {
            hashInputSpan[0] = (ulong)index;
            hashInputSpan[1] = 0;

            //var seed = HASH_SEEDS[0];
            foreach (var seed in HASH_SEEDS)
            {
                hashInputSpan[2] = seed;
                var hash = Murmur3.ComputeBytesHash<ulong>(hashInputSpan);

                entries.Add(new(hash.Low, index, false));
            }
        }

        foreach ((var symbol, var index) in symbolsArray.WithIndices())
        {
            var symbolHash = getHash(symbol).GetParts();
            hashInputSpan[0] = symbolHash.High;
            hashInputSpan[1] = symbolHash.Low;

            foreach (var seed in HASH_SEEDS)
            {
                hashInputSpan[2] = seed;
                var hash = Murmur3.ComputeBytesHash<ulong>(hashInputSpan);

                entries.Add(new(hash.Low, index, true));
            }
        }

        entries.Sort();

        SpanRingBuffer<HashRingEntry> lastIndexEntries = new(stackalloc HashRingEntry[2]);
        int assignmentCount = 0;
        var diffEntries = new List<HashRingDiffEntry>(entries.Count);
        var selectedDiffEntries = new List<HashRingDiffEntry>(symbolsArray.Length);
        int remainingIterations = 5;
        bool isFirstIteration = true;

        while (assignmentCount < symbolsArray.Length && remainingIterations-- > 0)
        {
            diffEntries.Clear();
            selectedDiffEntries.Clear();

            foreach (var reverse in Out.Span([false]))
            {
                lastIndexEntries.Clear();
                int index = reverse ? entries.Count - 1 : 0;
                int increment = reverse ? -1 : 1;
                while ((uint)index < (uint)entries.Count)
                {
                    var entry = entries[index];
                    index += increment;
                    if (!entry.IsSymbol)
                    {
                        if (isFirstIteration || isDefault(mappedSymbols[entry.Index]))
                        {
                            lastIndexEntries.Push(entry);
                        }
                    }
                    else if (isFirstIteration || !isDefault(symbolsArray[entry.Index]))
                    {
                        for (int i = 0; i < lastIndexEntries.Count; i++)
                        {
                            var indexEntry = lastIndexEntries[i];
                            var diff = reverse
                                ? indexEntry.Hash - entry.Hash
                                : entry.Hash - indexEntry.Hash;
                            var diffEntry = new HashRingDiffEntry(Diff: diff, indexEntry.Index, SymbolIndex: entry.Index);
                            diffEntries.Add(diffEntry);
                        }
                    }
                }
            }

            isFirstIteration = false;

            diffEntries.Sort();
            int j = -1;

            foreach (var entry in diffEntries)
            {
                j++;
                var index = entry.Index;
                ref var symbol = ref symbolsArray[entry.SymbolIndex];
                ref var mappedSymbol = ref mappedSymbols[index];

                if (!isDefault(symbol) && isDefault(mappedSymbol))
                {
                    mappedSymbol = symbol;
                    symbolMap[symbol] = index;

                    // Prevent reassignment of symbol
                    symbol = default;
                    assignmentCount++;
                    selectedDiffEntries.Add(entry);
                    if (assignmentCount == symbolsArray.Length) break;
                }
            }
        }

        Contract.Assert(assignmentCount == symbolsArray.Length);

        return symbolMap.ToImmutable();
    }
}
