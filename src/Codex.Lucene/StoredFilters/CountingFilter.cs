using System.Numerics;
using System.Text.Json.Serialization;
using Codex.Lucene.Formats;
using Codex.Sdk.Utilities;
using Codex.Utilities;
using Codex.Utilities.Serialization;
using Lucene.Net.Util;
using static Codex.Utilities.CollectionUtilities;

namespace Codex.Lucene.Search;

public class CountingFilter : IJsonConvertible<CountingFilter, List<RoaringDocIdSet>>
{
    public List<RoaringDocIdSet> Filters { get; } = new();

    public CountingFilter()
    {
    }

    public CountingFilter(List<RoaringDocIdSet> persistedFilters)
    {
        Filters.AddRange(persistedFilters);
    }

    public RoaringDocIdSet GetAggregate()
    {
        int maxDoc = -1;
        foreach (var filter in Filters)
        {
            maxDoc = Math.Max(maxDoc, filter.MaxDoc);
        }

        var fixedBitSet = new FixedBitSet(NumberUtils.CeilingMultiple(maxDoc + 1, RoaringDocIdSet.BLOCK_SIZE));

        foreach (var filter in Filters)
        {
            filter.SetBits(fixedBitSet);
        }

        var builder = new RoaringDocIdSet.Builder();
        builder.Add(fixedBitSet.GetIterator());
        return builder.Build();
    }

    public void Add(RoaringDocIdSet addedFilter)
    {
        var carryFilter = addedFilter;

        for (int i = 0; i < Filters.Count; i++)
        {
            var filter = Filters[i];
            Add(filter, carryFilter, out filter, out carryFilter);
            Filters[i] = filter;

            if (carryFilter.Count == 0)
            {
                break;
            }
        }

        if (carryFilter.Count != 0)
        {
            Filters.Add(carryFilter);
        }
    }

    public void Subtract(RoaringDocIdSet addedFilter)
    {
        var carryFilter = addedFilter;

        for (int i = 0; i < Filters.Count; i++)
        {
            var filter = Filters[i];
            Subtract(filter, carryFilter, out filter, out carryFilter);
            Filters[i] = filter;

            if (carryFilter.Count == 0)
            {
                break;
            }
        }

        // Remove trailing empty filters
        for (int i = Filters.Count - 1; i >= 0; i--)
        {
            var filter = Filters[i];
            if (filter.Count == 0)
            {
                Filters.RemoveAt(i);
            }
        }
    }

    private static RoaringDocIdSet Union(
        RoaringDocIdSet set1,
        RoaringDocIdSet set2)
    {
        if (set2.Count == 0)
        {
            return set1;
        }

        Func<MergeMode, bool> isCarryBit = m => true;
        Combine(set1, set2, out _, carry: out var union, isCarryBit);

        return union;
    }

    private static void Add(
        RoaringDocIdSet set1,
        RoaringDocIdSet set2,
        out RoaringDocIdSet result,
        out RoaringDocIdSet carry)
    {
        if (set2.Count == 0)
        {
            result = set1;
            carry = RoaringDocIdSet.Empty;
            return;
        }

        Func<MergeMode, bool> isCarryBit = m => m == MergeMode.Both;
        Combine(set1, set2, out result, out carry, isCarryBit);
    }

    private static void Subtract(
        RoaringDocIdSet set1,
        RoaringDocIdSet set2,
        out RoaringDocIdSet result,
        out RoaringDocIdSet carry)
    {
        if (set2.Count == 0)
        {
            result = set1;
            carry = RoaringDocIdSet.Empty;
            return;
        }

        Func<MergeMode, bool> isCarryBit = m => m == MergeMode.RightOnly;
        Combine(set1, set2, out result, out carry, isCarryBit);
    }

    public static void Diff(
        RoaringDocIdSet left,
        RoaringDocIdSet right,
        out RoaringDocIdSet leftOnly,
        out RoaringDocIdSet rightOnly)
    {
        var leftBuilder = new RoaringDocIdSet.Builder();
        var rightBuilder = new RoaringDocIdSet.Builder();

        foreach (var item in DistinctMergeSorted(
            left.Enumerate(),
            right.Enumerate(),
            i => i,
            i => i))
        {
            if (item.mode == MergeMode.LeftOnly)
            {
                leftBuilder.Add(item.left);
            }
            else if (item.mode == MergeMode.RightOnly)
            {
                rightBuilder.Add(item.right);
            }
        }

        leftOnly = leftBuilder.Build();
        rightOnly = rightBuilder.Build();
    }

    private static void Combine(
        RoaringDocIdSet set1,
        RoaringDocIdSet set2,
        out RoaringDocIdSet result,
        out RoaringDocIdSet carry,
        Func<MergeMode, bool> isCarryBit)
    {
        var resultBuilder = new RoaringDocIdSet.Builder();
        var carryBuilder = new RoaringDocIdSet.Builder();
        foreach (var item in DistinctMergeSorted(
            set1.Enumerate(),
            set2.Enumerate(),
            i => i,
            i => i))
        {
            if (item.mode != MergeMode.Both)
            {
                resultBuilder.Add(item.Either());
            }

            if (isCarryBit(item.mode))
            {
                carryBuilder.Add(item.Either());
            }
        }

        result = resultBuilder.Build();
        carry = carryBuilder.Build();
    }

    public static CountingFilter ConvertFromJson(List<RoaringDocIdSet> jsonFormat)
    {
        return new CountingFilter(jsonFormat);
    }

    public List<RoaringDocIdSet> ConvertToJson()
    {
        return Filters;
    }
}