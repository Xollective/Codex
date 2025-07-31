using System.Reflection;
using System.Runtime.InteropServices;
using Git = LibGit2Sharp;

namespace LibGit2Sharp;

public class LibGit
{
    static LibGit()
    {
        InitCore();
    }

    public static bool Init()
    {
        // This is here only to trigger static constructor
        return true;
    }

    private static void InitCore()
    {
        var architecture = RuntimeInformation.ProcessArchitecture;
        var dllDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"runtimes\\{GetOsPlatform()}-{architecture}\\native".ToLowerInvariant());
        Git.GlobalSettings.NativeLibraryPath = dllDirectory;
    }

    private static string GetOsPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }

        throw new NotSupportedException($"Unsupported OS platform.");
    }
}