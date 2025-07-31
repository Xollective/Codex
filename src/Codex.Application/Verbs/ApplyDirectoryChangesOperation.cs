using Codex.Sdk;
using Codex.Utilities;

namespace Codex.Application.Verbs;

[Verb("patchdir", HelpText = "Applies changes from source overlay directory to target base directory.")]
public record ApplyDirectoryChangesOperation : OperationBase
{
    [Option('s', "source", Required = true, HelpText = "The source directory to copy from. (corresponds to merge directory for linux overlay mount)")]
    public string SourceDirectory { get; set; }

    [Option('t', "target", Required = true, HelpText = "The target directory to copy to. (corresponds to lower directory for linux overlay mount)")]
    public string TargetDirectory { get; set; }

    [Option('m', "masking", Required = false, HelpText = "The masking directory indicating applicable files to copy. (corresponds to upper directory for linux overlay mount)")]
    public string? MaskingDirectory { get; set; }

    [Option('p', "purge", Required = false, Default = true, HelpText = "Deletes files and directories in the destination that no longer exist in the source")]
    public bool DeleteMissingTargetFiles { get; set; } = true;

    protected override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        SourceDirectory = Path.GetFullPath(SourceDirectory);
        TargetDirectory = Path.GetFullPath(TargetDirectory);
        MaskingDirectory = MaskingDirectory?.Apply(s => Path.GetFullPath(s));
    }

    protected override async ValueTask<int> ExecuteAsync()
    {
        await SdkPathUtilities.CopyFilesRecursiveAsync(
            sourceDirectory: SourceDirectory,
            targetDirectory: TargetDirectory,
            maskingDirectory: MaskingDirectory,
            logCopy: Logger.WriteLine,
            deleteMissingTargetFiles: DeleteMissingTargetFiles);

        return 0;
    }
}

