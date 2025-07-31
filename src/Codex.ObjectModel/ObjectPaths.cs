namespace Codex.ObjectModel;

public static class ObjectPaths
{
    private static readonly char[] DirSeparatorChars = new[] { '/', '\\' };

    public static string GetObjectPath<T>(string repositoryName, SearchType<T> searchType, T entity, bool forceUnique = false)
        where T : class, ISearchEntity
    {
        var pathSegments = searchType.GetObjectPath(entity);
        if (pathSegments == null) return null;

        for (int i = 0; i < pathSegments.Length; i++)
        {
            ref var segment = ref pathSegments[i];
            segment = Paths.SanitizeFileName(segment);
            segment = segment.Length <= 50
                ? segment
                : $"{segment.Truncate(50)}[{IndexingUtilities.ComputeSymbolUid(segment).Truncate(6)}]";
        }

        var repoName = SourceControlUri.GetUnicodeRepoFileName(repositoryName);

        var uidStamp = forceUnique
            ? $"[{IndexingUtilities.ComputeSymbolUid(entity.Uid).Substring(0, 6).ToLower()}]"
            : "";

        return $"{searchType.IndexName}/{repoName}/{string.Join('/', pathSegments)}{uidStamp}.bin";
    }

    public static string GetContentPath(IRepoFileScopeEntity file)
    {
        var repoName = SourceControlUri.GetUnicodeRepoFileName(file.RepositoryName);
        return $"content/{repoName}/{file.RepoRelativePath.Replace('\\', '/')}";
    }

    public static string[] GetPath(IDefinitionSearchModel s)
    {
        var d = s.Definition;
        return new[]
        {
            d.ProjectId,
            d.ShortName,
            $"{d.Id.Value}[{IndexingUtilities.ComputeSymbolUid(s.Uid).Truncate(6)}]"
        };
    }

    public static string[] GetPath(ITextChunkSearchModel s)
    {
        return null;
    }

    private static string[] GetPath(IProjectFileScopeEntity s)
    {
        return new[]
        {
            s.ProjectId,
            $"{s.ProjectRelativePath.AsSpan().SubstringAfterLastIndexOfAny(DirSeparatorChars)}[{IndexingUtilities.ComputeSymbolUid(s.ProjectRelativePath).Truncate(6)}]",
        };
    }

    public static string[] GetPath(IReferenceSearchModel s)
    {
        return GetPath(s.FileInfo).Concat(new[] { s.Symbol.Id.Value }).ToArray();
    }

    internal static string[] GetPath(ITextSourceSearchModel arg)
    {
        return GetPath(arg.File);
    }

    internal static string[] GetPath(IBoundSourceSearchModel arg)
    {
        return GetPath(arg.File.Info);
    }
}