using Codex.Analysis;
using Codex.Application.Verbs;
using Codex.Logging;
using Codex.ObjectModel;
using Codex.Sdk.Search;
using Codex.Storage.Store;
using Codex.Utilities;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.VisualBasic;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace Codex.Storage
{
    public class CodexLegacyProgram
    {
        public static ImmutableHashSet<string> LegacyVerbNames { get; } =
            ImmutableHashSet<string>.Empty
            .WithComparer(StringComparer.OrdinalIgnoreCase)
            .Union(GetActions().Select(a => a.Key));

        private static IEnumerable<KeyValuePair<string, VerbOperation>> GetActions()
        {
            return new Dictionary<string, VerbOperation>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "load",
                    new VerbOperation<IngestOperation>
                    (
                        ingest => new OptionSet
                        {
                            // Obsolete args
                            { "n|name=", "Name of the repository.", n => { } },
                            { "newBackend", "Use new backend with stored filters Not supported.", n => { } },
                            { "reset", "Reset elasticsearch for indexing new set of data.", n => { } },
                            { "u", "Updates the analysis data (in place).", n => { } },

                            { "scan", "Treats every directory under data directory as a separate store to upload.", n => ingest.Scan = n != null },
                            { "l|logDirectory", "Optional. Path to log directory", n => ingest.LogDirectory = n },
                            { "clean", "Reset target index directory when using -save option.", n => ingest.Clean = n != null },
                            { "d=", "The directory or a zip file containing analysis data to load.", n => ingest.InputPath = n },
                            { "save=", "Saves the analysis information to the given directory.", n => ingest.OutputDirectory = n },
                            { "test", "Indicates that save should use test mode which disables optimization.", n => ingest.DisableOptimization = n != null },
                        }
                    )
                },
                {
                    "index",
                    new VerbOperation<AnalyzeOperation>
                    (
                        Initialize: analyze =>
                        {
                            // Emit build tags in legacy mode. Mainly we want to emit the
                            // version build tag here.
                            analyze.EmitBuildTags = true;
                        },
                        GetOptions: analyze => new OptionSet
                        {
                            // Obsolete args
                            { "newBackend", "Use new backend with stored filters Not supported.", n => { } },
                            { "i|interactive", "Search newly indexed items.", n => { } },

                            { "ed|extData=", "Specifies one or more external data directories.", n => analyze.ExternalDataDirectories.Add(n) },
                            { "pd|projectData=", "Specifies one or more project data directories.", n => analyze.ProjectDataDirectories.Add(n) },
                            { "pds|projectDataSuffix=", "Specifies the suffix for saving project data.", n => analyze.ProjectDataSuffix = n },
                            { "noScan", "Disable scanning enlistment directory.", n => analyze.DisableEnumeration = n != null },
                            { "noMsBuild", "Disable loading solutions using msbuild.", n => analyze.DisableMsBuild = n != null },
                            { "noMsBuildLocator", "Disable loading solutions using msbuild.", n => analyze.DisableMsBuildLocator = n != null },
                            { "save=", "Saves the analysis information to the given directory.", n => analyze.OutputDirectory = n },
                            { "test", "Indicates that save should use test mode which disables optimization.", n => analyze.DisableOptimization = n != null },
                            { "clean", "Reset target index directory when using -save option.", n => analyze.Clean = n != null },
                            { "n|name=", "Name of the repository.", n => analyze.RepoName = StoreUtilities.GetSafeRepoName(n ?? string.Empty) },
                            { "p|path=", "Path to the repo to analyze.", n => analyze.RootDirectory = Path.GetFullPath(n) },
                            { "repoUrl=", "The URL of the repository being indexed", n => analyze.RepoUrl = n },
                            { "bld|binLogSearchDirectory=", "Adds a bin log file or directory to search for binlog files", n => analyze.BinLogSearchPaths.Add(n) },
                            { "ca|compilerArgumentFile=", "Adds a file specifying compiler arguments", n => analyze.CompilerArgumentsSearchPaths.Add(n) },
                            { "l|logDirectory=", "Optional. Path to log directory", n => analyze.LogDirectory = n },
                            { "s|solution=", "Optionally, path to the solution to analyze.", n => analyze.SolutionPaths.Add(n) },
                            { "projectMode", "Uses project indexing mode.", n => If(n != null, () => analyze.Scenario = AnalyzeOperation.AnalysisScenario.ProjectData) },
                            { "file=", "Specifies single file to analyze.", n => analyze.FileToAnalyze = n },
                            { "disableParallelFiles", "Disables use of parallel file analysis.", n => analyze.DisableParallelFiles = n != null },
                            { "disableDetectGit", "Disables use of LibGit2Sharp to detect git commit and branch.", n => analyze.DetectGit = n == null },
                        }
                    )
                }
            };
        }

        private static void If(bool condition, Action action)
        {
            if (condition) action();
        }

        public record VerbOperation<TOperation>(Func<TOperation, OptionSet> GetOptions, Action<TOperation> Initialize = null) : VerbOperation
            where TOperation : OperationBase, new()
        {
            public override bool TryParse(IReadOnlyList<string> args, out OperationBase operation, out IReadOnlyList<string> remainingArguments)
            {
                operation = null;

                var operationImpl = new TOperation();
                Initialize?.Invoke(operationImpl);
                var options = GetOptions(operationImpl);

                remainingArguments = options.Parse(args);
                if (remainingArguments.Count > 0) return false;

                operation = operationImpl;
                return true;
            }

            public override OptionSet HelpOptions => GetOptions(null);
        }

        public abstract record VerbOperation
        {
            public abstract bool TryParse(IReadOnlyList<string> args, out OperationBase operation, out IReadOnlyList<string> remainingArguments);

            public abstract OptionSet HelpOptions { get; }
        }

        public async Task<int> RunAsync(params string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Console.WriteLine("Started");

                var actions = GetActions().ToDictionarySafe(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase, overwrite: true);

                if (args.Length == 0)
                {
                    WriteHelpText();
                    return 0;
                }

                var verbArgs = args.Skip(1).ToArray();
                var verb = args[0].ToLowerInvariant();
                if (actions.TryGetValue(verb, out var action))
                {
                    if (!action.TryParse(verbArgs, out var operation, out var remainingArguments))
                    {
                        Console.Error.WriteLine($"Invalid argument(s): '{string.Join(", ", remainingArguments)}'");
                        WriteHelpText();
                        return -1;
                    }

                    Console.WriteLine("Parsed Arguments");
                    return await operation.RunAsync();
                }
                else
                {
                    Console.Error.WriteLine($"Invalid verb '{verb}'");
                    WriteHelpText();
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                return -1;
            }
        }

        protected bool isReentrant = false;

        protected HashSet<string> knownMessages = new HashSet<string>()
        {
            "Unable to load DLL 'api-ms-win-core-file-l1-2-0.dll': The specified module could not be found. (Exception from HRESULT: 0x8007007E)",
            "Invalid cast from 'System.String' to 'System.Int32[]'.",
            "The given assembly name or codebase was invalid. (Exception from HRESULT: 0x80131047)",
            "Value was either too large or too small for a Decimal.",
        };

        protected virtual void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (isReentrant)
            {
                return;
            }

            isReentrant = true;
            try
            {
                var ex = e.Exception;

                if (ex is InvalidCastException)
                {
                    if (ex.Message.Contains("Invalid cast from 'System.String' to"))
                    {
                        return;
                    }

                    if (ex.Message.Contains("Unable to cast object of type 'Microsoft.Build.Tasks.Windows.MarkupCompilePass1' to type 'Microsoft.Build.Framework.ITask'."))
                    {
                        return;
                    }
                }

                if (ex is InvalidOperationException)
                {
                    if (ex.Message.Contains("An attempt was made to transition a task to a final state when it had already completed."))
                    {
                        return;
                    }
                }

                if (ex is System.Net.WebException)
                {
                    if (ex.Message.Contains("(404) Not Found"))
                    {
                        return;
                    }
                }

                if (ex is AggregateException || ex is OperationCanceledException)
                {
                    return;
                }

                if (ex is DecoderFallbackException)
                {
                    return;
                }

                if (ex is DirectoryNotFoundException)
                {
                    return;
                }

                if (ex is FileNotFoundException)
                {
                    return;
                }

                if (ex is MissingMethodException)
                {
                    // MSBuild evaluation has a known one
                    return;
                }

                if (ex is XmlException && ex.Message.Contains("There are multiple root elements"))
                {
                    return;
                }

                if (knownMessages.Contains(ex.Message))
                {
                    return;
                }

                string exceptionType = ex.GetType().FullName;

                if (exceptionType.Contains("UnsupportedSignatureContent"))
                {
                    return;
                }

                string stackTrace = ex.StackTrace;
                if (stackTrace?.Contains("at System.Guid.StringToInt") == true)
                {
                    return;
                }

                var message = DateTime.Now.ToString() + ": First chance exception: " + ex.ToString();

                Log(message);
            }
            finally
            {
                isReentrant = false;
            }
        }
        protected void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log(e.ExceptionObject?.ToString());
            try
            {
                Log(string.Join(Environment.NewLine, AppDomain.CurrentDomain.GetAssemblies().Select(a => $"{a.FullName ?? "Unknown Name"}: {a.Location ?? "Unknown Location"}")));
            }
            catch
            {
            }
        }

        protected void Log(string text)
        {
            Console.Error.WriteLine(text);
        }

        protected void WriteHelpText()
        {
            foreach (var actionEntry in GetActions())
            {
                Console.WriteLine($"codex {actionEntry.Key} {{options}}");
                Console.WriteLine("Options:");
                actionEntry.Value.HelpOptions.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
            }
        }
    }
}
