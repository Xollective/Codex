using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    public class BinLogProjectAnalyzer(Logger logger, string[] searchPaths)
        : InvocationSolutionProjectAnalyzer(logger, searchPaths)
    {
        protected override string Description => "binlog";

        protected override string[] FileTypes { get; } = ["*.binlog"];

        protected override IEnumerable<InvocationSolutionInfoBuilderBase> GetBuilders(Repo repo, string[] files)
        {
            return files.Select(file => new SolutionInfoBuilder(file, repo));
        }

        public class SolutionInfoBuilder : InvocationSolutionInfoBuilderBase
        {
            private string binLogPath;

            public SolutionInfoBuilder(string binLogFilePath, Repo repo)
                : base(binLogFilePath, repo)
            {
                this.binLogPath = binLogFilePath;
                Initialize();
            }

            public void Initialize()
            {
                if (binLogPath == null)
                {
                    return;
                }

                foreach (var invocation in BinLogReader.ExtractInvocations(binLogPath))
                {
                    StartLoadProject(invocation);
                }
            }
        }
    }
}
