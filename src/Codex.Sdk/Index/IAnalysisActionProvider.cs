namespace Codex.Analysis;

public enum AnalysisAction
{
    Analyze,
    Skip,
    UpToDate
}


public interface IAnalysisActionProvider
{
    AnalysisAction GetAction(IProjectFileScopeEntity file);

    AnalysisAction GetAction(IProjectScopeEntity project);
}
