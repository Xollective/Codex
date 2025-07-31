using System.Diagnostics;
using Codex.Logging;
using Codex.Utilities;
using DotNext;

namespace Codex.Application.Verbs;

public interface IOperation : IDisposable
{
    ValueTask<int> RunAsync(bool initializeOnly = false, bool logErrors = true, bool throwErrors = true);
}

public abstract record CmdletOperation<TResult> : OperationBase
{
    public TResult Result { get; private set; }

    public void SetResult(TResult result)
    {
        Result = result;
        Cmdlet?.WriteObject(result);
    }
}

public abstract record OperationBase : IOperation, IDisposable
{
    public ICmdletContext? Cmdlet { get; set; }

    public string LogDirectory { get; set; }

    [Option("logPath", HelpText = "The path to emitted log file.")]
    public string LogPath { get; set; }

    [Option("launchDebugger", HelpText = "Indicates that debugger should be launched.", Hidden = true)]
    public bool LaunchDebugger { get; set; }

    public Logger Logger { get; set; }
    private Logger localLogger;
    private IDisposable globalLoggerScope;

    protected virtual ValueTask InitializeAsync()
    {
        if (LaunchDebugger)
        {
            Debugger.Launch();
        }

        localLogger = Logger;
        Logger ??= SdkFeatures.GetGlobalLogger();
        Logger ??= (localLogger = GetLogger());

        if (localLogger != null)
        {
            globalLoggerScope = SdkFeatures.GlobalLogger.EnableGlobal(localLogger);
        }

        if (!string.IsNullOrEmpty(LogDirectory))
        {
            LogPath ??= Path.Combine(LogDirectory, "cdx.log");
        }

        if (!string.IsNullOrEmpty(LogPath))
        {
            LogPath = Path.GetFullPath(LogPath);
            LogDirectory = Path.GetDirectoryName(LogPath);
        }

        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        globalLoggerScope?.Dispose();
        localLogger?.Dispose();
    }

    public async ValueTask<int> RunAsync(bool initializeOnly = false, bool logErrors = true, bool throwErrors = true)
    {
        using (this)
        {
            await InitializeAsync();

            bool logError(Exception ex)
            {
                if (logErrors)
                {
                    Logger?.LogExceptionError("Execute", ex);
                }

                return logErrors;
            }

            if (!initializeOnly)
            {
                try
                {
                    return await ExecuteAsync();
                }
                catch (Exception ex) when (logError(ex) && !throwErrors)
                {
                    return -1;
                }
            }

            return 0;
        }
    }

    protected abstract ValueTask<int> ExecuteAsync();

    protected virtual Logger GetLogger()
    {
        var consoleLogger = new ConsoleLogger();
        if (string.IsNullOrEmpty(LogDirectory) && string.IsNullOrEmpty(LogPath))
        {
            return consoleLogger;
        }

        return new MultiLogger(
            consoleLogger,
            new TextLogger(TextWriter.Synchronized(OpenLogWriter())));
    }

    protected StreamWriter OpenLogWriter()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
        return new StreamWriter(LogPath);
    }
}
