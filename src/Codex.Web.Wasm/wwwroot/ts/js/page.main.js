var DefaultWindowTitle = "ReF12";
var CodexIpc;
(function (CodexIpc) {
    class CodexPage {
        constructor() {
            this.leftPane = document.getElementById("leftPaneContent");
            this.rightPane = document.getElementById("rightPane");
            this.Server = new CodexIpc.IpcServer(this);
        }
        setLeftPane(innerHtml) {
            if (!innerHtml) {
                innerHtml = "<div></div>";
            }
            this.leftPane.innerHTML = innerHtml;
        }
        setExplorerPane(innerHtml) {
            if (!innerHtml) {
                innerHtml = "";
            }
            this.leftPane.innerHTML = innerHtml;
        }
        setPageTitle(title) {
            if (!title) {
                title = DefaultWindowTitle;
            }
            document.title = title;
        }
        setNavigationBar(address, title) {
            if (!title) {
                title = DefaultWindowTitle;
            }
            document.title = title;
            history.pushState(address, title, address);
        }
        setRightPane(innerHtml) {
            if (!innerHtml) {
                innerHtml = "<div></div>";
            }
            this.rightPane.innerHTML = innerHtml;
        }
        StartWorkerAsync() {
            if (!this.WorkerClient) {
                this.WorkerClient = new CodexIpc.WorkerClient(this.Server, "/worker.main.js");
            }
            return Promise.resolve(this.WorkerClient.Target);
        }
        LogMessageAsync(args) {
            console.log("Called LogMessage with args: " + args);
            return "Responded";
        }
    }
    CodexIpc.CodexPage = CodexPage;
})(CodexIpc || (CodexIpc = {}));
CodexIpc.ServiceMode = "Page";
var CodexPage = new CodexIpc.CodexPage();
CodexIpc.Service = CodexPage;
CodexIpc.OnLoaded = function () {
    console.log("Blazor loaded in page");
};
//# sourceMappingURL=page.main.js.map