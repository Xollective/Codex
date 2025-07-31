using System;
using System.Web;
using Codex.ObjectModel;
using Codex.Utilities;

namespace Codex.Storage.Store;

public partial class DirectoryCodexStore
{
    public IAnalyzedProjectProvider GetProjectProvider()
    {
        return new ProjectProvider(this, GetFileSystem());
    }

    private record ProjectProvider(DirectoryCodexStore Owner, FileSystem FileSystem) : IAnalyzedProjectProvider
    {
        public List<string> ProjectFiles { get; } = FileSystem.GetFiles(StoredEntityKind.Projects.Name).ToList();

        public BoundSourceFile Convert(StoredBoundSourceFile storedBoundFile)
        {
            return Owner.FromStoredBoundFile(storedBoundFile);
        }

        public void Dispose()
        {
            FileSystem.Dispose();
        }

        public IEnumerable<IStoredAnalyzedProject> GetProjects()
        {
            foreach (var file in ProjectFiles)
            {
                yield return new StoredAnalyzedProject(Owner, FileSystem, GetProjectKeyFromPath(file), file);
            }
        }

        private ProjectKey GetProjectKeyFromPath(string file)
        {
            var fileName = Path.GetFileName(file);
            var fileNameWithoutExtension = fileName.TrimEndIgnoreCase(EntityFileExtension);

            var projectIdUri = Uri.UnescapeDataString(fileNameWithoutExtension);
            var projectId = projectIdUri.AsSpan().SubstringBeforeFirstIndexOfAny("&").ToString();
            var qualifiedId = Uri.EscapeDataString(projectIdUri.AsSpan().SubstringBeforeLastIndexOfAny("&").ToString());

            return new ProjectKey(Uri.UnescapeDataString(projectId), qualifiedId);
        }
    }

    private record StoredAnalyzedProject(DirectoryCodexStore Owner, FileSystem FileSystem, ProjectKey Key, string ProjectInfoPath) : IStoredAnalyzedProject
    {
        public AnalyzedProjectInfo Load() => StoredEntityKind.Projects.Read(Owner, FileSystem, ProjectInfoPath);

        public StoredBoundSourceFile GetFile(string fileId)
        {
            return StoredEntityKind.BoundFiles.Read(Owner, FileSystem, fileId);
        }

        public IEnumerable<IAnalyzedFileReference> GetFiles()
        {
            var files = FileSystem.GetFiles(Path.Combine(StoredEntityKind.BoundFiles.Name, Key.QualifiedId));
            foreach (var file in files)
            {
                yield return new AnalyzedFileReference(this, file);
            }
        }
    }

    private record AnalyzedFileReference(StoredAnalyzedProject Project, string Id) : IAnalyzedFileReference
    {
        public Task AddToAsync(ICodexRepositoryStore store)
        {
            return StoredEntityKind.BoundFiles.Add(Project.Owner, Project.FileSystem, Id, store);
        }

        public StoredBoundSourceFile LoadStored()
        {
            return Project.GetFile(Id);
        }
    }
}
