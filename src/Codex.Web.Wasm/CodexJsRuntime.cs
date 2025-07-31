using System.Runtime.InteropServices.JavaScript;
using System;
using Codex.Utilities;
using System.Threading.Tasks;

namespace Codex.Web.Wasm;

public partial class CodexJsRuntime
{
    public const string ModuleName = "CodexJsRuntime";

    public static async Task StartAsync()
    {
        await JSHost.ImportAsync(moduleName: ModuleName, "../ts/js/CodexJsRuntime.js");

        await SetupAsync();
    }

    [JSImport("SetLeftPane", ModuleName)]
    internal static partial void SetLeftPane(string innerHtml);

    [JSImport("SetRightPane", ModuleName)]
    internal static partial void SetRightPane(string innerHtml);

    [JSImport("GoToSymbolOrLineNumber", ModuleName)]
    internal static partial void GoToSymbolOrLineNumber(int line, string symbol);

    [JSImport("GetHRef", ModuleName)]
    public static partial string GetHRef();

    [JSImport("Navigate", ModuleName)]
    public static partial void Navigate(string url);

    [JSImport("SetNavigationBar", ModuleName)]
    internal static partial string SetNavigationBar(string title, string address);

    [JSImport(nameof(SetupAsync), ModuleName)]
    internal static partial Task SetupAsync();

    internal static Uri GetBaseAddress()
    {
        var href = new Uri(GetHRef()).GetLeftPart(UriPartial.Path);
        Console.WriteLine("GetHRef: " + href);
        href = href.TrimEndIgnoreCase("index.html");
        Console.WriteLine("GetHRef: " + href);
        return new Uri(href);
    }
}