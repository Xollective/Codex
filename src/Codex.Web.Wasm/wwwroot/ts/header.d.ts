export interface ICodexClient {
    Navigate(url: string);
}

declare global {
    function ensureSearchBox();

    function GoToSymbolOrLineNumber(symbol: string, line: number);
    
    var searchBox: HTMLInputElement;
}
