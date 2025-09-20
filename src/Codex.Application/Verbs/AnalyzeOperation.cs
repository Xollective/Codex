using Codex.Analysis;
using Codex.Analysis.External;
using Codex.Analysis.Files;
using Codex.Analysis.Managed;
using Codex.Analysis.Projects;
using Codex.Analysis.Xml;
using Codex.Import;
using Codex.Lucene;
using Codex.Lucene.Search;
using Codex.Sdk;
using Codex.Search;
using Codex.Storage;
using Codex.Storage.Store;
using Codex.Utilities;
using static Codex.CodexConstants;

namespace Codex.Application.Verbs;

[Verb("analyze", HelpText = "Analyze a repo and emit data to a directory.")]
public record AnalyzeOperation : IndexReadOperationBase
{
    [Option("noMsBuildLocator", Default = false, HelpText = "Disable loading MSBuild locator.")]
    public bool DisableMsBuildLocator { get; set; } = false;

    [Option("noMsBuild", HelpText = "Disable loading solutions and projects using msbuild workspace.")]
    public bool DisableMsBuild { get; set; }

    [Option("loadMsBuildFiles", Default = true, HelpText = "Indicates whehther msbuild files should be analyzed for MSBuild semantic information")]
    public bool UseMsBuildFileAnalyzer { get; set; } = true;

    [Option("evalMsBuild", Default = false, HelpText = "Indicates whehther msbuild files should be evaluated for MSBuild semantic information")]
    public bool EvaluateMsBuildFiles { get; set; } = false;

    [Option("projectMode", HelpText = "Uses project indexing mode.")]
    public bool ProjectMode { get; set; }

    [Option("noScan", HelpText = "Disable scanning enlistment directory.")]
    public bool DisableEnumeration { get; set; }

    [Option("clean", HelpText = "Reset target index directory when using -save option.")]
    public bool Clean { get; set; }

    [Option("emitBuildTags", HelpText = "Indicates whether build tags should be emitted.")]
    public bool EmitBuildTags { get; set; }

    public bool AnalysisOnly { get; set; }

    public bool DryRun { get; set; }

    [Option("disableParallelFiles", HelpText = "Disables use of parallel file analysis.")]
    public bool DisableParallelFiles { get; set; }

    [Option('f', "file", HelpText = "Specifies single file to analyze.")]
    public string FileToAnalyze { get; set; }

    [Option('g', "disableDetectGit", Default = true, HelpText = "Disables use of LibGit2Sharp to detect git commit and branch.")]
    public bool DetectGit { get; set; } = true;

    [Option("test", HelpText = "Indicates that save should use test mode which disables optimization.")]
    public bool DisableOptimization { get; set; }

    public bool Finalize { get; set; } = true;

    [Option('s', "solution", HelpText = "Paths to solutions to analyze.")]
    public IList<string> SolutionPaths { get; set; } = new List<string>();

    [Option("projectDataSuffix", HelpText = "Specifies the suffix for saving project data.")]
    public string ProjectDataSuffix { get; set; }

    [Option("decAsm", HelpText = "Search paths for files/folders to analyze decompilation.")]
    public IList<string> DecompilationSearchPaths { get; set; } = new List<string>();

    [Option("metAsm", HelpText = "Search paths for files/folders to analyze metadata as source.")]
    public IList<string> MetadataAsSourceSearchPaths { get; set; } = new List<string>();

    [Option('c', "compilerArgumentFile", HelpText = "Search paths for files specifying compiler arguments.")]
    public IList<string> CompilerArgumentsSearchPaths { get; set; } = new List<string>();

    [Option('b', "binLogSearchDirectory", HelpText = "Adds a bin log file or directory to search for binlog files.")]
    public IList<string> BinLogSearchPaths { get; set; } = new List<string>();

    [Option("complog", HelpText = "Adds a compiler log file or directory to search for complog files.")]
    public IList<string> CompilerLogSearchPaths { get; set; } = new List<string>();

    [Option("projectData", HelpText = "Specifies one or more project data directories.")]
    public IList<string> ProjectDataDirectories { get; set; } = new List<string>();

    [Option('e', "extData", HelpText = "Specifies one or more external data directories")]
    public IList<string> ExternalDataDirectories { get; set; } = new List<string>();

    [Option('n', "name", HelpText = "Name of the repository")]
    public string RepoName { get; set; }

    // TODO: Extract from git
    [Option('u', "repoUrl", HelpText = "The URL of the repository being indexed.")]
    public string RepoUrl { get; set; }

    [Option("buildUrl", HelpText = "The URL of the current build.")]
    public string BuildUrl { get; set; }

    [Option('p', "path", Required = true, HelpText = "Path to the repo to analyze.")]
    public string RootDirectory { get; set; }

    [Option("project", HelpText = "The path to the project file when running ProjectData Scenario.")]
    public string ProjectPath { get; set; }

    [Option("scenario", HelpText = "The analysis scenario.")]
    public AnalysisScenario Scenario { get; set; } = AnalysisScenario.None;

    //[Option("format", HelpText = "The format for outputs.")]
    public DirectoryStoreFormat OutputFormat { get; set; } = DirectoryStoreFormat.Json;

    public enum AnalysisScenario
    {
        None,
        SourceIndexer,
        ProjectData
    }

    private bool RequireProjectsExist { get; set; }

    protected override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        RepoName = RepoName?.Trim();
        RepoUrl = RepoUrl?.Trim();
        RootDirectory = Path.GetFullPath(RootDirectory);

        if (Clean && Directory.Exists(OutputDirectory))
        {
            PathUtilities.ForceDeleteDirectory(OutputDirectory);
        }

        if (Scenario == AnalysisScenario.ProjectData)
        {
            DetectGit = false;
            ProjectMode = true;
            DisableMsBuild = true;
            UseMsBuildFileAnalyzer = false;
            DisableEnumeration = true;
        }

        if (OutputStore == null)
        {
            if (OutputDirectory == null)
            {
                AnalysisOnly = true;
                OutputStore = new NullCodexRepositoryStore();
            }
            else
            {
                OutputStore = Out.Var(out var dirStore, new DirectoryCodexStore(OutputDirectory, Logger)
                {
                    Clean = Clean,
                    DisableOptimization = DisableOptimization,
                    QualifierSuffix = ProjectDataSuffix,
                    Format = OutputFormat,
                });
            }
        }
    }

    protected override async ValueTask<int> ExecuteAsync()
    {
        if (!DisableMsBuild && !DisableMsBuildLocator)
        {
            MSBuildHelper.RegisterMSBuild();
        }

        try
        {
            await OutputStore.InitializeAsync();

            await RunRepoImporterAsync();

            await OutputStore.FinalizeAsync();

            await CleanupAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error indexing {RepoName}: {ex.Message}");
            Console.WriteLine(ex.ToString());
            return -1;
        }
    }

    private async Task RunRepoImporterAsync()
    {
        PipelineUtilities.AzureDevOps.AddBuildTag(BuildTags.CodexEnabled, print: EmitBuildTags);
        PipelineUtilities.AzureDevOps.AddBuildTag(BuildTags.FormatVersion, print: EmitBuildTags);

        RepoName ??= Path.GetFileName(RootDirectory);

        var targetIndexName = StoreUtilities.GetTargetIndexName(RepoName);
        string[] file = new string[0];

        SolutionPaths = SolutionPaths.Where(s => !string.IsNullOrEmpty(s)).Select(s => Path.GetFullPath(s)).ToList();

        RequireProjectsExist = true;

        string assembly = null;
        string package = null;

        if (File.Exists(RootDirectory))
        {
            if (RootDirectory.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                RootDirectory.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                assembly = RootDirectory;
            }

            if (RootDirectory.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                package = RootDirectory;
            }

            DetectGit = false;

            file = new[] { RootDirectory };
            RootDirectory = Path.GetDirectoryName(RootDirectory);
            RequireProjectsExist = false;
        }

        var logger = Logger;

        var gitSubmodules = new List<string>();
        var gitSubmodulesFilter = new DirectoryFileSystemFilter(gitSubmodules) { ExcludingRoots = true };
        FileSystem fileSystem = new CachingFileSystem(
            new UnionFileSystem(file.Union(SolutionPaths),
                new RootFileSystem(RootDirectory,
                    new MultiFileSystemFilter(
                        new DirectoryFileSystemFilter(@"\.", ".sln"),
                        gitSubmodulesFilter,

                        new GitIgnoreFilter() { Logger = logger },

                        // Filter out files from being indexed specified by the .cdxignore file
                        // This is used to ignore files which are not specified in the .gitignore files
                        new GitIgnoreFilter(".cdxignore") { Logger = logger },

                        BinaryFileSystemFilter.Default))
                {
                    DisableEnumeration = ProjectMode || DisableEnumeration || file.Length != 0
                }));

        List<RepoProjectAnalyzer> projectAnalyzers = new List<RepoProjectAnalyzer>(GetProjectAnalyzers());

        if (assembly != null)
        {
            fileSystem = new SystemFileSystem();

            projectAnalyzers.Clear();
            projectAnalyzers.Add(new MetadataAsSourceProjectAnalyzer(file));
        }

        if (package != null)
        {
            fileSystem = new ZipFileSystem(package);

            projectAnalyzers.Clear();
            projectAnalyzers.Add(new MetadataAsSourceProjectAnalyzer(new string[0])
            {
                ScanAssemblies = true
            });
        }

        var repoData = new DirectoryRepositoryStoreInfo(new RepositoryStoreInfo(
            repository: new Repository()
            {
                Name = RepoName,
                SourceControlWebAddress = RepoUrl,
            },
            commit: new Commit()
            {
                RepositoryName = RepoName,
                CommitId = targetIndexName,
                DateUploaded = DateTime.UtcNow,
                BuildUri = BuildUrl ?? PipelineUtilities.AzureDevOps.TryGetBuildUrl()
            },
            branch: new Branch()
            {
                HeadCommitId = targetIndexName,
            }));

        ISourceControlInfoProvider contentIdProvider = null;
        if (DetectGit)
        {
            var gitProvider = GitHelpers.DetectGit(repoData, RootDirectory, logger);

            // Source indexer uses generated project files. This is enabled so that
            // project files will get pulled from 
            if (Scenario == AnalysisScenario.SourceIndexer && gitProvider != null)
            {
                fileSystem = new GitFileSystem(RootDirectory, gitProvider.Repository, fileSystem, Logger,
                    shouldUseGit: filePath =>
                    {
                        // Only use git for project files
                        return !string.IsNullOrEmpty(filePath) && filePath.EndsWithIgnoreCase("proj");
                    });
            }

            gitSubmodules.Add(gitProvider.GetSubmodulePaths().Select(s =>
                PathUtilities.NormalizePath(Path.Combine(RootDirectory, s)).EnsureTrailingSlash()));

            contentIdProvider = gitProvider;
        }

        if (SourceControlUri.TryParse(repoData.Repository.SourceControlWebAddress, out var parsedRepoUrl))
        {
            var repoName = parsedRepoUrl.GetRepoName();
            repoData.Repository.Name = repoName;
            repoData.Commit.RepositoryName = repoName;

            PipelineUtilities.AzureDevOps.AddBuildTag(parsedRepoUrl.GetBuildTag(), print: EmitBuildTags);
        }

        RepoName = repoData.Repository.Name;

        ICodexRepositoryStore analysisTarget = await OutputStore.CreateRepositoryStore(repoData);

        if (contentIdProvider != null)
        {
            analysisTarget = new StandardAnalysisDecorator(analysisTarget, contentIdProvider);
        }

        if (ProjectDataDirectories.Count != 0)
        {
            var preAnalysisAnalyzer = new PreAnalyzedRepoProjectAnalyzer(ProjectDataDirectories);

            foreach (var projectDataDirectory in ProjectDataDirectories)
            {
                var interceptorStore = preAnalysisAnalyzer.CreateRepositoryStore(analysisTarget);
                var directoryStore = new DirectoryCodexStore(projectDataDirectory, logger)
                {
                    ReadProjectsOnly = true,
                    StoreInfo = repoData
                };

                await directoryStore.ReadAsync(interceptorStore);
            }

            projectAnalyzers.Insert(0, preAnalysisAnalyzer);
        }

        AnalysisServices analysisServices = new AnalysisServices(
                targetIndexName,
                fileSystem,
                analyzers: new RepoFileAnalyzer[]
                {
                        new SolutionFileAnalyzer(),
                        UseMsBuildFileAnalyzer
                            ? new MSBuildFileAnalyzer()
                            {
                                AllowEvaluation = EvaluateMsBuildFiles && !(DisableMsBuild || DisableMsBuildLocator)
                            }
                            : new XmlFileAnalyzer(MSBuildFileAnalyzer.DefaultMsBuildFileExtensions),
                        // This indexer allows an external tool to write out codex spans for importing.
                        new ExternalRepoFileAnalyzer(ExternalDataDirectories.ToArray()),
                        new XmlFileAnalyzer(
                            ".ds",
                            ".xml",
                            ".config",
                            ".settings"),
                })
        {
            RepositoryStore = analysisTarget,
            Logger = logger,
            ParallelProcessProjectFiles = ProjectMode && !DisableParallelFiles
        };

        if (ProjectMode)
        {
            // Exclude repo project from analysis for project mode
            analysisServices.IncludeRepoProject = analysisServices.IncludeRepoProject.And(
                p => p.ProjectKind != ProjectKind.Repo);
        }

        analysisServices.AnalysisIgnoreProjectFilter += gitSubmodulesFilter;
        analysisServices.AnalysisIgnoreProjectFilter += new DelegateFileSystemFilter()
        {
            ShouldIncludeFile = (fs, filePath) => !filePath.EndsWithIgnoreCase("wpftmp.csproj")
        };

        if (AnalysisOnly)
        {
            analysisServices.AnalysisIgnoreProjectFilter += new RootFileSystemFilter(RootDirectory);
        }

        if (FileToAnalyze != null)
        {
            analysisServices.AnalysisIgnoreFileFilter = analysisServices.AnalysisIgnoreFileFilter.Combine(new DelegateFileSystemFilter()
            {
                ShouldIncludeFile = (fs, filePath) => filePath.IndexOf(FileToAnalyze, StringComparison.OrdinalIgnoreCase) >= 0
            });
        }

        RepositoryImporter importer = new RepositoryImporter(RepoName,
            RootDirectory,
            analysisServices)
        {
            AnalyzerDatas = projectAnalyzers.Select(a => new AnalyzerData() { Analyzer = a }).ToList()
        };

        await importer.Import(finalizeImport: Finalize);
    }

    protected virtual IEnumerable<RepoProjectAnalyzer> GetProjectAnalyzers()
    {
        var includedSolutions = SolutionPaths.ToArray();
        if (DisableMsBuild)
        {
            includedSolutions = new string[0];
        }

        return new RepoProjectAnalyzer[]
        {
            new MetadataAsSourceProjectAnalyzer(MetadataAsSourceSearchPaths),
            new MetadataAsSourceProjectAnalyzer(DecompilationSearchPaths)
            {

            },
            new CompilerArgumentsProjectAnalyzer(Logger, CompilerArgumentsSearchPaths.ToArray())
            {
                RequireProjectFilesExist = RequireProjectsExist
            },
            new BinLogProjectAnalyzer(Logger, BinLogSearchPaths.ToArray())
            {
                RequireProjectFilesExist = RequireProjectsExist
            },
            new CompilerLogProjectAnalyzer(Logger, CompilerLogSearchPaths.ToArray())
            {
                RequireProjectFilesExist = RequireProjectsExist
            },
            new MSBuildSolutionProjectAnalyzer(
                includedSolutions: includedSolutions,
                // Source indexer uses generated standalone projects which
                // can be polluted by including directory build files. So exclude them.
                disableDirectoryBuildFiles: Scenario == AnalysisScenario.SourceIndexer)
            {
                RequireProjectFilesExist = RequireProjectsExist
            }
        };
    }
}
