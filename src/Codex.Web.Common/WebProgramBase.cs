using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Codex.Lucene.Search;
using Codex.Sdk.Search;
using Codex.Search;
using Codex.Utilities;
using Codex.View;
using Codex.Web.Wasm;
using Codex.Workspaces;

namespace Codex.Web.Common;

public record WebProgramBase(string AutoIndexRoot) : IRepositoryIndexer
{
    public virtual MainController App { get; } = new MainController();
    public virtual IHttpClient Client { get; set; } = new HttpClientWrapper();

    public string IndexSourceUrl { get; set; }
    public virtual HttpMessageHandler MessageHandler { get; } = new HttpClientHandler();
    public virtual WebViewModelController Controller { get; } = new WebViewModelController();

    public async Task RunAsync(ViewModelAddress? address)
    {
        Controller.RepositoryIndexer = this;

        var codex = await GetCodexAsync(address);
        App.Controller = Controller;
        App.CodexService = codex;
        //if (address != null)
        //{
        //    await address.NavigateAsync(App, ViewModelAddress.InferMode.Startup);
        //}
    }

    public async Task<ICodex> GetCodexAsync(ViewModelAddress? address)
    {
        var codex = await GetCodexAsync();

        return codex;
    }

    public async Task<ICodex> GetCodexAsync()
    {
        LuceneConfiguration configuration;
        string indexSourceUrl = IndexSourceUrl ?? await GetIndexSourceUrl();
        Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {indexSourceUrl}");
        indexSourceUrl = indexSourceUrl.Trim();
        {
            Console.WriteLine("Get configuration");
            AsyncOut<PagingDirectoryInfo> directoryInfo = new AsyncOut<PagingDirectoryInfo>();
            configuration = await PagingHelpers.CreatePagingConfigurationAsync(
                indexSourceUrl,
                configuration => configuration with
                {
                    Info = directoryInfo,
                    CacheLimit = 100
                });

            Console.WriteLine($"Got configuration: [{directoryInfo.Value.Entries.Count}]");
        }

        configuration.SourceTextRetriever = new BrowserSourceTextRetriever(Client);
        configuration.DefaultAccessLevel = RepoAccess.Internal;
        var codex = new LuceneCodex(configuration);

        return codex;
    }

    protected virtual async Task<string> GetIndexSourceUrl()
    {
        return await Client.GetStringAsync("sources.txt");
    }

    internal record BrowserSourceTextRetriever(IHttpClient BrowserClient) : HttpClientSourceTextRetriever
    {
        protected override async ValueTask<(HttpResponseMessage response, string content)> GetStringAsync(HttpRequestMessage request)
        {
            var response = BrowserClient.SendMessage(request);
            return (response, response.Content.ReadAsStream().ReadAllText());
        }
    }

    public async Task IndexRepository(SourceControlUri[] repoUris)
    {
        {
            string message = App.CodexService is UpdatableCodex
                ? "No remote index service available"
                : "Local codex is not updatable";
            throw new NotSupportedException(message);
        }
    }
}