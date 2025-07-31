using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codex.Utilities;

namespace Codex.MSBuild;

public class DesignTimeBuildRootProjectGenerator
{
    public static void GenerateDesignTimeBuildProject(string outputPath, IEnumerable<string> allProjectFullPaths)
    {
        var allProjectsSet = allProjectFullPaths
            .Select(p => PathUtilities.NormalizePath(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}