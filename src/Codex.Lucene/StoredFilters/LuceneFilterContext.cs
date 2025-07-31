using System.Numerics;
using Codex.Lucene.Framework;
using Codex.Sdk.Search;
using Codex.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Search;

public class LuceneFilterContext : StoredFilterSearchContext<LuceneClient>, ILuceneStoredFilter
{
    public LuceneFilterContext(LuceneClient client, 
        ProjectReferenceMinCountSketch projectReferenceCountSketch,
        ISearchTypeFilterProvider filterProvider,
        ISearchTypeFilterProvider declaredDefinitionFilterProvider) 
        : base(client)
    {
        ProjectReferenceCountSketch = projectReferenceCountSketch;
        FilterProvider = filterProvider;
        DeclaredDefinitionFilterProvider = declaredDefinitionFilterProvider;
        DeclaredDefinitionsFilter = new LuceneStoredFilter(projectReferenceCountSketch, declaredDefinitionFilterProvider);
    }

    public ProjectReferenceMinCountSketch ProjectReferenceCountSketch { get; }

    public ISearchTypeFilterProvider FilterProvider { get; }

    public ISearchTypeFilterProvider DeclaredDefinitionFilterProvider { get; }

    public override IStoredFilterInfo DeclaredDefinitionsFilter { get; }

    public new ILuceneStoredFilter? SecondaryFilter => base.SecondaryFilter as ILuceneStoredFilter;

    public LuceneFilterContext Intersect(LuceneFilterContext other)
    {
        return new LuceneFilterContext(
            Client,
            ProjectReferenceCountSketch,
            filterProvider: SearchTypeFilterProvider.Intersect(FilterProvider, other.FilterProvider),
            declaredDefinitionFilterProvider: SearchTypeFilterProvider.Intersect(DeclaredDefinitionFilterProvider, other.DeclaredDefinitionFilterProvider));
    }
}

public interface ISearchTypeFilterProvider
{
    IBitSet GetFilter(SearchTypeId searchType);
}

public record SearchTypeFilterProvider(Func<SearchTypeId, IBitSet> getSearchTypeFilter) : ISearchTypeFilterProvider
{
    private static readonly IBitSet AllBits = new Bits.MatchAllBits(int.MaxValue);
    private static readonly IBitSet NoBits = new Bits.MatchNoBits(int.MaxValue);

    public IBitSet GetFilter(SearchTypeId searchType) => getSearchTypeFilter(searchType);

    public static SearchTypeFilterProvider Intersect(ISearchTypeFilterProvider left, ISearchTypeFilterProvider right)
    {
        return new SearchTypeFilterProvider(searchType => new AndBits(left.GetFilter(searchType), right.GetFilter(searchType)));
    }

    public static SearchTypeFilterProvider From(PersistedStoredFilterSet filter)
    {
        return new SearchTypeFilterProvider(searchType => filter.FiltersByType.TryGetValue(searchType, out var bits) ? bits : NoBits);
    }

    public static SearchTypeFilterProvider All()
    {
        return new SearchTypeFilterProvider(searchType => AllBits);
    }
}

public interface ILuceneStoredFilter : IStoredFilterInfo
{
    ProjectReferenceMinCountSketch ProjectReferenceCountSketch { get; }

    ISearchTypeFilterProvider FilterProvider { get; }

    bool IStoredFilterInfo.IsPossibleProject(string projectId) => (ProjectReferenceCountSketch?.Get(projectId) ?? 1) > 0;

    ILuceneStoredFilter? SecondaryFilter { get; }
}

public record LuceneStoredFilter(ProjectReferenceMinCountSketch ProjectReferenceCountSketch, ISearchTypeFilterProvider FilterProvider) : ILuceneStoredFilter
{
    public ILuceneStoredFilter? SecondaryFilter => null;
}
