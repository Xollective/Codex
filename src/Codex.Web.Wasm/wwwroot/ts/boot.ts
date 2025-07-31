import { CodexWeb } from "./csdef";

var CodexAppRole = "UI";


var pageStateVersion = 0;

var pageState: CodexWeb.PageState = {
   version: pageStateVersion
};

var updateState = async function (requestJson) {
    return {};
}

window.onpopstate = (event) => {
    if (event) {
        if (!event.state) {
            pageState.isInitialized = false;
        }
        codexClient.Navigate(event.state || window.location.href, true);
    }
}

async function callPageController(data: CodexWeb.PageRequest, isPopState?: boolean) {
    ensureSearchBox();
    var priorSearchString = searchBox.value;

    pageStateVersion++;
    pageState.version = pageStateVersion;
    pageState.address = window.location.href;
    data.pageState = pageState;

    const responseData: CodexWeb.PageResult = await updateState(JSON.stringify(data));
    if (responseData.pageState && responseData.pageState.version) {
        if (responseData.pageState.version < pageStateVersion) {
            // this is a result from a prior call coming in after another
            // call was already made. Just throw away the results.
            return;
        }
    }

    if (responseData.url) {
        window.location.href = responseData.url;
    }

    if (responseData.leftPaneHtml) {
        CodexPage.setLeftPane(responseData.leftPaneHtml);
    }

    if (responseData.rightPaneHtmlLink)
    {
        try {
            const response = await fetch(responseData.rightPaneHtmlLink);
            responseData.rightPaneHtml = await response.text();
        } catch (error) {
            console.error("Error fetching right pane HTML:", error);
            responseData.rightPaneHtml = `<p>Error loading content: ${responseData.rightPaneHtmlLink}.\n${(error as Error).message}</p>`;
        }
    }
    
    if (responseData.rightPaneHtml) {
        CodexPage.setRightPane(responseData.rightPaneHtml);
    }

    // Reset the search string if loading a new page or restoring the prior state
    if (responseData.searchString && !data.searchString) {
        searchBox.value = responseData.searchString;
    }

    if (responseData.line || responseData.symbol) {
        GoToSymbolOrLineNumber(responseData.symbol, responseData.line);
    }

    if (responseData.pageState) {
        pageState = responseData.pageState;
    }

   var encodedAddress = encodeURIComponent(pageState.address || "/");
   updateLoginLink(pageState.loginId, encodedAddress);

    if (responseData.title || pageState.address) {
        setNavigationBar(pageState.address, responseData.title, isPopState);
    }

    if (data.url && data.url.indexOf('#') >= 0) {
     var dataUrl = new URL(data.url, window.location.href);
     if (dataUrl.hash.startsWith('#')) {
        var id = dataUrl.hash.slice(1);
        var target = document.getElementById(id);
        window.requestAnimationFrame(() => {
            target.scrollIntoView();
        });
     }
    }
}

function updateLoginLink(loginId?: string, encodedAddress?: string) {
}

function isCookieSet(cookieName) {
    // Split the cookie string into individual cookies
    const cookiesArray = document.cookie.split(';');

    // Loop through the cookies and check if the desired cookie is present
    for (let i = 0; i < cookiesArray.length; i++) {
        const cookie = cookiesArray[i].trim(); // Remove leading/trailing spaces
        if (cookie.startsWith(cookieName + '=')) {
            return true; // Cookie found
        }
    }

    return false; // Cookie not found
}

function setNavigationBar(address, title, isPopState) {
    if (!title) {
        title = DefaultWindowTitle;
    }

    document.title = title;
    if (!isPopState) {
        history.pushState(address, title, address);
    }
}

export var codexClient =
{
    Navigate: (url, isPopState) => {
        callPageController({ url }, isPopState);
    },
    Search: (searchString) => {
        callPageController({ searchString });
    }
};

export function setUpdateState(value) {
    updateState = value;

    // Enable the search box
    document.getElementById("search-box").removeAttribute("disabled");
}

function initSite(data)
{
    updateLoginLink(data.loginId);
}

updateLoginLink();