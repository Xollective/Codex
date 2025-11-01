using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Basic.CompilerLog.Util;
using Codex.Analysis.Managed;
using Codex.Import;
using Codex.Logging;
using Codex.MSBuild;
using Codex.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Codex.Analysis.Projects
{
    public class CompilerLogProjectAnalyzer(Logger logger, string[] searchPaths)
        : InvocationSolutionProjectAnalyzer(logger, searchPaths)
    {
        protected override string Description => "compiler log";

        protected override string[] FileTypes { get; } = ["*.compilerlog", "*.complog"];

        protected override IEnumerable<InvocationSolutionInfoBuilderBase> GetBuilders(Repo repo, string[] files)
        {
            return files.Select(file => new SolutionInfoBuilder(file, repo));
        }

        public class SolutionInfoBuilder : InvocationSolutionInfoBuilderBase
        {
            private string compilerLogPath;
            private SolutionReader reader;

            public SolutionInfoBuilder(string compilerLogFilePath, Repo repo)
                : base(compilerLogFilePath, repo)
            {
                this.compilerLogPath = compilerLogFilePath;
                Initialize();
            }

            public override bool HasProjects => reader?.ProjectCount > 0;

            public void Initialize()
            {
                if (compilerLogPath == null)
                {
                    return;
                }

                Placeholder.Todo("Dispose reader?");
                reader = SolutionReader.Create(compilerLogPath);
            }

            public override SolutionInfo Build(bool linkProjects = false)
            {
                return reader.ReadSolutionInfo();
            }
        }
    }
}
