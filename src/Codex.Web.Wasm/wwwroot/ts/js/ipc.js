var CodexIpc;
(function (CodexIpc) {
    class PromiseCompletionSource {
        constructor() {
            var self = this;
            this.Task = new Promise(function (resolve, reject) {
                self.Resolve = resolve;
                self.Reject = reject;
            });
        }
    }
    CodexIpc.PromiseCompletionSource = PromiseCompletionSource;
    var UnknownGlobal = globalThis;
    var WorkerGlobalScope = UnknownGlobal;
    var SharedWorkerGlobalScope = UnknownGlobal;
    var DedicatedWorkerGlobalScope = UnknownGlobal;
    let WellKnownCallIds;
    (function (WellKnownCallIds) {
        WellKnownCallIds[WellKnownCallIds["Disconnect"] = -1] = "Disconnect";
    })(WellKnownCallIds || (WellKnownCallIds = {}));
    class WorkerClientBase {
        constructor(server, port) {
            this.Id = server.connect(port);
            this.Server = server;
            var proxy = new Proxy(this, {
                get: this.getFunction,
                has: function (target, key) {
                    return true;
                }
            });
            this.Target = proxy;
        }
        getFunction(target, name, receiver) {
            if (name === "then") {
                return undefined;
            }
            return function (arg) {
                var message = {
                    args: arg,
                    id: target.Id,
                    method: name,
                };
                return target.Server.callClientAsync(message);
            };
        }
        close() {
            this.Server.disconnect(this.Id);
        }
    }
    CodexIpc.WorkerClientBase = WorkerClientBase;
    class SharedWorkerClient extends WorkerClientBase {
        constructor(server, startupScript) {
            var worker = new SharedWorker(startupScript);
            super(server, worker.port);
            this.Worker = worker;
            this.Worker.port.start();
            var client = this;
            window.addEventListener("beforeunload", ev => {
                server.callClient({
                    id: client.Id,
                    method: "<disconnect>",
                });
            });
        }
    }
    CodexIpc.SharedWorkerClient = SharedWorkerClient;
    class WorkerClient extends WorkerClientBase {
        constructor(server, startupScript) {
            var worker = new Worker(startupScript);
            super(server, worker);
            this.Worker = worker;
        }
    }
    CodexIpc.WorkerClient = WorkerClient;
    class IpcServer {
        constructor(target) {
            this.nextConnectionId = 1;
            this.nextCallId = 1;
            this.connections = new Map();
            this.pendingCalls = new Map();
            this.Target = target;
            var server = this;
            if (WorkerGlobalScope.importScripts) {
                if (SharedWorkerGlobalScope.onconnect) {
                    SharedWorkerGlobalScope.onconnect = function (e) {
                        var port = e.ports[0];
                        server.connect(port);
                    };
                }
                else if (DedicatedWorkerGlobalScope.postMessage) {
                    this.connect(DedicatedWorkerGlobalScope);
                }
            }
        }
        connect(port) {
            var id = this.nextConnectionId++;
            this.connections.set(id, port);
            ;
            var server = this;
            port.onmessage = async function (ev) {
                var d = ev.data;
                if (d.callId) {
                    let response = {};
                    try {
                        response.resolve = await server.Target[d.method](d.args);
                    }
                    catch (ex) {
                        response.reject = ex;
                    }
                    port.postMessage({
                        returnCallId: d.callId,
                        args: response,
                        method: d.method,
                        id: d.id
                    });
                }
                else if (d.returnCallId) {
                    let response = d.args;
                    let pcs = server.pendingCalls.get(d.returnCallId);
                    if (response.reject) {
                        pcs.Reject(response.reject);
                    }
                    else {
                        pcs.Resolve(response.resolve);
                    }
                }
                else if (d.method === "<disconnect>") {
                    server.disconnect(d.id);
                }
                else {
                    server.Target[d.method](d.args);
                }
            };
            return id;
        }
        disconnect(id) {
            this.connections.delete(id);
        }
        callClient(m) {
            this.connections.get(m.id).postMessage(m);
        }
        callClientAsync(m) {
            m.callId = this.nextCallId++;
            var pcs = new PromiseCompletionSource();
            this.pendingCalls.set(m.callId, pcs);
            this.connections.get(m.id).postMessage(m);
            return pcs.Task;
        }
    }
    CodexIpc.IpcServer = IpcServer;
})(CodexIpc || (CodexIpc = {}));
//# sourceMappingURL=ipc.js.map