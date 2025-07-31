using System.Diagnostics.ContractsLight;
using Codex.Analysis.Projects;

namespace Codex.ObjectModel.Implementation
{
    public partial class AnalyzedProjectInfo
    {
        public AnalyzedProjectInfo(string repositoryName, string projectId)
        {
            RepositoryName = repositoryName;
            ProjectId = projectId;
            Properties = new();
        }

        public ProjectTargetFramework? TargetFramework
        {
            get => ProjectTargetFramework.All.GetOrDefault(Properties.GetOrDefault(PropertyKey.TargetFramework) ?? "");
            set
            {
                if (value == null)
                {
                    Properties.Remove(PropertyKey.TargetFramework);
                }
                else
                {
                    Properties[PropertyKey.TargetFramework] = value.Identifier;
                }
            }
        }
    }
}
