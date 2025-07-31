using System.Diagnostics;
using System.Reflection;

namespace Tenray.ZoneTree.Core;

public static class ZoneTreeInfo
{
    static Version Version = null;

    /// <summary>
    /// Gets ZoneTree Product Version
    /// </summary>
    /// <returns></returns>
    public static Version ProductVersion
    {
        get
        {
            if (Version != null)
                return Version;
            string str = OperatingSystem.IsBrowser() ? "1.0.0.0" : GetVersionString();
            Version = Version.Parse(str);
            return Version;
        }
    }

    private static string GetVersionString()
    {
        return FileVersionInfo
            .GetVersionInfo(Assembly.GetExecutingAssembly().Location)
            .FileVersion;
    }
}
