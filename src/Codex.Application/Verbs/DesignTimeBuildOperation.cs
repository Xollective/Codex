namespace Codex.Application.Verbs;

using Codex.Analysis.MSBuild;
using Codex.Utilities;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.MSBuild;

[Verb("dt", aliases: new[] { "dtbuild" }, HelpText = "Run a design time build and generate a binlog.")]
public partial record DesignTimeBuildOperation : OperationBase
{
    [Value(0, Required = true, HelpText = "The path to the project or solution")]
    public string ProjectPath { get; set; }

    [Option("bl", Required = true, HelpText = "The input directory or a zip file containing analysis data to load.")]
    public string BinlogPath { get; set; }

    [Option("cv", HelpText = "The verbosity of the console logger.")]
    public ConsoleLoggerVerbosity ConsoleVerbosity { get; set; }

    [Option('p', "property", HelpText = "List of global properties in form Key=Value")]
    public IList<string> Properties { get; set; } = new List<string>();

    protected override async ValueTask InitializeAsync()
    {
        MSBuildHelper.RegisterMSBuild();
        await base.InitializeAsync();
    }

    protected override async ValueTask<int> ExecuteAsync()
    {
        var workspace = MSBuildWorkspace.Create();

        var logger = new DesignTimeLogger(BinlogPath);

        if (ProjectPath.EndsWithIgnoreCase(".sln"))
        {
            var solution = await workspace.OpenSolutionAsync(ProjectPath, logger);
        }

        return 0;
    }

    public enum ConsoleLoggerVerbosity
    {
        Quiet = 0,
        Minimal = 1,
        Normal = 2,
        Detailed = 3,
        Diagnostic = 4
    }
}
