using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Codex.Cli;
using Codex.Sdk;
using Codex.Utilities;
using CommandLine;

namespace Codex.Automation.Workflow
{
    public partial class Arguments
    {
        public string[] CommandLine = [];

        public bool AzureDevopsMode = true;
        public bool RunCodexInproc = false;

        public WorkflowProgram.RunCodexProcessDelegate RunCodexProcess { get => field ?? WorkflowProgram.RunCodexProcess; set; }

        public DateTime Timestamp = DateTime.UtcNow;
        public ArgList AdditionalCodexArguments = new ArgList();
        public ArgList AdditionalIndexArguments = new ArgList();

        public AnalyzeSettings Settings = new AnalyzeSettings();
        public ArgList BuildArgs = new ArgList();
        public ArgList RootProjects = new ArgList();

        public ArgList ProjectDataArgs = new ArgList();

        public List<string> AnalysisRemoveArguments = new List<string>();
        public List<string> IndexRemoveArguments = new List<string>();
        public string CodexOutputRoot;
        public string BinLogDir;
        public string ProjectDataDir;
        public string ExplicitRepoName;
        public string RepoName { get; set; }
        public string RepoSpec;
        public Uri HttpRepoUri { get; set; }
        public string ConfigSpecifier;

        public SourceControlUri? RepoUri;

        public string RepoConfigRoot
        {
            get
            {
                if (ConfigRoot == null || ConfigSpecifier == null) return null;
                return Path.GetFullPath(Path.Combine(
                    ConfigRoot,
                    "repos",
                    ConfigSpecifier));
            }
        }

        public string ConfigRoot;
        public string SourcesDirectory;
        public bool EncryptOutputs;

        private string _buildName;
        public string BuildName => _buildName.AsNotEmptyOrNull() ?? RepoName;

        public string CodexBinDir { get; internal set; }
        public string CodexExePath { get; internal set; }
        public string DebugDir { get; internal set; }
        public string ToolsDir { get; internal set; }
        public string BuildTempDir { get; internal set; }
        public string ExtractionRoot { get; internal set; }

        public string Commit;
        public string Qualifier;

        public string IngestInputDirectory;
        public string IngestOutputDirectory;
        public string AnalyzeOutputDirectory;
        public bool NoClone;
        public bool Clean;
        public bool NoBuildTag;
        public bool DisableUpload;
        public bool DisablePrepare;
        public bool UploadBinlogs;
        public bool GenerateCompilerLogs;
        public Dictionary<string, string> PersonalAccessTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static void ExtractCore(string separator, ref string argValue, ref string extractTarget)
        {
            if (argValue.Contains(separator) && argValue.Split(separator) is var parts)
            {
                argValue = parts[0];
                extractTarget ??= parts[1];
            }
        }

        public EnvMap GetEnvMap()
        {
            return GetEnvMap(this);
        }

        private static EnvMap GetEnvMap(Arguments arguments)
        {
            return new()
            {
                ["CodexConfigRoot"] = arguments.ConfigRoot,
                ["CodexRepoConfigRoot"] = arguments.RepoConfigRoot,
                ["CodexReposConfigRoot"] = Path.Combine(arguments.ConfigRoot ?? "", "repos"),
                ["CodexBuildConfigRoot"] = Path.Combine(arguments.ConfigRoot ?? "", "repos", ".build"),
                ["SrcDir"] = arguments.SourcesDirectory,
                ["BinLogDir"] = arguments.BinLogDir,
                ["CodexDebugDir"] = arguments.DebugDir,
                ["CodexToolsDir"] = arguments.ToolsDir,
                ["CodexBuildTempDir"] = arguments.BuildTempDir,
                ["CodexBinDir"] = arguments.CodexBinDir,
                ["CodexExtractTargets"] = Path.Combine(arguments.CodexBinDir ?? "", "Codex.Managed.Extractor.targets"),
                ["CodexCommonArgs"] = arguments.ProjectDataArgs?.ToString(),
                ["CodexProjectExtractionRoot"] = arguments.ExtractionRoot,
                ["MSBuildEnableWorkloadResolver"] = "false",
                ["DisableWorkloads"] = "true",
            };
        }
    }


    public class EnvMap : Dictionary<string, string>
    {
        public EnvMap() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public EnvMap(EnvMap map) : base(map, map.Comparer)
        {

        }

        public string ReplaceTokens(string input)
        {
            foreach ((var key, var value) in this)
            {
                if (input.ContainsIgnoreCase(key))
                {
                    input = input.ReplaceIgnoreCase($"{{{key}}}", value);
                }
            }

            return input;
        }
    }

    public class ArgList : Collection<string>
    {
        public ArgList() { }

        public ArgList(params string[] args)
        {
            Add(args);
        }

        public ArgList(IEnumerable<string> args)
        {
            Add(args);
        }

        public void Add(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                Add(arg);
            }
        }

        public override string ToString()
        {
            return string.Join(" ", this.Select(s => s.QuoteIfNeeded()));
        }

        protected override void InsertItem(int index, string item)
        {
            if (string.IsNullOrEmpty(item)) return;
            base.InsertItem(index, item);
        }
    }

    public class CliArgList<T>(T value, RefFunc<T, ArgList> getFieldRef) : IInitializedCliArgument
    {
        public List<string> Arguments;

        void IInitializedCliArgument.PostSetInitialize()
        {
            if (Arguments != null)
            {
                getFieldRef(value) = new ArgList(Arguments);
            }
        }
    }

    public class ArgumentContext
    {
        public bool IsActiveIteration { get; set; }
        public bool IsActive { get; set; }
        public string ArgumentName { get; set; }
        public IList<string> ArgumentValues { get; set; }
        public string ArgumentValue { get; set; }

        public void SetActive()
        {
            IsActiveIteration = true;
        }
    }
}
