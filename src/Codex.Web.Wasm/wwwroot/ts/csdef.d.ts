export namespace CodexWeb {
    interface PageState {
        loginId?: string,
        isInitialized?: boolean,
        version: number,
        address?: string
    }

    interface PageResult extends PageRequest {
        leftPaneHtml?: string,
        rightPaneHtml?: string,
        rightPaneHtmlLink?: string,
        line?: number,
        symbol?: string,
        title?: string,
    }

    interface PageRequest {
        pageState?: PageState
        url?: string,
        searchString?: string
    }
}