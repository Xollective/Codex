using Codex.Lucene.Search;
using System.ComponentModel;
using Codex.Storage.Store;
using Codex.Utilities;
using Codex.Sdk;
using Codex.Storage;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.ContractsLight;
using System.IO.Compression;

namespace Codex.Application.Verbs;

[Verb("zipput", HelpText = "Add files to zip archive.")]
public record ZipPutOperation : OperationBase
{
    [Option('s', "source", Required = true, HelpText = "The source directory or file to add")]
    public string SourcePath { get; set; }

    [Option('z', "zip", Required = true, HelpText = "The zip file to modify")]
    public string TargetZipPath { get; set; }

    [Option('p', "prefix", HelpText = "The prefix for entry paths")]
    public string EntryPathPrefix { get; set; }

    protected override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        SourcePath = Path.GetFullPath(SourcePath);
        TargetZipPath = Path.GetFullPath(TargetZipPath);
        EntryPathPrefix = EntryPathPrefix?.EnsureTrailingSlash('/', normalize: true);
    }

    protected override async ValueTask<int> ExecuteAsync()
    {
        var files = new string[0];
        bool isFile = false;
        if (File.Exists(SourcePath))
        {
            files = new[] { SourcePath };
            isFile = true;
        }
        else
        {
            files = PathUtilities.GetAllFilesRecursive(SourcePath);
        }

        using var zip = ZipFile.Open(TargetZipPath, ZipArchiveMode.Update);

        foreach (var file in files)
        {
            var relativePath = isFile
                ? Path.GetFileName(file)
                : PathUtilities.GetRelativePath(SourcePath, file);

            var zipInnerPath = PathUtilities.UriCombine(EntryPathPrefix, relativePath);

            Logger.LogMessage($"Copying '{zipInnerPath}' from '{file}'");
            var entry = zip.CreateEntry(zipInnerPath);

            using (var entryStream = entry.Open())
            using (var fileStream = File.OpenRead(file))
            {
                await fileStream.CopyToAsync(entryStream);
            }
        }

        return 0;
    }
}
