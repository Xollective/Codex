using System.IO;
using System.Runtime.CompilerServices;

namespace CodexTestBProject;

public class TestBProject
{
    public static string ProjectName { get; } = typeof(TestBProject).Assembly.GetName().Name;

    public static string ProjectDirectory { get; } = GetProjectDirectory();

    public static string RepoDirectory { get; } = GetParentDirectory(GetProjectDirectory(), ".gitignore");

    public static string ProjectPath { get; } = Path.Combine(ProjectDirectory, Path.GetFileName(ProjectDirectory) + ".csproj");

    private static string GetParentDirectory(string path, string parentDirectoryFile)
    {
        while (!string.IsNullOrEmpty(path) && !File.Exists(Path.Combine(path, parentDirectoryFile)))
        {
            path = Path.GetDirectoryName(path);
        }

        return path;
    }

    private static string GetProjectDirectory([CallerFilePath]string filePath = null)
    {
        return Path.GetDirectoryName(filePath);
    }
}