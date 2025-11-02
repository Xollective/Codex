using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Basic.CompilerLog.Util;
using Codex.Import;
using Codex.Logging;
using Codex.Utilities;
using Microsoft.CodeAnalysis;

namespace Codex.Analysis.Projects
{
    public abstract class InvocationSolutionProjectAnalyzer(
        Logger logger,
        string[] searchPaths) : RepoProjectAnalyzerBase
    {
        public bool RequireProjectFilesExist { get; init; } = true;

        public Func<string, string[]>? FileFinder { get; init; }

        protected abstract string Description { get; }

        protected abstract string[] FileTypes { get; }

        protected abstract IEnumerable<InvocationSolutionInfoBuilderBase> GetBuilders(Repo repo, string[] files);

        public override void CreateProjects(Repo repo)
        {
            logger.LogMessage($"{Description.Capitalize()} search paths:{Environment.NewLine}{string.Join(Environment.NewLine, searchPaths)}");

            var fileFinder = FileFinder ?? FindFiles;

            foreach (var searchPath in searchPaths)
            {
                var files = fileFinder(searchPath);
                if (files.Length != 0)
                {
                    logger.LogMessage($"Found {files.Length} {Description}s at compiler log search path '{searchPath}':{Environment.NewLine}{string.Join(Environment.NewLine, files)}");
                }
                else
                {
                    logger.LogMessage($"No {files.Length} {Description} found at compiler log search path '{searchPath}'.");
                    continue;
                }

                foreach (var builder in GetBuilders(repo, files))
                {
                    if (builder.HasProjects)
                    {
                        SolutionProjectAnalyzer.AddSolutionProjects
                            (repo,
                            () => Task.FromResult(builder.Build()),
                            workspace: builder.Workspace,
                            requireProjectExists: RequireProjectFilesExist,
                            solutionName: builder.SolutionName);
                    }
                }
            }

            base.CreateProjects(repo);
        }

        public string[] FindFiles(string searchPath)
        {
            return FindFiles(searchPath, FileTypes);
        }

        public static string[] FindFiles(string searchPath, params string[] fileTypes)
        {
            bool recursive = false;
            if (searchPath.EndsWith("*"))
            {
                searchPath = searchPath.TrimEnd('*');
                recursive = true;
            }

            if (string.IsNullOrWhiteSpace(searchPath))
            {
                return Array.Empty<string>();
            }

            if (File.Exists(searchPath))
            {
                return new[] { searchPath };
            }

            if (Directory.Exists(searchPath))
            {
                return fileTypes.SelectMany(fileType => Directory.GetFiles(searchPath, fileType,
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    .ToArray();
            }

            return Array.Empty<string>();
        }
    }
}
