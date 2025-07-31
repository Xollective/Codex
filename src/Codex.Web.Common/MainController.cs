using Codex.ObjectModel;
using Codex.Sdk.Search;
using Codex.Uno.Shared;
using Codex.Utilities;
using Codex.Web.Common;
using Codex.Web.Wasm;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Codex.View
{
    public class MainController
    {
        public static MainController App { get; } = new MainController();

        public ICodex CodexService { get; set; }
        //public SetTarget<ICodex> CodexServiceTarget { get; } = new();

        public SetTarget<string> StartArguments { get; } = new();

        public IViewModelController Controller { get; set; }

        public ViewModelDataContext ViewModel => Controller.ViewModel;

        private SemaphoreSlim SearchMutex = TaskUtilities.CreateMutex();
        private string _searchString;

        public MainController()
        {
            Controller = new WebViewModelController(this);
        }

        public CodexPage GetPage()
        {
            return new CodexPage(CodexService, this, ViewModel);
        }

        public async Task SearchTextChanged(string searchString)
        {
            searchString = searchString.Trim();
            _searchString = searchString;

            if (searchString.StartsWith("?"))
            {
                var address = ViewModelAddress.Parse(searchString);
                await address.NavigateAsync(this, infer: false);
                return;
            }

            using var scope = await SearchMutex.AcquireAsync();
            if (_searchString != searchString)
            {
                return;
            }

            if (searchString.Length < 3)
            {
                Controller.SetSearchInfo("Enter at least 3 characters.");
                return;
            }

            var response = await CodexService.SearchAsync(new SearchArguments()
            {
                SearchString = searchString.Trim('`'),
                TextSearch = searchString.StartsWith('`')
            });

            Controller.OnSearchResponse(searchString, response);
        }

        public Task GoToSpanExecuted(IProjectFileScopeEntity lineSpan, TargetSpan targetSpan)
        {
            return GoToSpanExecuted(GetSourceArguments.From(lineSpan), targetSpan);
        }

        public async Task GoToSpanExecuted(GetSourceArguments arguments, TargetSpan? targetSpan)
        {
            var currentSource = ViewModel.RightPane?.SourceFile;
            if (currentSource?.ProjectId == arguments.ProjectId && currentSource?.ProjectRelativePath == arguments.ProjectRelativePath)
            {
                if (targetSpan != null)
                {
                    Controller.GoToSource(currentSource, targetSpan.Value);
                }

                return;
            }

            var response = await CodexService.GetSourceAsync(arguments);

            if (response.Error != null)
            {
                Controller.ShowRightPaneError(response);
            }
            else
            {
                Controller.GoToSource(response.Result, targetSpan);
            }
        }

        public async Task ShowDocumentExplorer(GetSourceArguments arguments)
        {
            var source = ViewModel.RightPane?.SourceFile;
            if ((source?.ProjectId) != arguments.ProjectId
                || (source?.ProjectRelativePath) != arguments.ProjectRelativePath
                // No definitions means this is a synthesized source file just to indicate
                // that the file is currently open in the page. We still need to query the
                // real source in order to perform outline.
                || (source.Definitions?.Count ?? 0) == 0)
            {
                var response = await CodexService.GetSourceAsync(arguments);
                if (response.Error != null)
                {
                    Controller.ShowRightPaneError(response);
                    return;
                }

                source = response.Result;
            }

            Controller.ShowDocumentOutline(source);
        }

        public Task FindAllReferencesExecuted(IReferenceSymbol symbol)
        {
            FindAllReferencesArguments arguments = new FindAllReferencesArguments()
            {
                ProjectId = symbol.ProjectId,
                SymbolId = symbol.Id.Value,
            };

            return FindAllReferences(arguments);
        }

        public Task ShowNamespaceExplorer(GetProjectArguments arguments)
        {
            arguments.AddressKind = Storage.BlockLevel.AddressKind.TopLevelDefinitions;
            return ShowProjectExplorer(arguments);
        }

        public async Task ShowProjectExplorer(GetProjectArguments arguments)
        {
            var response = await CodexService.GetProjectAsync(arguments);
            if (response.Error != null)
            {
                Controller.SetSearchInfo(response.Error);
            }
            else
            {
                Controller.ShowProjectExplorer(response.Result);
            }
        }

        public async Task FindAllReferences(FindAllReferencesArguments arguments)
        {
            var response = await CodexService.FindAllReferencesAsync(arguments);


            if (response.Error != null)
            {
                Controller.SetSearchInfo(response.Error);
            }
            else if (response.Result?.Hits == null || response.Result.Hits.Count == 0)
            {
                Controller.SetSearchInfo("No results found.");
            }
            else
            {
                Controller.OnReferencesResponse(response);
            }
        }

        public async Task GoToDefinitionExecuted(FindDefinitionLocationArguments arguments)
        {
            var response = await CodexService.FindDefinitionLocationAsync(arguments);

            if (response.Error != null || response.Result.Hits.Count == 0)
            {
                Controller.ShowRightPaneError(response);
            }
            else if (arguments.ShouldUseReferencesResult(response.Result.Hits))
            {
                // Show definitions in left pane
                Controller.OnReferencesResponse(response);
            }
            else
            {
                IReferenceSearchResult reference = response.Result.Hits[0];
                await GoToSpanExecuted(reference.File, new TargetSpan(reference.ReferenceSpan.LineNumber, SymbolId: arguments.SymbolId));
            }
        }

        public Task DisplayOverview(OverviewKind mode)
        {
            return Controller.DisplayOverview(mode);
        }

        public async Task ShowRepositoriesSummary(GetRepositoryHeadsArguments arguments)
        {
            var response = await CodexService.GetRepositoryHeadsAsync(arguments);

            Controller.ShowRepositoriesSummary(response);
        }

        public async Task ShowProjectReferencesFile(string projectId)
        {
            var response = await CodexService.GetProjectAsync(new GetProjectArguments()
            {
                ProjectId = projectId,
            });
        }

        //public async void UpdateRightPane(Func<Task<RightPaneViewModel>> getViewModel)
        //{
        //    var rightViewModel = await getViewModel();
        //    ViewModel.RightPane = rightViewModel;
        //}
    }
}
