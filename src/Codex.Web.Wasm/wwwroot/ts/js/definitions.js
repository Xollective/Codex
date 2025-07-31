var CodexIpc;
(function (CodexIpc) {
    CodexIpc.OnLoaded = () => {
        console.error("OnLoaded base impl");
        return Promise.resolve(true);
    };
    function GetServiceMode() {
        return CodexIpc.ServiceMode;
    }
    CodexIpc.GetServiceMode = GetServiceMode;
    function GetService() {
        return CodexIpc.Service;
    }
    CodexIpc.GetService = GetService;
    class IndexService {
        constructor() {
            this.ServicePromise = new CodexIpc.PromiseCompletionSource();
        }
        StartAsync(service) {
            console.log("Staring service in browser worker");
            this.Service = service;
            this.ServicePromise.Resolve(service);
            console.log("Finished service in browser worker");
            return true;
        }
        async WaitForStartAsync() {
            await this.ServicePromise.Task;
            return 2920;
        }
        async CallAsync(nameAndArgs) {
            let service = await this.ServicePromise.Task;
            return await service.invokeMethodAsync("CallAsync", nameAndArgs[0], nameAndArgs[1]);
        }
        async LogAsync(message) {
            console.log("Worker recieved message: " + message);
            return 1234;
        }
    }
    CodexIpc.IndexService = IndexService;
})(CodexIpc || (CodexIpc = {}));
//# sourceMappingURL=definitions.js.map