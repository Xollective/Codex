using System.Runtime.InteropServices.JavaScript;
using System;
using System.Threading;
using Codex.Utilities;
using System.Threading.Tasks;
using Codex.Web.Wasm;
using Codex;
using Codex.Web.Common;

namespace System;

public partial class BrowserAppContext
{
    public static SynchronizationContext SynchronizationContext { get; private set; }

    public static int MainThreadId { get; private set; }

    public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

    public static async Task InitializeAsync(bool isSingleThreaded = false)
    {
        await CodexJsRuntime.StartAsync();

        var baseAddress = CodexJsRuntime.GetBaseAddress();
        var client = GetClient(default);

        client.BaseAddress = baseAddress;

        SdkFeatures.GetClient = GetClient;
        SdkFeatures.HttpClient = client;

        SynchronizationContext = SynchronizationContext.Current;
        MainThreadId = Thread.CurrentThread.ManagedThreadId;

    }

    private static IInnerHttpClient GetClient(HttpClientKind kind)
    {
        //var client = new HttpClientWrapper(new BrowserHttpHandler());
        //var client = new BrowserHttpHandler();
        var client = new BrowserHttpClientWrapper();
        return client;
    }

    public static TaskUtilities.SynchronizationContextAwaitable SwitchToMainThread()
    {
        return SynchronizationContext.SwitchTo();
    }
}