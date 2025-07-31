namespace Codex.Utilities;

using System.IO;
using System.Runtime.CompilerServices;
using Codex.Logging;
using LibGit2Sharp;

public class GitFileSystem : FileSystemWrapper
{
    private readonly Logger logger;
    private readonly Func<string, bool> shouldUseGit;

    public string RootDirectory { get; }
    public GitFileSystem(
        string rootDirectory, 
        Repository repository, 
        FileSystem inner, 
        Logger logger, 
        Func<string, bool> shouldUseGit = null)
        : base(inner)
    {
        Repository = repository;
        this.logger = logger;
        this.shouldUseGit = shouldUseGit ?? (_ => true);
        Tree = repository.Head?.Tip?.Tree;
        RootDirectory = rootDirectory;
    }

    public Repository Repository { get; }
    public Tree Tree { get; }

    public override Stream OpenFile(string filePath)
    {
        return OpenFile(filePath, default, out _);
    }

    public override Stream OpenFile(string filePath, OpenFileOptions options, out FileProperties properties)
    {
        if (TryGetFromBlob(filePath, out var stream, blob => blob.GetContentStream()))
        {
            properties = FileProperties.FromGit;
            return stream;
        }

        return base.OpenFile(filePath, options, out properties);
    }

    private bool TryGetFromBlob<T>(string filePath, out T result, Func<Blob, T> getResult, [CallerMemberName]string caller = null)
    {
        try
        {
            if (shouldUseGit(filePath) && Tree != null)
            {
                var relativePath = Paths.MakeRelativeToFolder(filePath, RootDirectory)?.Replace('\\', '/');
                if (!relativePath.Contains(".."))
                {
                    var blob = Tree[relativePath]?.Target as Blob;
                    if (blob != null)
                    {
                        result = getResult(blob);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogExceptionError($"GitFileSystem.{caller}({filePath}, RootDir:'{RootDirectory}')", ex);
        }

        result = default;
        return false;
    }

    public override long GetFileSize(string filePath)
    {
        if (TryGetFromBlob(filePath, out var size, blob => Repository.ObjectDatabase.RetrieveObjectMetadata(blob.Id).Size))
        {
            return size;
        }

        return base.GetFileSize(filePath);
    }

    //public override Stream OpenFile(string filePath)
    //{
    //    return OpenFile(filePath, out _);
    //}

    //public override Stream OpenFile(string filePath, out FileProperties properties)
    //{
    //    return base.OpenFile(filePath);
    //}
}