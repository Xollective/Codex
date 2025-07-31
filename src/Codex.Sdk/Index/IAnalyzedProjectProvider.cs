using Codex.ObjectModel;

namespace Codex.Storage.Store;

public interface IAnalyzedProjectProvider : IDisposable
{
    IEnumerable<IStoredAnalyzedProject> GetProjects();

    BoundSourceFile Convert(StoredBoundSourceFile storedBoundFile);
}

public record struct ProjectKey(string ProjectId, string QualifiedId);

public record struct ProjectFileKey(string ProjectRelativePath);


public interface IStoredAnalyzedProject
{
    ProjectKey Key { get; }

    AnalyzedProjectInfo Load();

    IEnumerable<IAnalyzedFileReference> GetFiles();

    StoredBoundSourceFile GetFile(string fileId);
}

public interface IAnalyzedFileReference
{
    string Id { get; }

    StoredBoundSourceFile LoadStored();

    Task AddToAsync(ICodexRepositoryStore store);
}

