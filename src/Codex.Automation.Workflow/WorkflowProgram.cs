using System.Buffers;
using System.Collections;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Codex.Application;
using Codex.Cli;
using Codex.Logging;
using Codex.Sdk;
using Codex.Utilities;
using CommandLine;
using DotNext.Collections.Generic;
using J2N.Text;

namespace Codex.Automation.Workflow
{
    using static Codex.CodexConstants;
    using static Helpers;
    using static PipelineUtilities.AzureDevOps;
    using static PipelineUtilities;

    public partial class WorkflowProgram
    {
        public delegate bool RunCodexProcessDelegate(string exePath, string arguments, AsyncOut<int> exitCode);
        public static RunCodexProcessDelegate RunCodexProcess = (exePath, arguments, exitCode) => Helpers.RunProcess(exePath, arguments, exitCode: exitCode);

        internal static bool HasFlag(Mode mode, Mode flag)
        {
            return (mode & flag) == flag;
        }

        public static async Task<int> Main(string[] args)
        {
            using var _ = SdkFeatures.GlobalLogger.EnableLocal(
                SdkFeatures.GetGlobalLogger()
                ?? new ConsoleLogger());
            return await RunAsync(args);
        }

        public static ParseResult Parse(ConfiguredCommandLineArgs args, Arguments arguments)
        {
            var rootCommand = CreateRootCommand(arguments);
            var builder = new CommandLineBuilder(rootCommand)
                .UseVersionOption()
                .UseHelp()
                .UseEnvironmentVariableDirective()
                .UseParseDirective()
                .UseParseErrorReporting();

            if (args.UseExceptionHandler)
            {
                builder = builder.UseExceptionHandler();
            }

            builder = builder.CancelOnProcessTermination();

            return builder.Build().Parse(args.Arguments);
        }

        public static async Task<int> RunAsync(ConfiguredCommandLineArgs args, AsyncOut<Arguments> arguments = null)
        {
            arguments = arguments.SetOrCreate();
            arguments.Value = new();
            var parseResult = Parse(args, arguments.Value);
            var exitCode = await parseResult.InvokeAsync();

            if (exitCode != 0 && arguments.Value.AzureDevopsMode)
            {
                Console.WriteLine("##vso[task.complete result=Failed;]DONE");
            }

            return exitCode;
        }

        public static int Run(ConfiguredCommandLineArgs args, out Arguments arguments)
        {
            var result = RunAsync(args, new(out var argumentsResult)).GetAwaiter().GetResult();
            arguments = argumentsResult.Value;
            return result;
        }

        public static RootCommand CreateRootCommand(Arguments arguments)
        {
            string codexBinDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            arguments.CodexBinDir = codexBinDirectory;
            arguments.CodexExePath = Path.Combine(codexBinDirectory, "Codex.exe");

            var rootCommand = new RootCommand();

            foreach (var group in Enum.GetNames<Mode>().GroupBy(n => Enum.Parse<Mode>(n)))
            {
                var mode = group.Key;
                if (Out.Var(out var command, CreateSpecialCommand(mode, arguments)) == null)
                {
                    command = Arguments.AddInternalCommand(arguments, mode);
                }

                foreach (var name in group.Except([command.Name], StringComparer.OrdinalIgnoreCase))
                {
                    command.AddAlias(ProcessCommandName(name));
                }

                command.Description ??= GetCommandDescription(mode);

                rootCommand.Add(command);
            }

            return rootCommand;
        }

        public static string GetCommandDescription(Mode mode)
        {
            var ops = new List<string>()
            {
                HasFlag(mode, Mode.Prepare) ? "prepare pipeline variables" : null,
                HasFlag(mode, Mode.BuildOnly) ? "clone and build repository" : null,
                HasFlag(mode, Mode.AnalyzeOnly) ? "analyze repository" : null,
                HasFlag(mode, Mode.IndexOnly) ? "index analysis output" : null,
                HasFlag(mode, Mode.UploadOnly) ? "upload outputs" : null,
            };

            ops.RemoveAll(s => s == null);

            if (ops.Count > 0)
            {
                ops[0] = ops[0].Then(o => char.ToUpperInvariant(o[0]) + o[1..]);
            }

            return ops.Count switch
            {
                0 => null,
                2 => $"{ops[0]} and {ops[1]}",
                1 => ops[0],
                _ => string.Join(", ", ops)
            };
        }

        public static string ProcessCommandName(object mode)
        {
            var name = mode.ToString();
            bool lastWasLower = false;
            StringBuilder sb = new StringBuilder();
            foreach (var ch in name)
            {
                if (lastWasLower && !char.IsLower(ch))
                {
                    sb.Append("-");
                }

                lastWasLower = char.IsLower(ch);
                sb.Append(char.ToLowerInvariant(ch));
            }

            return sb.ToString();
        }

        private static Command? CreateSpecialCommand(Mode mode, Arguments arguments)
        {
            Command command = null;
            var name = ProcessCommandName(mode);
            if (mode == Mode.GetLocation)
            {
                command = CliModel.Bind<Arguments>(
                    new Command(name),
                    m =>
                    {
                        return arguments;
                    },
                    async a =>
                    {
                        Console.WriteLine(Assembly.GetExecutingAssembly().Location);
                        return 0;
                    });
            }
            else if (mode == Mode.Codex || mode == Mode.Cli)
            {
                command = CliModel.Bind<Arguments>(
                    new Command(name),
                    m =>
                    {
                        if (mode == Mode.Codex)
                        {
                            m.Option(c => ref c.RunCodexInproc, "in-proc-codex", description: "Run codex in-proc instead of as external process", defaultValue: true, isHidden: true);
                        }

                        m.Argument(c => ref c.CommandLine, "command-line", arity: ArgumentArity.ZeroOrMore);
                        return arguments;
                    },
                    async a =>
                    {
                        if (mode == Mode.Codex)
                        {
                            a.RunCodexProcess(arguments.CodexExePath, new ArgList(arguments.CommandLine).ToString(), exitCode: new(out var exitCode));
                            return exitCode;
                        }
                        else
                        {
                            return await new CliProgram().RunAsync(a.CommandLine);
                        }
                    });
            }

            return command;
        }

        [CollectionBuilder(typeof(ConfiguredCommandLineArgs), nameof(Create))]
        public record struct ConfiguredCommandLineArgs(params string[] Arguments) : IEnumerable<string>
        {
            public static ConfiguredCommandLineArgs Create(ReadOnlySpan<string> items) => new(items.ToArray());

            public bool UseExceptionHandler { get; set; } = true;

            public static implicit operator ConfiguredCommandLineArgs(string[] args) => new(args);

            public static implicit operator string[](ConfiguredCommandLineArgs args) => args.Arguments;

            public IEnumerator<string> GetEnumerator()
            {
                return Arguments.AsEnumerable().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal static bool RunMode(Mode mode, Arguments arguments, AsyncOut<int> exitCode)
        {
            string codexBinDirectory = arguments.CodexBinDir;
            string executablePath = arguments.CodexExePath;

            bool success = true;
            if (HasFlag(mode, Mode.BuildOnly))
            {
                UpdateBuildNumber(arguments);
            }

            arguments.CodexOutputRoot = Path.GetFullPath(arguments.CodexOutputRoot ?? "");

            if (!string.IsNullOrEmpty(arguments.CodexOutputRoot)
                && string.IsNullOrEmpty(arguments.SourcesDirectory))
            {
                arguments.SourcesDirectory = Path.Combine(arguments.CodexOutputRoot, "src");
            }

            if (!arguments.NoBuildTag && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RELEASE_RELEASENAME")))
            {
                AddBuildTag(BuildTags.CodexEnabled);
                AddBuildTag(BuildTags.FormatVersion);

                if (HasFlag(mode, Mode.IndexOnly))
                {
                    AddBuildTag(BuildTags.CodexIndexEnabled);
                }

                if (arguments.RepoUri is { } uri)
                {
                    AddBuildTag(uri.GetBuildTag());
                }
            }

            string createOutDir(string name)
            {
                var dir = Path.Combine(arguments.CodexOutputRoot, name);
                Directory.CreateDirectory(dir);
                return dir;
            }

            arguments.BinLogDir ??= createOutDir("binlogs");
            string binlogDirectory = arguments.BinLogDir;
            arguments.ProjectDataDir = createOutDir("projectdata");
            arguments.DebugDir = createOutDir("debug");
            arguments.ToolsDir = createOutDir("tools");

            arguments.BuildTempDir = createOutDir("bldtmp");

            arguments.ProjectDataArgs = new()
            {
                executablePath,
                "analyze",
                "-o",
                arguments.ProjectDataDir,
                "--scenario",
                "ProjectData",
                "--path",
                arguments.Settings?.RepoRoot ?? arguments.SourcesDirectory,
            };

            arguments.ProjectDataArgs.Add(arguments.ExplicitRepoName?.Then(n => new string[] { "--name", n }) ?? []);

            string analysisOutputDirectory = (arguments.AnalyzeOutputDirectory ??= createOutDir("store"));
            string indexOutputDirectory = (arguments.IngestOutputDirectory ??= createOutDir("index"));
            arguments.ExtractionRoot = createOutDir("extract");

            var envMap = arguments.GetEnvMap();

            if (arguments.RepoConfigRoot is string repoConfigRoot
                && (File.Exists(Out.Var(out var analysisSettingsPath, Path.Combine(repoConfigRoot, $"{arguments.Qualifier}.analyze.settings.json")))
                || File.Exists(Out.Var(out analysisSettingsPath, Path.Combine(repoConfigRoot, "analyze.settings.json")))))
            {
                var text = File.ReadAllText(analysisSettingsPath);
                var processedText = MiscUtilities.ExpandVariableTokens(text, name =>
                {
                    return (envMap.TryGetValue(name, out var value) ? value : Environment.GetEnvironmentVariable(name))?.Replace(@"\", @"\\");
                });
                var settings = JsonSerializationUtilities.DeserializeEntity<AnalyzeSettings>(processedText);
                arguments.AdditionalCodexArguments.Add(settings.AddArguments);
                arguments.AnalysisRemoveArguments.Add(settings.RemoveArguments);
                arguments.Settings = settings;
                arguments.EncryptOutputs |= settings.EncryptOutputs;
            }

            Func<string> getAnalysisArguments = () => string.Join(" ", new ArgList(new[] {
                    "analyze",
                    arguments.Clean ? "--clean" : "",
                    "--noMsBuild",
                    "--out",
                    analysisOutputDirectory,
                    "--path",
                    arguments.Settings?.RepoRoot ?? arguments.SourcesDirectory,
                    "--binLogSearchDirectory",
                    arguments.BinLogDir + "*",
                    "--projectData",
                    arguments.ProjectDataDir,
                    }
                    .Except(arguments.AnalysisRemoveArguments, StringComparer.OrdinalIgnoreCase)
                    .Concat(arguments.AdditionalCodexArguments)
                    .Select(s => envMap.ReplaceTokens(s))));
            var analysisArguments = getAnalysisArguments();
            string indexArguments = string.Join(" ", new ArgList(new[] {
                    "ingest",
                    arguments.Clean ? "--clean" : "",
                    "--storedFilters",
                    "--external",
                    "--in",
                    arguments.IngestInputDirectory ?? analysisOutputDirectory,
                    "--out",
                    indexOutputDirectory,
                    }
                    .Except(arguments.IndexRemoveArguments, StringComparer.OrdinalIgnoreCase)
                    .Concat(arguments.AdditionalIndexArguments)
                    .Select(s => envMap.ReplaceTokens(s))));

            if (HasFlag(mode, Mode.Prepare))
            {
                // download files
                SetPipelineVariable("CodexBinDir", codexBinDirectory, isSecret: false);
                SetPipelineVariable("CodexAnalysisOutDir", analysisOutputDirectory, isSecret: false);
                SetPipelineVariable("CodexExePath", executablePath, isSecret: false);
                SetPipelineVariable("CodexAnalysisArguments", analysisArguments, isSecret: false);

                if (!string.IsNullOrEmpty(arguments.RepoName))
                {
                    SetPipelineVariable("CodexRepoName", arguments.RepoName, isSecret: false);
                }
            }

            if (HasFlag(mode, Mode.BuildOnly))
            {
                var analysisPreparation = new AnalysisPreparation(arguments);
                analysisPreparation.Run();
                analysisArguments = getAnalysisArguments();

                Directory.CreateDirectory(arguments.ProjectDataDir);

                SetPipelineVariable("CodexAnalysisArguments", analysisArguments, isSecret: false);

                // Set build number again here because build may have changed the build number.
                UpdateBuildNumber(arguments);
            }

            if (HasFlag(mode, Mode.AnalyzeOnly))
            {
                Directory.CreateDirectory(arguments.ProjectDataDir);

                // run exe
                success &= arguments.RunCodexProcess(executablePath, analysisArguments, exitCode);
            }

            if (HasFlag(mode, Mode.IndexOnly))
            {
                // run exe
                success &= arguments.RunCodexProcess(executablePath, indexArguments, exitCode);
            }

            if (HasFlag(mode, Mode.UploadOnly))
            {
                foreach (var (directory, _outputName) in new[] {
                    (analysisOutputDirectory, BuildAnalysisArtifactName),
                    (indexOutputDirectory, BuildIndexArtifactName),
                    (arguments.DebugDir, "DebugDir")})
                {
                    if (Directory.Exists(directory) && Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Any())
                    {
                        // get json files and zip
                        var outputName = _outputName;

                        var zipDirectory = createOutDir(outputName);

                        string outputZipFileName = arguments.RepoUri?.GetUnicodeRepoFileName() ?? Path.GetFileName(directory.TrimTrailingSlash());

                        if (arguments.Commit != null)
                        {
                            outputZipFileName += "." + Guid.NewGuid().ToString("N").Substring(0, 8);
                        }

                        string outputZip = Path.Combine(zipDirectory, outputZipFileName + ".zip");

                        Console.WriteLine($"Zipping analysis files: '{directory}' -> '{outputZip}' ");

                        MiscUtilities.CreateZipFromDirectory(sourceDirectory: directory, outputZip,
                            password: arguments.EncryptOutputs ? SdkFeatures.DefaultZipStorePassword : (string)null,
                            publicKey: arguments.EncryptOutputs ? SdkFeatures.DefaultZipStorePasswordPublicKey : (string)null);

                        if (HasFlag(mode, Mode.Test))
                        {
                            outputName += "Test";
                        }

                        // publish to a vsts build
                        Console.WriteLine($"Publishing to Build: '{outputZip}' -> '{outputName}'");
                        Console.WriteLine($"##vso[artifact.upload artifactname={outputName};]{outputZip}");
                        AddBuildTag(outputName);
                    }
                    else
                    {
                        Console.WriteLine($"Could not find files in directory '{directory}'.");
                    }
                }

                //foreach (var debugFile in PathUtilities.GetDirectoryFilesSafe(arguments.DebugDir, "*", SearchOption.TopDirectoryOnly))
                //{
                //    Console.WriteLine($"##vso[artifact.upload artifactname={DebugArtifactName};]{debugFile}");
                //}
            }

            if (arguments.UploadBinlogs)
            {
                foreach (var binlog in
                    PathUtilities.GetDirectoryFilesSafe(binlogDirectory, "*.binlog", SearchOption.TopDirectoryOnly)
                    .Concat(PathUtilities.GetDirectoryFilesSafe(binlogDirectory, "*.complog", SearchOption.TopDirectoryOnly)))
                {
                    Console.WriteLine($"##vso[artifact.upload artifactname={BinLogArtifactName};]{binlog}");
                }
            }

            if (HasFlag(mode, Mode.Ingest))
            {

            }

            return success;
        }

        private static void UpdateBuildNumber(Arguments arguments)
        {
            if (!string.IsNullOrEmpty(arguments.BuildName))
            {
                var buildId = Environment.GetEnvironmentVariable("BUILD_BUILDID");
                var invalidChars = new char[] { '"', '/', ':', '<', '>', '\\', '|', '?', '@', '*' };
                var buildNumber = $"{arguments.BuildName}{arguments.Timestamp:yyyyMMdd.hhmmsss}#{buildId}";
                invalidChars.ForEach(c => buildNumber = buildNumber.Replace(c, '_'));
                Console.WriteLine(
                    $"##vso[build.updatebuildnumber]{buildNumber}");
            }
        }
    }
}
