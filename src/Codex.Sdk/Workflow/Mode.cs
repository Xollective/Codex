namespace Codex.Automation.Workflow
{
    /// <summary>
    /// Defines the mode for the automation workflow
    /// Maybe this is in the SDK for use by AutoIndexing?
    /// </summary>
    public enum Mode
    {
        Prepare = 1 << 16,
        UploadOnly = 1 << 17,
        AnalyzeOnly = 1 << 18,
        BuildOnly = 1 << 19,
        GC = 1 << 20 | Prepare,
        IndexOnly = 1 << 21,
        Codex = 1 << 22,
        Test = 1 << 23,

        Analyze = Prepare | AnalyzeOnly | UploadOnly,
        Upload = Prepare | UploadOnly,
        Build = Prepare | BuildOnly,
        Index = Prepare | IndexOnly,
        Ingest = Index,

        IndexAndUpload = Index | UploadOnly,

        BuildAndAnalyze = Build | AnalyzeOnly | UploadOnly,

        TestBuildAndAnalyze = Build | AnalyzeOnly | UploadOnly | Test,

        FullAnalyze = Prepare | BuildOnly | AnalyzeOnly | UploadOnly,

        FullIndex = Prepare | BuildOnly | AnalyzeOnly | IndexOnly | UploadOnly,

        GetLocation = 0,
        Cli
    }
}
