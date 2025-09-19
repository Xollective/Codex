using System.CommandLine;
using Codex.Application;
using Codex.Cli;
using Codex.Sdk;
using Codex.Sdk.Utilities;
using Codex.Storage;
using Codex.Utilities;
using CommandLine;

namespace Codex.Automation.Workflow
{
    using static MiscUtilities;
    using static WorkflowProgram;

    public partial class Arguments
    {
        public static Command AddInternalCommand(Arguments arguments, Mode mode)
        {
            // NOTE: This tool often runs in contexts where we want to switch modes freely so we hide options rather than removing
            // them to avoid caller needing to micromanage the arguments which are passed.
            return CliModel.Bind<Arguments>(
                new Command(ProcessCommandName(mode)),
                m =>
                {
                    RefFunc<Arguments, List<string>> argList(RefFunc<Arguments, ArgList> getFieldRef, out Action<Arguments, List<string>> init)
                    {
                        var box = new Box<List<string>>();

                        init = (args, list) =>
                        {
                            getFieldRef(args).Add(list);
                        };

                        return a => ref box.Value;
                    }

                    Action<Arguments, List<string>>? init = null;
                    var s = new Box<string>();
                    var ss = new Box<string[]>();
                    var b = new Box<bool>();

                    using (m.HideOptionsIfNot(HasFlag(mode, Mode.IndexOnly) || HasFlag(mode, Mode.AnalyzeOnly)))
                    {
                        m.Option(c => ref c.RunCodexInproc, "in-proc-codex", description: "Run codex in-proc instead of as external process", defaultValue: true, isHidden: true);
                    }

                    // Core output directory - used by most modes
                    m.Option(c => ref c.CodexOutputRoot, name: "output", aliases: ["o"], required: true,
                        description: "Root directory for all Codex output files (analysis, index, binlogs, etc.)",
                        defaultValue: GetEnvironmentVariableOrDefault("SYSTEM_ARTIFACTSDIRECTORY")?.Then(dir => Path.Combine(dir, "cdxout"))?.AsOptional() ?? default);

                    using (m.HideOptionsIfNot(HasFlag(mode, Mode.AnalyzeOnly) || HasFlag(mode, Mode.BuildOnly)))
                    {
                        m.Option(c => ref arguments.RepoSpec, name: "repo", aliases: ["r", "url"], required: true,
                            description: "Repository name or url with optional commit/tag (format: name<@commit><#qualifier>)");

                        m.Option(c => ref arguments.ExplicitRepoName, name: "repo-name", aliases: ["name", "n"], required: false,
                            description: "Explicit name of repository (normally not needed if --repo is specified)");

                        // Repository and source control options
                        m.Option(c => ref c.Commit, name: "commit", aliases: ["ref-spec", "ref", "c"], required: false,
                            description: "Git commit hash or refspec to analyze (used for unique artifact naming)");

                        m.Option(c => ref c.ConfigRoot, name: "config-root", required: false,
                            description: "Directory containing repository-specific configuration files (analyze.settings.json)");

                        m.Option(c => ref c.BinLogDir, name: "bin-log-directory", aliases: "bld", required: false,
                            description: "Directory containing MSBuild binary logs to analyze",
                            transform: value => Path.GetFullPath(value));
                    }

                    // General options
                    m.Option(c => ref c.Clean, name: "clean", required: false,
                        description: "Clean output directories before running");

                    m.Option(c => ref c.NoBuildTag, name: "no-build-tag", required: false,
                        description: "Skip adding Azure DevOps build tags");

                    m.Option(c => ref c.AnalyzeOutputDirectory, name: "analyze-output-directory", required: false,
                        description: "Output directory for analysis results (defaults to {output}/store)");

                    // Build-related options
                    using (m.HideOptionsIfNot(HasFlag(mode, Mode.BuildOnly)))
                    {
                        m.Option(c => ref c.SourcesDirectory, name: "sources-directory", aliases: ["s", "src"], required: false,
                            description: "Directory containing source code to analyze (defaults to {output}/src)");

                        m.Option(c => ref c._buildName, name: "build-name", required: false,
                            description: "Custom build name for Azure DevOps build number update");

                        m.Option(c => ref c.GenerateCompilerLogs, name: "generate-compiler-logs", aliases: "complog", required: false,
                            description: "Generate compiler logs during build for detailed analysis");

                        m.Option(c => ref c.NoClone, name: "no-clone", required: false,
                            description: "Skip cloning the repository (use existing source)");

                        m.Option(argList(c => ref c.BuildArgs, out init), init: init, name: "build-args", required: false,
                            description: "Additional arguments to pass to the build system");

                        m.Option(argList(c => ref c.RootProjects, out init), init: init, name: "root-projects", required: false,
                            description: "Root project files to build (e.g., solution or project files)");

                        m.Option(c => ref ss.Value, name: "pat", required: false,
                            description: "Personal access tokens for repository access (format: url=token)",
                            init: (c, values) =>
                            {
                                foreach (var value in values)
                                {
                                    var pair = value.Split('=');
                                    if (pair.Length == 2)
                                    {
                                        Console.WriteLine($"Adding PAT: '{pair[0]}'='{string.Empty.PadRight(Math.Max(3, pair[1].Length), '*')}'");
                                        arguments.PersonalAccessTokens[pair[0]] = pair[1];
                                    }
                                }
                            });
                    }

                    // Analysis options
                    using (m.HideOptionsIfNot(HasFlag(mode, Mode.AnalyzeOnly) || HasFlag(mode, Mode.Prepare) || HasFlag(mode, Mode.BuildOnly)))
                    {
                        m.Option(c => ref c.AdditionalCodexArguments, name: "additional-codex-arguments", required: false,
                            description: "Additional arguments to pass to Codex analyze command");

                        m.Option(c => ref c.AnalysisRemoveArguments, name: "analysis-remove-arguments", required: false,
                            description: "Arguments to remove from the default analyze command");
                    }

                    // Index/Ingest options
                    using (m.HideOptionsIfNot(HasFlag(mode, Mode.IndexOnly) || HasFlag(mode, Mode.Prepare)))
                    {
                        m.Option(c => ref c.IngestInputDirectory, name: "ingest-input-directory", required: false,
                         description: "Input directory for ingestion (defaults to analyze output directory)");

                        m.Option(argList(c => ref c.AdditionalIndexArguments, out init), init: init, name: "additional-index-arguments", required: false,
                            description: "Additional arguments to pass to Codex ingest command");

                        m.Option(c => ref c.IndexRemoveArguments, name: "index-remove-arguments", required: false,
                            description: "Arguments to remove from the default ingest command");
                    }

                    using (m.HideOptionsIfNot(HasFlag(mode, Mode.IndexOnly) || HasFlag(mode, Mode.UploadOnly) || HasFlag(mode, Mode.Prepare)))
                    {
                        m.Option(c => ref c.IngestOutputDirectory, name: "ingest-output-directory", required: false,
                            description: "Output directory for indexed data (defaults to {output}/index)");
                    }

                    // Upload options
                    using (m.HideOptionsIfNot(HasFlag(mode, Mode.UploadOnly)))
                    {
                        m.Option(c => ref c.EncryptOutputs, name: "encrypt-outputs", required: false,
                            description: "Encrypt output artifacts with password protection");
                    }

                    m.Option(c => ref c.UploadBinlogs, name: "upload-binlogs", required: false,
                        description: "Upload MSBuild binary logs as Azure DevOps artifacts");

                    // GC mode specific
                    if (HasFlag(mode, Mode.GC))
                    {

                    }

                    // Debug/diagnostic options
                    m.Option(c => ref b.Value, name: "print-env", required: false,
                        isHidden: true,
                        description: "Print all environment variables for debugging",
                        init: (c, shouldPrint) =>
                        {
                            if (shouldPrint)
                            {
                                Console.WriteLine($"Environment Variables:");
                                foreach (System.Collections.DictionaryEntry envVar in Environment.GetEnvironmentVariables())
                                {
                                    Console.WriteLine($"{envVar.Key}={envVar.Value}");
                                }
                                Console.WriteLine($"Done printing environment variables.");
                            }
                        });

                    m.AddHandler(a =>
                    {
                        a.Initialize();
                    });

                    return arguments;
                },
                async args =>
                {
                    var result = RunMode(mode, args, new(out var exitCode));

                    return exitCode;
                });
        }

        public Arguments Initialize()
        {
            ParseRepoSpec();

            return this;
        }

        private void ParseRepoSpec()
        {
            if (!string.IsNullOrEmpty(RepoSpec))
            {
                ExtractCore("@", ref RepoSpec, ref Commit);
                ExtractCore("#", ref RepoSpec, ref Qualifier);
                ExtractCore("@", ref RepoSpec, ref Commit);

                if (SourceControlUri.TryParse(RepoSpec, out RepoUri, checkRepoNameFormat: true))
                {
                    RepoName ??= RepoUri.GetRepoName();
                    HttpRepoUri ??= new Uri(RepoUri.GetUrl());
                }
                else if (Uri.TryCreate(RepoSpec, UriKind.Absolute, out var result))
                {
                    HttpRepoUri ??= result;
                    RepoName ??= result.ToString().TrimEnd('/').SubstringAfterLastIndexOfAny("/").ToString().AsNotEmptyOrNull() ?? RepoName;
                }
                else
                {
                    RepoName ??= RepoSpec;
                }
            }

            RepoName = ExplicitRepoName ?? RepoName;

            ConfigSpecifier ??= RepoUri?.Then(u => Path.Combine(u.GetKindPathFragment(), RepoName, Qualifier ?? string.Empty));

            if (RunCodexInproc)
            {
                RunCodexProcess = (exePath, arguments, exitCode) =>
                {
                    var args = arguments.SplitArgs();

                    var code = CodexProgram.Main(args).GetAwaiter().GetResult();
                    exitCode?.Set(code);
                    return code == 0;
                };
            }
        }
    }
}
