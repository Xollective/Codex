#nullable enable

using Codex;
using Codex.View;
using Codex.Web.Wasm;

namespace Codex.Web.Common;

public record PageState
{
    public string? LoginId { get; set; }
    public bool IsInitialized { get; set; }
    public int Version { get; set; }
    public string? Address { get; set; }
    public IProjectFileScopeEntity? RightSource { get; set; }
}

public record PageResult
{
    public string? LeftPaneHtml { get; set; }
    public string? RightPaneHtmlLink { get; set; }
    public string? RightPaneHtml { get; set; }
    public int? Line { get; set; }
    public string? Symbol { get; set; }
    public string? SearchString { get; set; }
    public string? Title { get; set; }
    public PageState PageState { get; set; } = new PageState();
    public string? Url { get; set; }
}

public record PageRequest
{
    public string? Url { get; set; }
    public string? SearchString { get; set; }
    public PageState? PageState { get; set; }

    public bool TryGetCurrentAddress(out string currentAddress)
    {
        return (currentAddress = (PageState?.Address ?? Url)!) != null;
    }

    public async Task<PageResult> NavigateAsync(MainController app, bool log = false)
    {
        var controller = app.Controller as WebViewModelController;
        var result = new PageResult() { PageState = PageState ?? new PageState() };
        controller?.SetContextualResult(result);

        if (TryGetCurrentAddress(out string currentAddress))
        {
            app.Controller.ViewModel.NavigationBar = app.Controller.ViewModel.NavigationBar with
            {
                Address = ViewModelAddress.Parse(currentAddress)
            };
        }

        ViewModelAddress? address = null;
        if (Url != null)
        {
            if (log) Console.WriteLine($"Controller.Navigate: '{Url}'");

            if (Features.HideDefaultBranding && Url.StartsWith("#"))
            {
                // Navigating to section in home page. Redirect to main site.
                return new PageResult()
                {
                    Url = "https://ref12.io/" + Url
                };
            }

            address = ViewModelAddress.Parse(Url);

            if (log) Console.WriteLine($"Controller.Navigate: '{Url}' (left: {address.leftPaneMode}, right: {address.rightPaneMode})");
        }
        else if (SearchString != null)
        {
            if (log) Console.WriteLine($"Search '{SearchString}'");

            address = ViewModelAddress.Search(SearchString);
        }

        if (address != null)
        {
            var infer = PageState?.IsInitialized != true ? ViewModelAddress.InferMode.Startup : ViewModelAddress.InferMode.Default;
            await address.NavigateAsync(app, infer);
        }

        if (log) Console.WriteLine($"IsSame '{object.ReferenceEquals(result, controller?.Result)}'");
        void printState()
        {
            if (log) Console.WriteLine($"Left: '{result.LeftPaneHtml?.Length}' Right: '{result.RightPaneHtml?.Length}'");
        }

        printState();
        controller?.UpdateState();
        printState();
        return result;
    }
}