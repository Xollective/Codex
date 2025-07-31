using Codex.Utilities;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Codecs;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Search
{
    public static class LuceneConstants
    {
        public const string SourceFieldName = ".source";
        public const string StableIdFieldName = ".stableId";

        public const string FilterRoot = "filters";
        public const string RepoSettingsRelativePath = "reposettings.json";
        public const string AllGroupName =  ReservedGroupNamePrefix + "all";

        public const string ReservedGroupNamePrefix = "_";
        public const string ExclusiveGroupNameSuffix = ".only";

        public static bool IsReservedGroupName(string name) => name.StartsWith(ReservedGroupNamePrefix);

        public static string GetGroupName(this RepoAccess access)
        {
            return ReservedGroupNamePrefix + access.ToString().ToLowerInvariant();
        }

        public static string CoerceRepoFilterName(string name)
        {
            return IsRepoFilterName(name) ? name : "repo_/" + name;
        }

        public static bool IsRepoFilterName(string name)
        {
            return name.Contains("/");
        }

        public static bool IsAccessGroupName(string name)
        {
            return IsReservedGroupName(name) && Enum.TryParse<RepoAccess>(name.AsSpan().Slice(1), ignoreCase: true, out _);
        }

        public const string PrefilterRelativePath = "prefilter.json";

        public const string StableIdStorageHeaderPath = "db/header.json";
        public const string StableIdStorageHeaderLegacyPath = ".filters/db/header.json";

        public static IEnumerable<string> GetRepositorySettingsSearchPaths(string repoName)
        {
            var settingsRoot = Path.Combine("reposettings");
            if (SourceControlUri.TryParse(repoName, out var repoUri, checkRepoNameFormat: true))
            {
                settingsRoot = Path.Combine(settingsRoot, repoUri.Kind.ToString().ToLower());
                var settingsPath = Path.Combine(settingsRoot, repoUri.GetRepoName() + ".settings.json");
                yield return settingsPath;

                var settingsDir = Path.GetDirectoryName(settingsPath);

                while (settingsDir.Length > settingsRoot.Length)
                {
                    yield return Path.Combine(settingsDir, "dirsettings.json");
                    settingsDir = Path.GetDirectoryName(settingsDir);
                }
            }
            else
            {
                settingsRoot = Path.Combine(settingsRoot, "other");
                var settingsPath = Path.Combine(settingsRoot, SourceControlUri.GetUnicodeRepoFileName(repoName) + ".settings.json");
                yield return settingsPath;
            }
        }


        public static readonly Analyzer StandardAnalyzer = new StandardAnalyzer(CurrentVersion);

        public const LuceneVersion CurrentVersion = LuceneVersion.LUCENE_48;

        public static readonly Query MatchNoDocsQuery = new BooleanQuery();

        public const byte SummaryNGramSize = 3;
        public const string SummaryIndexSegmentIdFieldName = ".segmentId";

        public class Boosts : CodexConstants.Boosts
        {
        }
    }
}
