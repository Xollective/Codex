using System.Diagnostics.ContractsLight;
using Codex.Lucene.Formats;
using Codex.Lucene.Search;
using Codex.Storage;
using Codex.Utilities;

namespace Codex.Lucene
{
    public interface IDiffable<T>
        where T : IDiffable<T>
    {
        static abstract void Diff(T left, T right, out T leftOnly, out T rightOnly);

        static (T leftOnly, T rightOnly) Diff(T left, T right)
        {
            T.Diff(left, right, out var leftOnly, out var rightOnly);
            return (leftOnly, rightOnly);
        }
    }

    public static class Diffable
    {
        public static (T leftOnly, T rightOnly) Diff<T>(T left, T right)
            where  T : IDiffable<T>
        {
            T.Diff(left, right, out var leftOnly, out var rightOnly);
            return (leftOnly, rightOnly);
        }
    }

    public record EntityAssociation(MurmurHash EntityUid, int DocId);

    public class PersistedStoredFilter : IDiffable<PersistedStoredFilter>
    {
        public ICommitInfo CommitInfo { get; set; }

        public Dictionary<SearchTypeId, EntityAssociation[]> EntityVerificationMap { get; set; }

        public ProjectReferenceMinCountSketch ProjectReferenceCountSketch { get; set; } = new();

        public PersistedStoredFilterSet AllFilter { get; set; } = new();

        public PersistedStoredFilterSet DeclaredDefinitionFilter { get; set; } = new();

        public bool IsEmpty => AllFilter.FiltersByType.Count == 0;

        public PersistedStoredFilter()
        {
        }

        public static void Diff(PersistedStoredFilter left, PersistedStoredFilter right, out PersistedStoredFilter leftOnly, out PersistedStoredFilter rightOnly)
        {
            leftOnly = new();
            rightOnly = new();

            leftOnly.ProjectReferenceCountSketch = left.ProjectReferenceCountSketch;
            rightOnly.ProjectReferenceCountSketch = right.ProjectReferenceCountSketch;

            var allFilterDiff = Diffable.Diff(left.AllFilter, right.AllFilter);

            leftOnly.AllFilter = allFilterDiff.leftOnly;
            rightOnly.AllFilter = allFilterDiff.rightOnly;

            var declaredDefFilter = Diffable.Diff(left.DeclaredDefinitionFilter, right.DeclaredDefinitionFilter);

            leftOnly.DeclaredDefinitionFilter = declaredDefFilter.leftOnly;
            rightOnly.DeclaredDefinitionFilter = declaredDefFilter.rightOnly;
        }

        public void Add(PersistedStoredFilter other, bool recomputeAggregates = true)
        {
            ProjectReferenceCountSketch?.Add(other.ProjectReferenceCountSketch);

            AllFilter.Add(other.AllFilter);
            DeclaredDefinitionFilter.Add(other.DeclaredDefinitionFilter);

            if (recomputeAggregates)
            {
                RecomputeAggregates();
            }
        }

        public void Subtract(PersistedStoredFilter other, bool recomputeAggregates = false)
        {
            ProjectReferenceCountSketch?.Subtract(other.ProjectReferenceCountSketch);

            AllFilter.Subtract(other.AllFilter);
            DeclaredDefinitionFilter.Subtract(other.DeclaredDefinitionFilter);

            if (recomputeAggregates)
            {
                RecomputeAggregates();
            }
        }

        public void RecomputeAggregates()
        {
            AllFilter.RecomputeAggregates();
            DeclaredDefinitionFilter.RecomputeAggregates();
        }

        public void ClearCountingFilters()
        {
            AllFilter.CountingFiltersByType.Clear();
            DeclaredDefinitionFilter.CountingFiltersByType.Clear();
        }
    }

    public class PersistedStoredFilterSet : IDiffable<PersistedStoredFilterSet>
    {
        public Dictionary<SearchTypeId, RoaringDocIdSet> FiltersByType { get; set; } = new();

        public Dictionary<SearchTypeId, CountingFilter> CountingFiltersByType { get; set; } = new();

        public void RecomputeAggregates()
        {
            FiltersByType.Clear();
            foreach ((var searchType, var countingFilter) in CountingFiltersByType)
            {
                var aggregateFilter = countingFilter.GetAggregate();
                if (aggregateFilter.Count != 0)
                {
                    FiltersByType[searchType] = aggregateFilter;
                }
            }
        }

        public void Add(PersistedStoredFilterSet subFilter)
        {
            foreach ((var searchType, var filter) in subFilter.FiltersByType)
            {
                var countingFilter = CountingFiltersByType.GetOrAdd(searchType, new CountingFilter());
                countingFilter.Add(filter);
            }
        }

        public void Subtract(PersistedStoredFilterSet subFilter)
        {
            foreach ((var searchType, var filter) in subFilter.FiltersByType)
            {
                bool found = CountingFiltersByType.TryGetValue(searchType, out var countingFilter);
                Contract.Assert(found);
                countingFilter.Subtract(filter);
            }
        }

        public static void Diff(PersistedStoredFilterSet left, PersistedStoredFilterSet right, out PersistedStoredFilterSet leftOnly, out PersistedStoredFilterSet rightOnly)
        {
            Contract.Assert(left.CountingFiltersByType.Count == 0);
            Contract.Assert(right.CountingFiltersByType.Count == 0);

            leftOnly = new PersistedStoredFilterSet();
            rightOnly = new PersistedStoredFilterSet();

            foreach ((var searchType, var leftFilter) in left.FiltersByType)
            {
                if (right.FiltersByType.TryGetValue(searchType, out var rightFilter))
                {
                    CountingFilter.Diff(leftFilter, rightFilter, out var leftOnlyFilter, out var rightOnlyFilter);
                    leftOnly.FiltersByType[searchType] = leftOnlyFilter;
                    rightOnly.FiltersByType[searchType] = rightOnlyFilter;
                }
                else
                {
                    leftOnly.FiltersByType[searchType] = leftFilter;
                }
            }

            foreach ((var searchType, var rightFilter) in right.FiltersByType)
            {
                if (!left.FiltersByType.TryGetValue(searchType, out var leftFilter))
                {
                    rightOnly.FiltersByType[searchType] = rightFilter;
                }
            }
        }
    }

    public class PersistedIdSet
    {
        public int Cardinality { get; set; }

        public int MaxDoc { get; set; }

        public Dictionary<int, Memory<byte>> Segments { get; set; } = new();
    }
}