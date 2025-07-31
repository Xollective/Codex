namespace Codex.Application.Verbs;

public class LegacyArgumentRemapper
{
    public static Dictionary<string, Dictionary<string, string>> GetRemappings()
    {
        var remappings = new Dictionary<string, Dictionary<string, string>>()
        {
            {
                "index",
                new()
                {
                    { "save", "--out" },
                    { "bld", "--binLogSearchDirectory" },
                    { "repoUrl", "--repoUrl" },
                    { "noMsBuildLocator", "--noMsBuildLocator" },
                    { "ca", "--compilerArgumentFile" },
                }
            }
        };


        return remappings;
    }
}
