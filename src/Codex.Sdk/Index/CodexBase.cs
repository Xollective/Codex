using System;
using System.Collections.Concurrent;
using Codex.ObjectModel;
using Codex.ObjectModel.CompilerServices;
using Codex.ObjectModel.Implementation;
using Codex.Sdk;
using Codex.Sdk.Search;
using Codex.Storage.BlockLevel;
using Codex.Utilities;
using static Codex.Search.SearchUtilities;

namespace Codex.Search
{
    using M = SearchMappings;
    using static CodexConstants.Terms;

    public record CodexBaseConfiguration
    {
        public TimeSpan CachedAliasIdRetention = TimeSpan.FromMinutes(30);

        public ISourceTextRetriever SourceTextRetriever { get; set; }

        public virtual bool UseBlockModel { get; }
    }

    public abstract class CodexBase<TClient, TConfiguration> : ICodex
       where TClient : IClient
       where TConfiguration : CodexBaseConfiguration
    {
        public readonly TConfiguration Configuration;

        public CodexBase(TConfiguration configuration)
        {
            Configuration = configuration;
        }

        public Task<IndexQueryHitsResponse<ICommit>> GetRepositoryHeadsAsync(GetRepositoryHeadsArguments arguments)
        {
            return UseClient<ICommit>(arguments, async (context, responseBuilder) =>
            {
                var client = context.Client;

                var results = await client.CommitIndex.SearchAsync(context,
                    cq => cq.All(),
                    take: arguments.MaxResults);

                return new IndexQueryHits<ICommit>()
                {
                    Hits = results.Hits.SelectList(s => s.Source.Commit),
                    Total = results.Total
                };
            });
        }

        public Task<IndexQueryResponse<ReferencesResult>> FindAllReferencesAsync(FindAllReferencesArguments arguments)
        {
            return FindReferencesCore(arguments, async context =>
            {
                var client = context.Client;
                IIndexSearchResponse<IReferenceSearchModel> referencesResult = await FindAllReferencesAsyncCore(context, arguments);

                if (string.IsNullOrEmpty(arguments.ProjectScopeId) // No need to look for inbound type forwards when scoped to a project
                    && referencesResult.Hits.Count < arguments.MaxResults
                    && referencesResult.Hits.Count > 0)
                {
                    await populateTypeForwardedReferencesAsync();

                    async Task populateTypeForwardedReferencesAsync()
                    {
                        var typeSymbol = await getTypeSymbolIdAsync();
                        if (typeSymbol == null) return;

                        var transitiveForwardedProjects = await GetTransitiveTypeForwardProjects(
                            context,
                            projectId: arguments.ProjectId,
                            typeSymbol: typeSymbol.Value,
                            maxResults: arguments.MaxResults - referencesResult.Hits.Count,
                            gotoDef: false);

                        if (transitiveForwardedProjects.Count == 0) return;

                        var extraReferencesResult = await FindAllReferencesAsyncCore(context, arguments, transitiveForwardedProjects);

                        referencesResult = new IndexSearchResponse<IReferenceSearchModel>()
                        {
                            Hits =
                            {
                                referencesResult.Hits,
                                extraReferencesResult.Hits
                            },
                            Total = referencesResult.Total + extraReferencesResult.Total
                        };
                    }

                    async ValueTask<SymbolIdArgument?> getTypeSymbolIdAsync()
                    {
                        var refModel = referencesResult.Hits[0].Source.ToImplementation();
                        var reference = refModel.Symbol;
                        var isType = reference.Kind.Value?.IsTypeKind() ?? false;
                        if (isType)
                        {
                            return reference.Id;
                        }

                        var definition = refModel.Definition;
                        if (definition == null)
                        {
                            var results = await client.DefinitionIndex.SearchAsync(context,
                                        cq =>
                                            cq.Term(M.Definition.Id, arguments.SymbolId)
                                            & cq.Term(M.Definition.ProjectId, arguments.ProjectId),
                                        take: 1);

                            if (results.Hits.Count == 0) return default;

                            definition = results.Hits[0].Source.Definition;
                        }

                        return definition.ContainerTypeSymbolId;
                    }
                }

                return referencesResult;
            });
        }

        private async Task<IReadOnlyCollection<string>> GetTransitiveTypeForwardProjects(
            StoredFilterSearchContext<TClient> context,
            string projectId,
            SymbolIdArgument typeSymbol,
            int maxResults,
            bool gotoDef = false)
        {
            var typeForwards = await context.Client.ReferenceIndex.SearchAsync(
                                context,
                                cq =>
                                    (cq.Term(M.Reference.Id, typeSymbol)
                                    & cq.Term(M.Reference.ReferenceKind, ReferenceKind.TypeForwardedTo))
                                    + cq.Term(gotoDef ? M.Reference.ReferencingProjectId : M.Reference.ProjectId, projectId),
                                take: maxResults,
                                updateOptions: o => o with { AddressKind = AddressKind.InfoHeader });

            var transitiveForwardedProjects = new HashSet<string>();
            if (typeForwards.Hits.Count == 0) return transitiveForwardedProjects;

            ILookup<string, string> projectByTargetProjectLookup = typeForwards.Hits.Select(r => r.Source)
                .Select(r => (from: r.FileInfo.ProjectId, to: r.Symbol.ProjectId))
                .Select(r => gotoDef ? (from: r.to, to: r.from) : r)
                .ToLookup(r => r.to, r => r.from, StringComparer.OrdinalIgnoreCase);

            var transitiveForwardedProjectsQueue = new Queue<string>();
            transitiveForwardedProjectsQueue.Enqueue(projectId);

            while (transitiveForwardedProjectsQueue.TryDequeue(out var project))
            {
                foreach (var forwardingProject in projectByTargetProjectLookup[project])
                {
                    if (transitiveForwardedProjects.Add(forwardingProject))
                    {
                        transitiveForwardedProjectsQueue.Enqueue(forwardingProject);
                    }
                }
            }

            return transitiveForwardedProjects;
        }

        private async Task<IIndexSearchResponse<IReferenceSearchModel>> FindAllReferencesAsyncCore(
            StoredFilterSearchContext<TClient> context,
            FindAllReferencesArguments arguments,
            IEnumerable<string> projectsOverride = null)
        {
            var allProjects = projectsOverride ?? new[] { arguments.ProjectId };

            arguments.RequireLineTexts = true;
            var kinds = arguments.GetFindAllReferenceKinds();
            return await context.Client.ReferenceIndex.SearchAsync(
                context,
                cq =>
                    ((cq.Term(M.Reference.Id, arguments.SymbolId)
                        & cq.Terms(M.Reference.ProjectId, allProjects)
                        & cq.Terms(M.Reference.ReferenceKind, kinds?.Enumerate()))
                        | (arguments.IncludeRelatedDefinitions
                            ? (cq.Terms(M.Reference.ReferencingProjectId, allProjects) & cq.Term(M.Reference.RelatedDefinition, arguments.SymbolId))
                            : null))
                    & cq.Term(M.Reference.ReferencingProjectId, arguments.ProjectScopeId),
                boost: kinds?.Contains(ReferenceKind.Reference) != false
                    // Only need to boost categorized references is search my include uncategorized references
                    ? cq => !cq.Term(M.Reference.ReferenceKind, ReferenceKind.Reference)
                    : default,
                sort: OneOrMany(M.Reference.ProjectId),
                take: arguments.MaxResults,
                updateOptions: o => kinds?.IsSubsetOf(ReferenceKindSet.DefinitionKinds) == true
                    // If searching only for definitions, get address which only include definitions
                    ? o with { AddressKind = AddressKind.Definitions }
                    : o);
        }

        public Task<IndexQueryHitsResponse<IDefinitionSearchModel>> FindDefinitionAsync(FindDefinitionArguments arguments)
        {
            return UseClient<IDefinitionSearchModel>(arguments, async (context, responseBuilder) =>
            {
                var client = context.Client;

                var results = await client.DefinitionIndex.SearchAsync(context,
                    cq =>
                        cq.Term(M.Definition.Id, arguments.SymbolId)
                        & cq.Term(M.Definition.ProjectId, arguments.ProjectId),
                    take: arguments.MaxResults);

                return new IndexQueryHits<IDefinitionSearchModel>()
                {
                    Hits = results.Hits.SelectList(s => s.Source),
                    Total = results.Total
                };
            });
        }

        public Task<IndexQueryResponse<ReferencesResult>> FindDefinitionLocationAsync(FindDefinitionLocationArguments arguments)
        {
            Placeholder.Todo("Prefer results from current repository");
            return FindReferencesCore(arguments, async context =>
            {
                var client = context.Client;

                bool shouldContinue = false;
                bool isForwardingLookup = false;
                IIndexSearchResponse<IReferenceSearchModel> referencesResult;
                var symbolId = arguments.SymbolId;
                IReadOnlyCollection<string> projects = arguments.ProjectId.AsSingle();
                int iterations = 3;
                do
                {
                    shouldContinue = false;
                    referencesResult = await context.Client.ReferenceIndex.SearchAsync(
                        context,
                        cq =>
                            cq.Terms(M.Reference.ProjectId, projects)
                            & cq.Term(M.Reference.Id, symbolId)
                            & cq.Term(M.Reference.ReferenceKind, ReferenceKind.Definition),
                        take: arguments.MaxResults,
                        updateOptions: o => o with { AddressKind = AddressKind.Definitions });

                    if (referencesResult.Total == 0 && !isForwardingLookup && string.IsNullOrEmpty(arguments.ProjectScopeId))
                    {
                        var definitionResult = await context.Client.DefinitionIndex.SearchAsync(
                            context,
                            cq => cq.Term(M.Definition.Id, symbolId),
                            boost: cq => cq.Term(M.Definition.ProjectId, arguments.ProjectId),
                            take: 1);

                        if (definitionResult.Total != 0)
                        {
                            IDefinitionSymbol definition = definitionResult.Hits[0].Source.Definition;
                            var typeSymbol = definition.Kind.Value?.IsTypeKind() == true
                                ? definition.Id
                                : definition.ContainerTypeSymbolId;

                            if (!typeSymbol.IsValid) continue;

                            if (definition.ProjectId == arguments.ProjectId && definition.Kind == SymbolKinds.Constructor)
                            {
                                // For constructors, retry to find the containing type.
                                symbolId = typeSymbol;
                                shouldContinue = true;
                                continue;
                            }

                            var forwardingProjects = await GetTransitiveTypeForwardProjects(
                                context,
                                projectId: arguments.ProjectId,
                                typeSymbol: typeSymbol,
                                maxResults: arguments.MaxResults,
                                gotoDef: true);

                            if (forwardingProjects.Count != 0)
                            {
                                projects = forwardingProjects;
                                shouldContinue = true;
                                isForwardingLookup = true;
                            }
                        }
                    }
                }
                while (shouldContinue && --iterations >= 0);

                if (arguments.FallbackFindAllReferences && referencesResult.Hits.Count == 0)
                {
                    arguments.IsFallback = true;
                    arguments.ReferenceKind = null;
                    // No definitions, return the the result of find all references 
                    referencesResult = await FindAllReferencesAsyncCore(context, arguments);
                }
                else
                {
                    arguments.SymbolId = symbolId;
                }

                return referencesResult;
            });
        }

        private async Task<IndexQueryResponse<ReferencesResult>> FindReferencesCore(FindAllReferencesArguments arguments, Func<StoredFilterSearchContext<TClient>, Task<IIndexSearchResponse<IReferenceSearchModel>>> getReferencesAsync)
        {
            return await UseClientSingle<ReferencesResult>(arguments, async (context, responseBuilder) =>
            {
                var client = context.Client;

                IIndexSearchResponse<IReferenceSearchModel> referencesResult = await getReferencesAsync(context);

                //var displayName = await GetSymbolShortName(context, arguments);
                var findAllReferenceKinds = arguments.GetFindAllReferenceKinds();

                var searchResults =
                    (from hit in referencesResult.Hits
                     let referenceSearchModel = hit.Source
                     where referenceSearchModel.Symbol.Id == arguments.SymbolId
                     from span in referenceSearchModel.Spans
                     where findAllReferenceKinds?.Contains(span.Reference.ReferenceKind) != false
                     where !span.Reference.ExcludeFromSearch
                     select new ReferenceSearchResult()
                     {
                         RootEntity = referenceSearchModel,
                         File = referenceSearchModel.FileInfo,
                         ReferenceSpan = span
                     }).ToList();

                var result = new ReferencesResult()
                {
                    Arguments = arguments,
                    ReferenceKind = arguments.ReferenceKind,
                    Hits = searchResults,
                    Total = referencesResult.Total,
                };

                if (Configuration.UseBlockModel)
                {
                    var references = referencesResult.Hits.Select(r => r.Source.ToImplementation()).Where(r => r.Symbol.Id == arguments.SymbolId);
                    result.Definition = references.Select(r => r.Definition).FirstOrDefault(d => d != null);
                    result.SymbolDisplayName = result.Definition?.GetFindAllReferencesDisplayName();
                    result.RelatedDefinitions = references.SelectMany(r => r.RelatedDefinitions.EmptyIfNull()).Distinct(CodeSymbol.RelatedDefinitionComparer).ToList();
                }
                else
                {
                    var primaryReference = new ReferenceSymbol()
                    {
                        Id = SymbolId.UnsafeCreateWithValue(arguments.SymbolId),
                        ProjectId = arguments.ProjectId,
                        ReferenceKind = ReferenceKind.Definition
                    };

                    var relatedDefinitionReferences = primaryReference.ToCollection().Concat(referencesResult.Hits
                        .Select(r => r.Source)
                        .TakeWhile(r => arguments.IncludeRelatedDefinitions)
                        .Where(s => s.Symbol.Id.Value != arguments.SymbolId)
                        .SelectMany(r => r.Spans
                            .Where(i => i.Reference.ReferenceKind != ReferenceKind.Definition && i.RelatedDefinition == arguments.SymbolId))
                        .Select(rs => rs.Reference)
                        .Take(5));

                    var definitionsResult = await client.DefinitionIndex.SearchAsync(
                        context,
                        cq => relatedDefinitionReferences
                            .Select(s => cq.Term(M.Definition.ProjectId, s.ProjectId) & cq.Term(M.Definition.Id, s.Id.Value))
                            .Aggregate(cq.None(), (q1, q2) => q1 | q2)
                        ,
                        take: 20);

                    var displayName = definitionsResult.Hits.Where(d => d.Source.Definition.SymbolEquals(primaryReference))
                        .FirstOrDefault()?.Source.Definition.GetFindAllReferencesDisplayName();

                    var relatedDefinitions = relatedDefinitionReferences.Join(definitionsResult.Hits.Select(r => r.Source.Definition),
                        r => Requires.Expect<ICodeSymbol>(r),
                        d => Requires.Expect<ICodeSymbol>(d),
                        (r, d) => new RelatedDefinition(d, r.ReferenceKind),
                        CodeSymbol.SymbolEqualityComparer)
                        .Where(r => r.ReferenceKind != ReferenceKind.Definition)
                        .ToList();

                    result.SymbolDisplayName = displayName;
                    result.RelatedDefinitions = relatedDefinitions;
                }

                return result;
            });
        }

        private async Task<string> GetSymbolShortName(StoredFilterSearchContext<TClient> context, FindSymbolArgumentsBase arguments)
        {
            var client = context.Client;
            var definitionsResult = await client.DefinitionIndex.SearchAsync(
                    context,
                    cq =>
                        cq.Term(M.Definition.ProjectId, arguments.ProjectId)
                        & cq.Term(M.Definition.Id, arguments.SymbolId),
                    take: 1);

            return definitionsResult.Hits.FirstOrDefault()?.Source.Definition.ShortName;
        }

        public async Task<IndexQueryResponse<IBoundSourceFile>> GetSourceAsync(GetSourceArguments arguments)
        {
            // TODO: Get text source if bound source is unavailable.
            return await UseClientSingle<IBoundSourceFile>(arguments, async (context, responseBuilder) =>
            {
                var client = context.Client;

                //var boundResults = await client.SearchAsync<BoundSourceSearchModel>(sd => sd
                //    .StoredFilterSearch(context, IndexName(SearchTypes.BoundSource), qcd => qcd.Bool(bq => bq.Filter(
                //            fq => fq.Term(s => s.File.Info.ProjectId, arguments.ProjectId),
                //            fq => fq.Term(s => s.File.Info.ProjectRelativePath, arguments.ProjectRelativePath))))
                //    .Take(1))
                //.ThrowOnFailure();

                Placeholder.Todo("Prefer results from current repository");

                BoundSourceSearchModel boundSearchModel = null;
                //if (arguments.StableId != null)
                //{
                //    var results = await client.BoundSourceIndex.GetAsync<BoundSourceSearchModel>(null, arguments.Uid.Value);
                //    boundSearchModel = results.FirstOrDefault();
                //}

                if (boundSearchModel == null)
                {
                    var boundResults = await client.BoundSourceIndex.QueryAsync<BoundSourceSearchModel>(
                        context,
                        cq =>
                             cq.Term(M.BoundSource.ProjectId, arguments.ProjectId)
                             & cq.Term(M.BoundSource.ProjectRelativePath, arguments.ProjectRelativePath)
                             & cq.Term(M.BoundSource.RepositoryName, arguments.RepositoryName),
                        take: 1,
                        updateOptions: o => o with { AddressKind = arguments.DefinitionOutline ? AddressKind.Definitions : AddressKind.Default } );

                    boundSearchModel = boundResults.Hits.FirstOrDefault()?.Source;
                }

                if (boundSearchModel != null)
                {
                    var result = boundSearchModel.BoundFile ?? new BoundSourceFile(boundSearchModel.BindingInfo.Expect<IBoundSourceInfo>());
                    result.RootEntity = boundSearchModel;

                    boundSearchModel.File ??= new SourceFileBase()
                    {
                        Info = new SourceFileInfo()
                        {
                            ProjectId = arguments.ProjectId,
                            ProjectRelativePath = arguments.ProjectRelativePath,
                            RepositoryName = arguments.RepositoryName
                        }
                    };

                    var sourceFile = result.SourceFile ??= new SourceFile(boundSearchModel.File)
                    {
                        Content = boundSearchModel.Content
                    };

                    if (!arguments.DefinitionOutline)
                    {
                        var commitResults = await client.CommitIndex.QueryAsync<CommitSearchModel>(
                            context,
                            cq => cq.Term(M.Commit.RepositoryName, result.RepositoryName),
                            take: 1);

                        var commit = result.Commit ??= commitResults.Hits.FirstOrDefault()?.Source.Commit;

                        sourceFile.Info.CommitId ??= commit?.CommitId;

                        if (SourceControlUri.TryParse(result.RepositoryName, out var repoUri, checkRepoNameFormat: true)
                            && sourceFile.Info.WebAddress == null
                            // Don't add files which are not apart of source control
                            && sourceFile.Info.SourceControlContentId != null
                            && sourceFile.Info.RepoRelativePath != null
                            // Don't add web access link for files not under source tree (i.e. [Metadata])
                            && !sourceFile.Info.RepoRelativePath.StartsWith("["))
                        {
                            sourceFile.Info.WebAddress = repoUri.GetFileUrlByCommit(sourceFile.Info.RepoRelativePath, commitId: commit?.CommitId)
                                ?? repoUri.GetFileUrlByBranch(sourceFile.Info.RepoRelativePath, branch: Placeholder.Value<string>("Need to get branch value. There should potentially be a value which combines branch and commit"));

                            sourceFile.Info.DownloadAddress ??= repoUri.GetContentByCommit(commit: sourceFile.Info.CommitId, repoRelativePath: sourceFile.Info.RepoRelativePath);
                        }
                    }

                    return result;
                }

                responseBuilder.Error = "Unable to find source file";
                return default;
            });
        }

        public async Task<IndexQueryResponse<GetProjectResult>> GetProjectAsync(GetProjectArguments arguments)
        {
            return await UseClientSingle<GetProjectResult>(arguments, async (context, responseBuilder) =>
            {
                var client = context.Client;
                GetProjectResult result = null;
                using var _ = new DisposeAction(() =>
                {
                    if (result == null)
                    {
                        responseBuilder.Error = $"Unable to find project information: '{arguments.ProjectId}'";
                    }
                });

                //var response = await client.SearchAsync<ProjectSearchModel>(sd => sd
                //    .StoredFilterSearch(context, IndexName(SearchTypes.Project), qcd => qcd.Bool(bq => bq.Filter(
                //            fq => fq.Term(s => s.Project.ProjectId, arguments.ProjectId))))
                //    .Take(1))
                //.ThrowOnFailure();

                if (arguments.AddressKind == AddressKind.Definitions && arguments.ReferencedProjectId is string referencedProjectId)
                {
                    var projectRefResponse = await client.ProjectReferenceIndex.SearchAsync(
                        context,
                        cq =>
                             cq.Term(M.ProjectReference.ProjectId, arguments.ProjectId) & cq.Term(M.ProjectReference.ReferencedProjectId, referencedProjectId),
                        take: 1,
                        updateOptions: options => options with { AddressKind = arguments.AddressKind });

                    if (projectRefResponse.Hits.Count != 0)
                    {
                        result = new GetProjectResult()
                        {
                            GenerateReferenceMetadata = Configuration.UseBlockModel,
                            AddressKind = arguments.AddressKind,
                            ReferencingProjects = projectRefResponse?.Hits.Select(h => h.Source).ToList()
                        };
                    }
                    else return null;
                }

                var response = await client.ProjectIndex.SearchAsync(
                    context,
                    cq =>
                         cq.Term(M.Project.ProjectId, arguments.ProjectId),
                    take: 1,
                    updateOptions: options => options with { AddressKind = arguments.AddressKind });

                if (response.Hits.Count != 0)
                {
                    IProjectSearchModel projectSearchModel = response.Hits.First().Source;

                    // TODO: Not sure why this is a good marker for when the project was uploaded. Since project search model may be deduplicated,
                    // to a past result we probably need something more accurate. Maybe the upload date of the stored filter. That would more closely
                    // match the legacy behavior.
                    //var commitResponse = await client.SearchAsync<ICommitSearchModel>(sd => sd
                    //     .StoredFilterSearch(context, IndexName(SearchTypes.Commit), qcd => qcd.Bool(bq => bq.Filter(
                    //             fq => fq.Term(s => s.Commit.RepositoryName, projectSearchModel.Project.RepositoryName))))
                    //     .Take(1)
                    //     .CaptureRequest(context));

                    var commitResponse = await client.CommitIndex.SearchAsync(
                        context,
                        cq => cq.Term(M.Commit.RepositoryName, projectSearchModel.Project.RepositoryName),
                        take: 1);

                    var referencesResult = await client.ProjectReferenceIndex.SearchAsync(
                        context,
                        cq => cq.Term(M.ProjectReference.ReferencedProjectId, arguments.ProjectId),
                        sort: OneOrMany(M.ProjectReference.ReferencedProjectId),
                        take: arguments.MaxResults);

                    result = new GetProjectResult()
                    {
                        GenerateReferenceMetadata = Configuration.UseBlockModel,
                        AddressKind = arguments.AddressKind,
                        Project = projectSearchModel.Project,
                        DateUploaded = commitResponse?.Hits.FirstOrDefault()?.Source.Commit.DateUploaded ?? default(DateTime),
                        ReferencingProjects = referencesResult?.Hits.Select(h => h.Source).ToList()
                    }; 

                    if (!Configuration.UseBlockModel)
                    {
                        var project = projectSearchModel.Project.ToImplementation();
                        if (projectSearchModel.Project.Definitions.Count == 0)
                        {
                            bool isTopLevelSearch = arguments.AddressKind == AddressKind.TopLevelDefinitions;
                            var definitionsResponse = await client.DefinitionIndex.SearchAsync(context,
                                cq =>
                                    (isTopLevelSearch ? cq.Terms(M.Definition.Kind, SymbolKindsExtensions.TypeKinds.Select(s => s.ToStringEnum())) : null)
                                    & cq.Term(M.Definition.ProjectId, arguments.ProjectId),
                                take: arguments.MaxResults);

                            if (definitionsResponse?.Hits.Count > 0)
                            {
                                var definitions = definitionsResponse.Hits.Select(h => h.Source.Definition.ToImplementation());
                                project.Definitions.AddRange(definitions);
                            }
                        }

                        if (arguments.AddressKind == AddressKind.TopLevelDefinitions)
                        {
                            project.Definitions.RemoveAll(d => !d.IsTopLevel());
                        }
                    }
                    else
                    {

                    }

                    //var referencesResult = await client.SearchAsync<IProjectReferenceSearchModel>(s => s
                    //    .StoredFilterSearch(context, IndexName(SearchTypes.ProjectReference), qcd => qcd.Bool(bq => bq.Filter(
                    //        fq => fq.Term(r => r.ProjectReference.ProjectId, arguments.ProjectId))))
                    //    .Sort(sd => sd.Ascending(r => r.ProjectId))
                    //    .Source(sfd => sfd.Includes(f => f.Field(r => r.ProjectId)))
                    //    .Take(arguments.MaxResults))
                    //.ThrowOnFailure();

                    return result;
                }

                return null;
            });
        }

        public async Task<IndexQueryHitsResponse<ISearchResult>> SearchAsync(SearchArguments arguments)
        {
            var searchPhrase = arguments.SearchString;

            searchPhrase = searchPhrase?.Trim();

            if (string.IsNullOrEmpty(searchPhrase) || searchPhrase.Length < 3)
            {
                return new IndexQueryHitsResponse<ISearchResult>()
                {
                    Error = "Search phrase must be at least 3 characters"
                };
            }

            return await UseClient<ISearchResult>(arguments, async (context, responseBuilder) =>
            {
                Placeholder.Todo("Allow filtering text matches by extension/path");

                var client = context.Client;

                if (!arguments.TextSearch)
                {
                    var terms = GetSearchTerms(ref arguments, searchPhrase, out var annotateRepos);
                    terms.IncludeReferencedDefinitions |= arguments.AllowReferencedDefinitions;

                    //var definitionsResult2 = await client.DefinitionIndex.SearchAsync(
                    //    null,
                    //    cq => cq.Term(M.Definition.ConstantValue.MarkForRemoval(), 0),
                    //    //filterIndexName: allowReferencedDefinitions ? indexName : GetDeclaredDefinitionsIndexName(indexName)
                    //    take: arguments.MaxResults);

                    IStoredFilterInfo filter = terms.IncludeReferencedDefinitions ? context : context.DeclaredDefinitionsFilter;
                    var definitionsResult = await client.DefinitionIndex.SearchAsync(
                        filter,
                        cq => arguments.TestTermSearch ? cq.Term(M.Definition.ShortName, terms.Terms[0]) : GetTermsFilter(context, cq, terms),
                        boost: arguments.TestTermSearch ? null : cq => GetTermsFilter(context, cq, terms, boostOnly: true),
                        updateOptions: o => o with { ProjectSortField = M.Definition.ProjectId },
                        //filterIndexName: allowReferencedDefinitions ? indexName : GetDeclaredDefinitionsIndexName(indexName)
                        take: arguments.MaxResults);

                    //var indexName = IndexName(SearchTypes.Definition);

                    //var definitionsResult = await client.SearchAsync<IDefinitionSearchModel>(s => s
                    //        .StoredFilterSearch(context, indexName, qcd => qcd.Bool(bq => bq
                    //            .Filter(GetTermsFilter(terms, allowReferencedDefinitions: allowReferencedDefinitions))
                    //            .Should(GetTermsFilter(terms, boostOnly: true))),
                    //            filterIndexName: allowReferencedDefinitions ? indexName : GetDeclaredDefinitionsIndexName(indexName))
                    //        .Take(arguments.MaxResults))
                    //    .ThrowOnFailure();

                    if (definitionsResult.Hits.Count != 0 || !arguments.FallbackToTextSearch)
                    {
                        return new IndexQueryHits<ISearchResult>()
                        {
                            Hits = new List<ISearchResult>(definitionsResult.Hits.Select(hit =>
                                new SearchResult()
                                {
                                    Definition = new DefinitionSymbol(hit.Source.Definition)
                                    {
                                        RootEntity = hit.Source
                                    }
                                        .ApplyIf(annotateRepos, ds =>
                                        {
                                            if (hit.MatchesSecondaryFilter || context.SecondaryFilter == null)
                                            {
                                                ds.ProjectId = "Public Repositories";
                                            }
                                            else
                                            {
                                                ds.ProjectId = "Internal Repositories";
                                            }
                                        })
                                })),
                            Total = definitionsResult.Total
                        };
                    }
                }

                // Fallback to performing text phrase search
                //var textChunkResults = await client.SearchAsync<ITextChunkSearchModel>(
                //     s => s
                //         .StoredFilterSearch(context, IndexName(SearchTypes.TextChunk), f =>
                //             f.Bool(bq =>
                //             bq.Must(qcd => qcd.ConfigureIfElse(isPrefix,
                //                 f0 => f0.MatchPhrasePrefix(mpp => mpp.Field(sf => sf.Chunk.ContentLines).Query(searchPhrase).MaxExpansions(100)),
                //                 f0 => f0.MatchPhrase(mpp => mpp.Field(sf => sf.Chunk.ContentLines).Query(searchPhrase))))))
                //         .Highlight(h => h.Fields(hf => hf.Field(sf => sf.Chunk.ContentLines).BoundaryCharacters("\n\r")))
                //         .Take(arguments.MaxResults))
                //     .ThrowOnFailure();


                bool isPrefix = searchPhrase?.EndsWith("*") ?? false;
                searchPhrase = searchPhrase?.TrimEnd('*');

                Placeholder.Todo("Add highlighting support");
                var textChunkResults = await client.TextChunkIndex.SearchAsync(
                    context,
                    cq => isPrefix
                        ? cq.MatchPhrasePrefix(M.TextChunk.Content, searchPhrase, maxExpansions: 100)
                        : cq.MatchPhrase(M.TextChunk.Content, searchPhrase),
                    take: arguments.MaxResults,
                    updateOptions: o => o with { HighlightField = M.TextChunk.Content });

                var chunkIds = textChunkResults.Hits.ToDictionarySafe(s => s.Source.StableId);

                var textResults = await client.TextSourceIndex.SearchAsync(
                    context,
                    cq => cq.Terms(M.TextSource.ChunkId, chunkIds.Keys),
                    take: arguments.MaxResults);

                TextLineSpan withOffset(TextLineSpan span, int startLineNumber)
                {
                    span.LineNumber += startLineNumber;
                    return span;
                }

                var sourceFileResults =
                   (from hit in textResults.Hits
                    let chunk = hit.Source.Chunk
                    let chunkHit = (hit: chunkIds.GetOrDefault(chunk.Id), chunk.StartLineNumber)
                    where chunkHit.hit != null
                    from highlight in chunkHit.hit.Highlights
                    select new SearchResult()
                    {
                        TextLine = new TextLineSpanResult()
                        {
                            File = hit.Source.File,
                            TextSpan = withOffset(highlight.CreateCopy(), chunkHit.StartLineNumber)
                        }
                    }).ToList<ISearchResult>();

                return new IndexQueryHits<ISearchResult>()
                {
                    Hits = sourceFileResults,
                    Total = textResults.Total
                };
            });
        }

        private static SearchTerms GetSearchTerms(ref SearchArguments arguments, string searchPhrase, out bool annotateRepos)
        {
            var terms = new SearchTerms(searchPhrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(t => t.ToLowerInvariant()).ToList());
            annotateRepos = terms.AllRepoSearch || terms.RepoSearch;
            if (annotateRepos)
            {
                arguments = arguments with
                {
                    SecondaryAccessLevel = RepoAccess.Public
                };
            }

            if (terms.AllRepoSearch)
            {
                terms.Terms.Add("#repo");

                // Allow showing up to 1000 results when returning repo results
                arguments.MaxResults = Math.Max(1000, arguments.MaxResults);

                if (arguments.AccessLevel == null || arguments.AccessLevel > RepoAccess.Internal)
                {
                    // Show even internal repos
                    arguments.AccessLevel = RepoAccess.Internal;
                }
            }

            return terms;
        }

        public record SearchTerms(List<string> Terms)
        {
            public bool IncludeReferencedDefinitions { get; set; } = TryExtractTerm(Terms, CodexConstants.Terms.IncludeReferencedDefinitions);
            public bool IncludeExtensionMembers { get; set; } = TryExtractTerm(Terms, CodexConstants.Terms.IncludeExtensionMembers);
            public long? ConstantValue { get; set; } = TryExtractIntegralValue(Terms);

            public bool AllRepoSearch { get; set; } = TryExtractTerm(Terms, CodexConstants.Terms.AllRepoSearch);
            public bool RepoSearch { get; } = CheckTerm(Terms, "#repo");
        }

        public static bool CheckTerm(List<string> terms, string term)
        {
            return terms.Any(t => t.EqualsIgnoreCase(term));
        }

        public static bool TryExtractTerm(List<string> terms, string term)
        {
            return terms.RemoveAll(t => t.EqualsIgnoreCase(term)) != 0;
        }

        public static long? TryExtractIntegralValue(List<string> terms)
        {
            (long value, int index)? result = null;
            foreach ((var item, int index) in terms.AsSegment().WithIndices())
            {
                if (IntHelpers.TryParseInt<long>(item, out var value))
                {
                    if (result != null)
                    {
                        // Only one integral value is allowed 
                        return null;
                    }

                    result = (value, index);
                }
            }

            if (result != null)
            {
                terms.RemoveAt(result.Value.index);
            }

            return result?.index;
        }

        //private Func<CodexQueryBuilder<IDefinitionSearchModel>, CodexQuery<IDefinitionSearchModel>> GetTermsFilter(
        //    string[] terms,
        //    bool boostOnly = false,
        //    bool allowReferencedDefinitions = false)
        //{
        //    return qcd => qcd.Bool(bq => bq.Filter(GetTermsFilters(terms, boostOnly, allowReferencedDefinitions)));
        //}

        private CodexQuery<IDefinitionSearchModel> GetTermsFilter(
            IStoredFilterInfo filter,
            CodexQueryBuilder<IDefinitionSearchModel> cq,
            SearchTerms terms,
            bool boostOnly = false)
        {
            CodexQuery<IDefinitionSearchModel> query = default;
            foreach (var term in terms.Terms)
            {
                query &= ApplyTermFilter(filter, term.ToLowerInvariant(), cq, boostOnly, terms);
            }

            if (!boostOnly)
            {
                query &= !cq.Term(M.Definition.ExcludeFromDefaultSearch, true);
            }
            else
            {
                //var rankedDefKinds = CodexConstants.DefinitionKindByRank;
                //for (int i = 0, boost = rankedDefKinds.Length; i < rankedDefKinds.Length; i++, boost--)
                //{
                //    var kinds = rankedDefKinds[i];
                //    foreach (var kind in kinds.Values)
                //    {
                //        query |= cq.Term(M.Definition.Kind, kind).Boost(boost);
                //    }
                //}
            }

            return query;
        }

        private CodexQuery<IDefinitionSearchModel> KeywordFilter(string term, CodexQueryBuilder<IDefinitionSearchModel> fq)
        {
            return fq.Term(M.Definition.Keywords, term.ToLowerInvariant());
        }

        private CodexQuery<IDefinitionSearchModel> NameFilter(string term, CodexQueryBuilder<IDefinitionSearchModel> fq, bool boostOnly)
        {
            var terms = term.CreateNameTerm();

            if (boostOnly)
            {
                return fq.Term(M.Definition.ShortName, terms.ExactNameTerm.ToLowerInvariant(), CodexConstants.Boosts.ShortNameBoost);
            }
            else
            {
                return NameFilterCore(fq, terms);
            }
        }

        private CodexQuery<IDefinitionSearchModel> NameFilterCore(CodexQueryBuilder<IDefinitionSearchModel> fq, QualifiedNameTerms terms)
        {
            // This is used by enum search. We search based on the triangulation values so that if enum values shift somewhat we still
            // find the enum value. We then apply a post filter to ensure the value matches.
            if (!string.IsNullOrEmpty(terms.ContainerTerm) && IntHelpers.TryParseInt(terms.RawNameTerm, out long value))
            {
                //return fq.Terms(M.Definition.ConstantValue, value.GetTriangulationValues())
                //    .SetShouldExclude(ds => ds.Definition.ConstantValue != value);
            }

            return fq.Term(M.Definition.ShortName, terms.NameTerm, CodexConstants.Boosts.ShortNameBoost)
                | fq.Term(M.Definition.ShortName, terms.SecondaryNameTerm, CodexConstants.Boosts.ShortNameBoost)
                | fq.Term(M.Definition.AbbreviatedName, terms.NameTerm, CodexConstants.Boosts.ShortNameBoost);
        }

        private CodexQuery<IDefinitionSearchModel> QualifiedNameTermFilters(string term, CodexQueryBuilder<IDefinitionSearchModel> fq, SearchTerms settings)
        {
            // TODO: Should this no-op if ContainerTerm is null/empty? Maybe already does.
            var terms = ParseContainerAndName(term);

            Placeholder.Todo("Bring back this logic?");
            // TEMPORARY HACK: This is needed due to the max length placed on container terms
            // The analyzer should be changed to use path_hierarchy with reverse option
            //if ((terms.ContainerTerm.Length > (CustomAnalyzers.MaxGram - 2)) && terms.ContainerTerm.Contains("."))
            //{
            //    terms.ContainerTerm = terms.ContainerTerm.SubstringAfterFirstOccurrence('.');
            //}

            var query = NameFilterCore(fq, terms)
                & (fq.Term(M.Definition.ContainerQualifiedName, terms.ContainerTerm)
                | fq.Term(M.Definition.ExtensionContainerQualifiedName, terms.ContainerTerm, include: settings.IncludeExtensionMembers));
            return query;
        }

        private CodexQuery<IDefinitionSearchModel> IndexTermFilters(string term, CodexQueryBuilder<IDefinitionSearchModel> fq)
        {
            return Placeholder.Value<CodexQuery<IDefinitionSearchModel>>("Determine how index queries will be represented? Probably as a stored filter");
            //return fq.Term("_index", term.ToLowerInvariant());
        }

        private CodexQuery<IDefinitionSearchModel> ApplyTermFilter(IStoredFilterInfo filter, string term, CodexQueryBuilder<IDefinitionSearchModel> fq, bool boostOnly, SearchTerms terms)
        {
            bool isTagTerm = term.StartsWith("#");

            var d = isTagTerm ? null : NameFilter(term, fq, boostOnly);

            // Trim off exact match quotes which are only used by name term
            term = term.Trim('"').TrimStart('#');
            if (!boostOnly)
            {
                d |= KindFilter(term, fq);

                if (isTagTerm && term == nameof(WellKnownKeywords.entrypoint))
                {
                    d |= KeywordFilter(nameof(WellKnownKeywords.main), fq);
                }
                else
                {
                    d |= KeywordFilter(term, fq);
                }

                if (isTagTerm && term == "type")
                {
                    d |= KindFilter("class", fq)
                        | KindFilter("interface", fq)
                        | KindFilter("struct", fq)
                        | KindFilter("enum", fq)
                        | KindFilter("delegate", fq);
                }

                if (!isTagTerm)
                {
                    d |= QualifiedNameTermFilters(term, fq, terms);
                    d |= IndexTermFilters(term, fq);

                    if (SymbolId.TryGetBinaryValue(term, out _))
                    {
                        // Advanced case where symbol id is mentioned as a term
                        d |= fq.Term(M.Definition.Id, term);
                    }

                    if (filter.IsPossibleProject(term))
                    {
                        // TODO: Exclude if term doesn't appear in ProjectReferenceSketch
                        d |= fq.Term(M.Definition.ProjectId, term);
                        d |= fq.Term(M.Definition.ExtensionProjectId, term, include: terms.IncludeExtensionMembers);
                    }

                }
            }

            return d;
        }

        private static CodexQuery<IDefinitionSearchModel> KindFilter(string term,
            CodexQueryBuilder<IDefinitionSearchModel> fq)
        {
            return fq.Term(M.Definition.Kind, term);
        }

        private bool GetValueFromEntry((DateTime resolveTime, string repositorySnapshotId) resolvedEntry, out string aliasId)
        {
            var age = DateTime.UtcNow - resolvedEntry.resolveTime;
            if (age > Configuration.CachedAliasIdRetention)
            {
                // Entry is too old, need resolve the alias id
                aliasId = null;
                return false;
            }

            aliasId = resolvedEntry.repositorySnapshotId;
            return true;
        }

        protected OneOrMany<IMappingField<T>> OneOrMany<T>(params IMappingField<T>[] fields)
        {
            return fields;
        }

        protected abstract Task<StoredFilterSearchContext<TClient>> GetStoredFilterContextAsync(ContextCodexArgumentsBase arguments);

        protected virtual async Task<TResponse> UseClientCoreAsync<TResponse, TResult>(ContextCodexArgumentsBase arguments, Func<StoredFilterSearchContext<TClient>, TResponse, Task<TResult>> useClient)
            where TResponse : IndexQueryResponse<TResult>, new()
        {
            var response = new TResponse();
            var startTime = TimestampUtilities.Timestamp;

            try
            {
                var sfContext = await GetStoredFilterContextAsync(arguments);
                if (arguments.SecondaryAccessLevel != null && arguments.SecondaryAccessLevel != sfContext.AccessLevel)
                {
                    var newArgs = arguments with { AccessLevel = arguments.SecondaryAccessLevel };
                    sfContext.SecondaryFilter = await GetStoredFilterContextAsync(newArgs);
                }

                response.RawQueries = sfContext.Requests;
                var result = await Task.Run(() => useClient(sfContext, response));
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.Error = "Exception - " + ex.ToString();
            }
            finally
            {
                response.Duration = TimestampUtilities.Timestamp - startTime;
            }

            return response;
        }

        protected virtual Task<IndexQueryHitsResponse<T>> UseClient<T>(ContextCodexArgumentsBase arguments, Func<StoredFilterSearchContext<TClient>, IndexQueryHitsResponse<T>, Task<IndexQueryHits<T>>> useClient)
        {
            return UseClientCoreAsync(arguments, useClient);
        }

        protected virtual Task<IndexQueryResponse<T>> UseClientSingle<T>(ContextCodexArgumentsBase arguments, Func<StoredFilterSearchContext<TClient>, IndexQueryResponse<T>, Task<T>> useClient)
        {
            return UseClientCoreAsync(arguments, useClient);
        }
    }
}
