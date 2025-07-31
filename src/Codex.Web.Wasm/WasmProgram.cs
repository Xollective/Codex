using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codex;
using Codex.Lucene.Search;
using Codex.Sdk.Search;
using Codex.Search;
using Codex.Utilities;
using Codex.View;
using Codex.Web.Common;
using Codex.Web.Wasm;
using Codex.Workspaces;

internal record WasmProgram(string StartUrl) : WebProgramBase("/usr/share/autoindex")
{
    public override MainController App => MainController.App;
    public override JSViewModelController Controller { get; } = new JSViewModelController();
    public override IHttpClient Client => JSViewModelController.Client;
    public override HttpMessageHandler MessageHandler => ((HttpClientWrapper)Client).Handler;

    public static async Task Main()
    {
        try
        {
            Console.WriteLine("Started Main");

            await BrowserAppContext.InitializeAsync();

            string startUrl = CodexJsRuntime.GetHRef().ToString();
            Console.WriteLine($"Hello, Browser! StartUrl:({startUrl}) from: {Thread.CurrentThread.ManagedThreadId} HasSyncContext={BrowserAppContext.SynchronizationContext != null}]");
            var address = ViewModelAddress.Parse(startUrl);

            await new WasmProgram(startUrl).RunAsync(address);

            await BrowserAppContext.SwitchToMainThread();
            CodexJsRuntime.Navigate(startUrl);

            // Keep the app alive. There were some issues before when we allowed Main to complete.
            await RunTimer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in Main:\n{ex}");
            throw;
        }
        finally
        {
            Console.WriteLine("Finished Main");
        }
    }

    protected override async Task<string> GetIndexSourceUrl()
    {
        var url = await base.GetIndexSourceUrl();
        if (url.Contains(":") || url.AsSpan().IndexOfAny(@"/\") == 0)
        {
            return url;
        }
        else
        {
            var uriBuilder = new UriBuilder(StartUrl);
            uriBuilder.Path = PathUtilities.UriCombine(uriBuilder.Path, url.Replace('\\', '/'));
            return uriBuilder.Uri.ToString();
        }

    }

    public static async Task RunTimer()
    {
        while (true)
        {
            //Placeholder.Trace2();
            await Task.Delay(5000);
        }
    }

    public static async void RunThreadDiag()
    {
        while (true)
        {
            await Task.Delay(5000);
            Console.WriteLine("Printing thread diagnostics:");

            foreach (var diag in CachingPageFileProvider.ThreadDiags.Values)
            {
                Console.WriteLine(diag);
            }
        }
    }

    private static void RunTasks()
    {
        for (int i = 0; i < 10; i++)
        {
            Task.Run(async () =>
            {
                try
                {
                    Thread.Sleep(1000);

                    Console.WriteLine($"Running thread with id: {Thread.CurrentThread.ManagedThreadId} on '{MyClass.GetHRef()}'");

                    var sample = await JSViewModelController.Client.GetStringAsync("sample.txt").ConfigureAwait(false);
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {sample}");

                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception thrown in Main: " + ex);
                }
            });
        }
    }
}

public partial class MyClass
{
    [JSExport]
    internal static string Greeting()
    {
        var text = $"Greetings from C# at {GetHRef()}";
        Console.WriteLine(text);
        return text;
    }

    //[JSImport("window.location.href", "main.js")]
    //internal static partial string GetHRef();

    internal static string GetHRef() => "hello world";
}
