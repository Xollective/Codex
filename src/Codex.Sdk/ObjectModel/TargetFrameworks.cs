using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using static Codex.Analysis.Projects.ProjectTargetFramework;

namespace Codex.Analysis.Projects;

using static DefineConstant;

public record ProjectTargetFramework(DefineConstant Define, string Identifier)
{
    public int Priority => (int)Define;

    public static ImmutableDictionary<string, ProjectTargetFramework> All { get; } = GetFrameworkMap();

    static ImmutableDictionary<string, ProjectTargetFramework> GetFrameworkMap()
    {
        var map = ImmutableDictionary.CreateBuilder<string, ProjectTargetFramework>(StringComparer.OrdinalIgnoreCase);
        foreach (var define in Enum.GetValues<DefineConstant>())
        {
            string defineString = define.ToString();
            var id = defineString.Replace("_", ".").ToLowerInvariant();
            var framework = new ProjectTargetFramework(define, id);
            map[defineString] = framework;
            map[id] = framework;
        }

        return map.ToImmutable();
    }

    public static int Compare(ProjectTargetFramework tf1, ProjectTargetFramework tf2)
    {
        return (tf1?.Priority ?? 0).CompareTo(tf2?.Priority ?? 0);
    }

    public enum DefineConstant
    {
        // Temporarily prefer other frameworks until support for net9.0 is verified
        // Namely, whether it is installed on agents and the version of Roslyn we ship
        // with has support for any new language features.
        NET9_0 = 1,
        NET46,
        NET472,
        NET48,
        NETSTANDARD2_0,
        NETSTANDARD2_1,
        NETCOREAPP2_0,
        NETCOREAPP2_1,
        NETCOREAPP3_0,
        NETCOREAPP3_1,
        NET5_0,
        NET6_0,
        NET7_0,
        NET8_0,
    }
}